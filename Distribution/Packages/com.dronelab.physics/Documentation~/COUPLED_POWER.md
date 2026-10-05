# Этап 11: нагрузка винта, инерция ротора и питание

Этап 10 подтверждён пользователем. Новое расширение находится в Assets/DronePhysics;
UPM snapshots пересобираются в конце улучшений. Старые profiles работают в режиме
FirstOrder без изменения смысла motor lag. Это DC/BLDC equivalent model, не симуляция
коммутации фаз, индуктивности, PWM или электрохимии аккумулятора.

## RPM/J и батарея

PowerSystem теперь получает осевую скорость **каждого** rotor point относительно ветра,
а также текущую плотность. При каждой попытке ограничения скорости пересчитываются
RPM, J, Cq, Q, электрическая нагрузка, ток батареи и просадка. Проверка сил и энергетики
использует один и тот же поток; больше нет нагрузки J=0 при фактическом J!=0.
Реальный currentA из RPM-таблицы остаётся измеренной телеметрией и не подменяет модель.

С батареей допустимы карты с Clamp, полным grid и неотрицательным Cq. Reject отклоняется:
старты и пробные RPM/J решателя могут выходить за измеренную область. Clamp публикуется
в телеметрии и не означает подтверждённую точность за границей данных.

Решатель требует монотонного Q(omega) при фиксированной осевой скорости. В каждой
билинейной ячейке проверяются углы и три условия:
`2*Cq + RPM*dCq/dRPM >= 0`, `2*Cq - J*dCq/dJ >= 0`,
`2*Cq + RPM*dCq/dRPM - J*dCq/dJ >= 0`.
Они покрывают внутренний grid и Clamp edges, обеспечивая монотонную нагрузку для
bisection. Карты с windmilling/отрицательным Q и нарушением условий явно отклоняются.
Это ограничение выбранного решателя, не утверждение, что такие режимы не существуют.
Карта + axial inflow/translational lift этапа 10 по-прежнему запрещены для защиты от
несогласованного наложения моделей. Flapping/H-force/ground effect остаются отдельными.

## RotorInertia — opt-in

В `rotors[].motor`:

| Поле | Смысл |
|---|---|
| `dynamicsModel` | Необязательное FirstOrder / RotorInertia; отсутствие = прежний FirstOrder |
| `rotatingInertiaKgM2` | Jr, kg·m², положительная **суммарная spin-axis инерция моторного ротора и винта** |
| `responseTimeUpS`, `responseTimeDownS` | Формируют запрос скорости прежним exponential lag; supply solver ограничивает выполнение |

Все роторы должны выбирать RotorInertia; смешение режимов отклоняется. Нужны Electrical
battery, motorResponse, электрические параметры и Jr. Простая батарея недостаточна
для нового динамического привода. Оценка Jr=5e-6 в fixture — синтетическая, не данные
модели mesh. Геометрия/масса модели не позволяют достоверно вычислить Jr двигателя.

Динамическое уравнение: `Jr*(omegaNext-omegaPrev)/dt = Mshaft-Q`.
Используется implicit midpoint: `omegaEval=(omegaPrev+omegaNext)/2`, Q берётся из
характеристик при omegaEval/J/rho. Та же omegaEval используется для T, H-force,
flapping и нового gyro moment. Состояние Omega хранит скорость **конца** шага.

Пассивная скорость находится из `Jr*(next-prev)/dt+Q((next+prev)/2)=0`.
При запросе остановки, disarm, fault, пустой батарее или недостаточной мощности ротор
не может замедляться быстрее пассивного coast. Нет активного торможения/рекуперации.
Если midpoint предполагал переход через нулевые обороты, действует positivity guard:
next=0, эффективный средний Q=Jr*prev/dt. Он учитывает остановку внутри шага и сохраняет
баланс энергии; обычные шаги используют Q(midpoint) без этой поправки.

Governor масштабирует прирост относительно coast: `next=coast+lambda*(desired-coast)`.
Это не мгновенное масштабирование накопленной скорости. При текущей высокой back EMF
и упавшем Vbus привод может свободно вращаться, пока positive motor torque недоступен.
Во время coast bus current=0; энергия уходит в аэродинамическую нагрузку винта.
Bearing friction остановленного привода отдельно не моделируется.

## Электричество и энергия

`Kt=60/(2*pi*Kv)`, `Iequivalent=I0+Mshaft/Kt` при активном приводе.
`Pmotor=(Kt*omegaEval + Iequivalent*Rmotor)*Iequivalent`.
Для проверки voltage envelope используется консервативный максимум скорости двух
концов шага. Motor current здесь — эквивалентный DC ток, **не** измеренный BLDC phase RMS
и **не** ток батареи. ESC efficiency переводит мощность на bus side.
Ограничения motor/ESC current, motor power, pack current, SOC charge-per-step, terminal
voltage сохраняются. Батарея использует прежний устойчивый low-current root P=I*(Voc-I*R).

`Erotor=.5*Jr*omega^2`, `Pshaft=Mshaft*omegaEval`.
Midpoint обеспечивает discrete identity:
`Pshaft = Q*omegaEval + (ErotorNext-ErotorPrev)/dt`.
Motor losses = Pmotor-Pshaft, ESC losses = Pmotor*(1/etaESC-1), battery loss = I²R.
CSV публикует spin balance residual отдельно; это численная диагностика, не физическое
тепловыделение. Reset намеренно обнуляет состояние и счётчики, не является физическим
торможением и начинает новый recording segment.

Эти балансы относятся к модели привода и осевого аэродинамического момента. Поправки
T/GE/flapping этапа 10 по-прежнему не определяют полноценную дополнительную power curve.
Полное энергетическое взаимодействие воздуха, корпуса и гибких лопастей не заявляется.

## Реакция корпуса и гироскопика

Spin sign `s=-ReactionSign`: CW=+1, CCW=-1 в принятом Unity-соглашении.
Реакция мотора на корпус: `Mreaction=ReactionSign*Mshaft*axis`, где
`Mshaft=Q + Jr*(next-prev)/dt`. Нельзя прибавлять одновременно старый Q torque и весь
новый Mshaft: Q уже включён. При пассивном coast без bearing torque реакция привода=0;
аэродинамический момент тормозит вращающийся винт, а не создаёт фиктивный motor torque.

`H=sum(s*Jr*omegaEval*axis)`; gyro moment на корпус `-cross(omegaBody,H)`.
Он ортогонален omegaBody и не создаёт мгновенной работы сам по себе. Противоположные
равные роторы гасят H, асимметричные RPM/оси/Jr могут создавать момент. Флаг
`gyroscopicRotorEffects` требует RotorInertia; выключение убирает только gyro, а не
энергию и обязательную реакцию при раскрутке. Lever-arm моментов от сил здесь нет;
их уже создаёт AddForceAtPosition. Rigidbody решает обычную динамику собственного тела.

Модель предполагает малую spin inertia относительно locked-body inertia и быстрые
роторы относительно body rates. Back-reaction углового ускорения корпуса на относительный
RPM не решается совместной mass matrix; это явное weak-coupling приближение.
Guard: сумма Jr <=5% минимального principal moment корпуса. Это **не процент точности**.
Профиль massProperties.inertia описывает весь аппарат с заблокированными роторами;
Jr нельзя повторно прибавлять к этому tensor. Полная динамика тяжёлых маховиков вне envelope.

Основы выбранных уравнений: maxon Motor data and simulation (DC back EMF, Kt, потери)
https://support.maxongroup.com/hc/en-us/articles/360013761160-Motor-data-and-simulation;
MIT OpenCourseWare, Angular Momentum and Motion of Rotating Rigid Bodies
https://ocw.mit.edu/courses/2-003sc-engineering-dynamics-fall-2011/pages/angular-momentum-and-motion-of-rotating-rigid-bodies/.
Midpoint solver, constraints и weak-coupling gate — реализация DroneLab, а не аппаратная
калибровка по этим источникам. Motor inductance, active braking, regen и thermal state
здесь отсутствуют; тепловая динамика — следующий этап.

## Проверка и CSV 1.2.0

1. Подтянуть ветку, запустить EditMode/PlayMode.
2. Terrain → **DroneLab → Test Bench → Module Checks → Rotor Inertia and Power**.
3. Назначить камере новый дрон, сохранить сцену, Play → F → H.
4. Проверить старт, удержание высоты и Fault. После disarm RPM/T угасают по нагрузке,
   ток становится нулевым. HUD показывает spin energy и gyro moment.
5. Для map-ветки назначить `quad_test_coupled_power_map` в Drone Profile. В ней axial/lift
   поправки выключены; карта сама описывает осевой поток. Общая среда — environment_final_acceptance.

На своём профиле: **DroneLab → Power → Create Profile with Battery** → Rotor inertia dynamics; выбрать Jr и optional gyro. Новый режим включает Electrical автоматически.
Тестовые коэффициенты и батарея оценочные. Fixed timestep рекомендуется 0.01 s.

CSV: `rpm` = конец шага, `force_rpm` = скорость расчёта сил; `advance_j` относится к
force_rpm. `reaction_nm` теперь содержит signed mount torque в RotorInertia; в FirstOrder
сохраняет signed Q. Новые rotor columns: propeller_q_nm, acceleration_q_nm (Jr*domega/dt,
без spin sign), spin_energy_j, motor_current_a, motor_loss_w, esc_loss_w, force_rpm.
Aggregate: propeller_power_w, spin_energy_rate_w, spin_balance_error_w, motor_loss_w,
esc_loss_w, gyro_x/y/z_nm. shaft_power_w включает раскрутку только в RotorInertia.
Неизвестная/неподдерживаемая величина остаётся пустой; физический ноль = 0.
Старые записи читаются по названиям столбцов. JSON сохраняет 1.0.0 с optional additions;
новый профиль требует source runtime/schema этапа 11, SDK snapshot 0.2.0 его не поддерживает.
