# Этап 6: аккумулятор и моторная энергетика

## Проверить на своей модели

1. Подтянуть `codex/physics-foundation`, дождаться компиляции Unity и обновления пакетов.
2. Остановить Play. Выбрать **корень дрона** с DronePhysicsBody.
3. **DroneLab → Power → Create Profile with Battery**.
4. Drone root должен указывать на этот корень. Для первой проверки оставить
   **Electrical motor model выключенным**: это режим Simple.
5. Capacity Ah = **1.3**, Initial SOC = **1**, Pack resistance = **0.06**,
   Pack current limit = **80**. Нажать **Validate and save new battery JSON**.
6. Сохранить как `drone_battery.json`. Окно назначает профиль дрону. Сохранить сцену.
7. Play → кликнуть Game View → **F → H**. В HUD появятся Battery, SOC, OCV, bus,
   ток, потребление W/mAh, RPM authority и power limited.
8. Наблюдать около 30 секунд. При висении ток положительный, SOC уменьшается,
   напряжение bus ниже OCV, used mAh растёт. На текущем профиле ожидается
   power limited=false. Разный ток CSV и расчёт батареи допустим: это разные источники.
9. **F** выключает моторы: ток батареи становится 0, SOC перестаёт уменьшаться;
   механический остаточный RPM затухает. **Backspace** сбрасывает стенд и восстанавливает
   Initial SOC — это явный тестовый reset батареи.

Для проверки ограничений сохранить ещё один профиль с **Pack current limit = 5 A**.
Запустить с ним: power limited=true, RPM authority<100%, подъёмная сила снижается.
H не гарантирует висение, если доступной мощности недостаточно.
Для быстрого разряда можно использовать Capacity Ah = **0.01**, сохранив отдельный профиль.

Для Electrical повторить шаги с включённым **Electrical motor model**, сохранив новый файл.
Геометрия, масса, характеристики винтов и ground effect сохраняются. Уже заданные
motor.electrical сохраняются. Если их нет, окно вставляет явно обозначенные demo defaults.
Переключение профилей выполняется до Play; горячей перезагрузки нет.

Это 4S demo: cellCount=4, nominalVoltageV=14.8, OCV(SOC) =
`(0,12),(.2,14),(.8,15.6),(1,16.8)` V **для всего пакета**.
Моторы demo: Kv=900 RPM/V, R=0.08 Ω, I0=0.4 A, maxCurrent=30 A,
maxPower=400 W, motorEfficiency=0.85, escEfficiency=0.95, escMaxCurrent=35 A.
Это не измерения скачанного дрона. Редактор здесь создаёт профиль для проверки физики;
полноценное заполнение характеристик остаётся будущей задачей UI.

## Контракт и модели

JSON 1.0.0 не менялся. `powerSystem.battery.mode` выбирает None, Simple или Electrical.
Для активной батареи обязательны cellCount, nominalVoltageV, capacityAh, initialSoc,
internalResistanceOhm, maxDischargeCurrentA и ocvCurve; для каждого ротора — motor.electrical.
OCV: 2..256 точек, SOC строго возрастает от 0 до 1, напряжение положительно и не убывает.
Число ячеек — метаданные; OCV не умножается на cellCount.

| Флаг | Поведение |
|---|---|
| batteryDischarge | Уменьшает SOC; выключенный флаг оставляет источник с фиксированным SOC, но ток/энергия считаются |
| batteryVoltageSag | Включает сопротивление батареи; выключенный флаг означает Rbattery=0 |
| motorElectrical | Должен быть true для Electrical, false для Simple |
| Все battery flags=false, mode=None | Сохраняет прежнюю физику, батареи в runtime нет |

Флаги батареи при mode=None отклоняются. InitialSoc=0 означает пустую батарею даже
при выключенном discharge; фиксированный источник требует InitialSoc>0.
Необязательная motor.electrical.maxPowerW при отсутствии не ограничивает мощность.

### Simple

Для ненулевых оборотов каждого ротора:

`Pshaft=Q(ω) ω`

`Pmotor=Pshaft/motorEfficiency + nominalVoltageV noLoadCurrentA`

`Pbus=Pmotor/escEfficiency`

No-load loss здесь оценён при номинальном напряжении. Модель без Kv/R мотора;
эти поля сохраняются, но электрические потери Simple определяет через КПД и I0.
Нормированный RPM=1 относится к nominalVoltageV: доступный верхний предел
`maxOmega × min(1,Vbus/nominalVoltageV)`.
maxCurrentA и escMaxCurrentA в Simple ограничивают **оценённый ток батарейной стороны
данного ротора**, потому что фазный ток в этом режиме неизвестен.

### Electrical

Эквивалентная steady-state DC-модель для BLDC/ESC, а не коммутация трёх фаз:

`Kt=60/(2π motorKvRpmPerVolt)` в N·m/A и V/(rad/s).

`Imotor=noLoadCurrentA + Q(ω)/Kt`

`Vrequired=Kt ω + Imotor motorResistanceOhm`

`Pmotor=Vrequired Imotor`, `Pbus=Pmotor/escEfficiency`.

Проверяются Vrequired≤Vbus, Imotor≤min(maxCurrentA,escMaxCurrentA),
Pmotor≤maxPowerW. Rmot и I0 уже учитывают потери; motorEfficiency здесь **не применяется**,
чтобы не учитывать их второй раз. Это оценка эквивалентного моторного тока;
Rmot должна быть идентифицирована под эту модель, не подставляется произвольное
межфазное сопротивление из несогласованных измерений. Battery-side rotor current
равен Pbus/Vbus; он отличается от Imotor из-за PWM и потерь ESC.

### Батарея и ограничение RPM

Суммируем Pbus всех моторов. При заданных Voc=OCV(SOC) и Rbattery:

`Pbus=I(Voc-I Rbattery)`.

Выбирается устойчивый малый корень:
`I=2 Pbus / (Voc+sqrt(Voc²-4 Rbattery Pbus))`.
При R=0 это I=Pbus/Voc. Vbus=Voc-I Rbattery, Vbus≥Voc/2.
Отрицательный discriminant означает невозможную нагрузку, которая должна уменьшиться.

После обычного motor lag governor проверяет напряжение, ток пакета/моторов,
мощность и остаток заряда на шаг. При перегрузке подбирает общий множитель RPM 0..1
за 48 итераций бисекции. Он сохраняет отношение RPM, но снижает collective и torque:
это отдельный power limited, не allocator saturation. PID прекращает интегрирование
при power limited предыдущего шага. Обнуление или clamp RPM — квазистационарное ограничение,
не физически вычисленное торможение ротора через его инерцию.

`SOCnext=max(0,SOC-I dt/(3600 capacityAh))`.
При включённом discharge I дополнительно ограничивается оставшимся зарядом / dt.
Если остаток не обеспечивает даже no-load power на шаг, governor останавливает моторы;
малый неиспользуемый SOC может остаться. Пустой пакет не создаёт тягу.
При disarm расчёт потребления=0, прежний motor lag даёт свободное затухание.
Потребление бортовой электроники пока не введено.

Шаг атомарный для сил: все запросы характеристик проходят до AddForce; затем
подтверждаются RPM и расход заряда ровно один раз. OCV/bus/current в HUD относятся
к началу последнего шага, SOC — к его концу; погрешность отображения порядка dt.
Проверяемый баланс: `Voc I = Vbus I + I² Rbattery`.
Сохраняются terminal/chemical energy в J, consumedAh и оценённые rotor current.

## Совместимость и границы

- OmegaSquared, CtCq и RpmTable поддержаны. Для таблицы Q должен не убывать по RPM,
  чтобы power envelope был монотонным и бисекция определённой.
- RPM/J PerformanceMap с батареей и непустая efficiencyCurve пока отклоняются с
  явной ошибкой: требуют решения немонотонного coupled flow/power. Без батареи
  карты продолжают работать, поля efficiencyCurve можно хранить как исходные данные.
- currentA из RPM CSV не используется для разряда: нет контрактного напряжения,
  при котором этот ток измерен. HUD отличает CSV I от estimated bus current.
- Ground effect усиливает T при прежнем Q. На меньших RPM для висения Qω снижается;
  произвольный дополнительный множитель тока/мощности не введён.
- Нет regenerative charging, inductance, switching, motor thermal, rotor kinetic
  energy или химической динамики. Mechanical rotor inertia остаётся зарезервированным
  полем; нельзя объявлять текущий расход точным на быстрых переходах.
- maxTotalThrust/TW — free-air capacity исходного профиля, а не обещание тяги при
  текущем заряде/лимитах. Чтобы зависнуть, пульт должен иметь достаточный power envelope.
- Модель OCV/R и DC-уравнения: [MathWorks Battery Equivalent Circuit](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html)
  и [DC Motor](https://www.mathworks.com/help/simscape-electrical/ref/dcmotor.html).
  Выбранные ограничения governor и demo coefficients — реализация DroneLab, не
  скопированный validated BLDC solver или паспортные данные.

## Тесты

В **Window → General → Test Runner** выбрать сборку **DroneLab.Physics.PlayModeTests**
и выполнить её. Новые PowerRigidbodyTests создают собственные local physics scenes,
Terrain тестовой сцены не используется. PilotInputTests теперь используют официальный
InputTestFixture: отдельный Input System, ручной update, восстановление исходного
состояния после теста и предварительное включение edge tracking свежих кнопок.
Ссылки TestFramework/testables добавлены по [документации Unity](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/set-up-test-assemblies.html).
Особенность первого wasPressedThisFrame описана в
[ButtonControl](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.Controls.ButtonControl.html).
Новая сцена вручную не нужна. Фактический повтор input-тестов ожидается у пользователя.

Core/EditMode проверяют ток, энергию, SOC/OCV, overload, reset, несовместимые
настройки, таблицы/CtCq, snapshot, 500 случайных команд на каждый режим и сходимость
50/100/200 Hz. Результаты запуска — VALIDATION.md; Unity здесь не запускалась.

## Расширение этапа 11

Ограничения выше описывают исходный FirstOrder governor. RPM/J maps теперь допускаются
с Clamp и monotone nonnegative Q. Optional RotorInertia включает spin energy, passive
coast, current/voltage-limited acceleration и новый mount/gyro torque. Точные формулы
и различие sampling RPM end/midpoint: COUPLED_POWER.md. Старые профили сохраняют FirstOrder.


Этап 12 добавляет optional powerSystem.thermalEnabled для Electrical: отдельные
thermal nodes motor/ESC/battery, потери → нагрев, cooling, Rmotor(T), thermal current
limits. Старые модели/JSON остаются без thermal. Commit требует успешный Resolve с
тем же dt и выполняется один раз. Formula/units/presets — THERMAL_WEATHER.md.
