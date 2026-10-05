# Этап 12: температура компонентов и погодные пресеты

2026-10-06, Asia/Yekaterinburg. Пользователь подтвердил тесты этапа 11.
Источник JSON-контракта — Tools/generate_physics_contract.py; схема сохраняет 1.0.0
с optional additions. CSV — 1.3.0. UPM snapshot 0.2.0 остаётся на этапе 9 до финальной
пересборки. Это документ реализации и границ модели, не аппаратной калибровки.

## Выбор модели

`powerSystem.thermalEnabled=true` включает нагрев, охлаждение и protection derating.
Отключённый/отсутствующий флаг сохраняет прежний расчёт. Требуется battery mode
Electrical и явные thermal nodes: battery.thermal, каждый motor.electrical.thermal и
motor.electrical.escThermal. Simple не даёт нужного эквивалентного сопротивления/тока
обмотки и с включённой thermal model отклоняется. FirstOrder и RotorInertia допустимы;
вращательная энергетика остаётся доступной только RotorInertia.

Каждый двигатель, его эквивалентный ESC и батарея имеют одну effective temperature.
Это не распределение температуры обмотки/корпуса/магнитов или junction/case. Контур
motor→housing→air и общий heatsink 4-in-1 ESC не моделируются. Тепловые ёмкости,
теплоотдача и thresholds требуют собственного источника и условий монтажа. В
синтетических профилях они помечены Estimated. Схема не превращает их в измерения.

| Параметр узла | Единицы / смысл |
|---|---|
| heatCapacityJPerK | J/K; effective C >0, можно оценить как mass × specific heat |
| heatTransferWPerK | W/K; base G, приближённо 1/Rth для выбранного монтажа |
| airflowHeatTransferWPerKPerMps | W/K/(m/s); empirical linear cooling coefficient |
| maxAirSpeedMps | m/s; предел скорости для cooling correction, до 200 |
| initialTemperatureK | K; 150..500, явное начальное состояние, не автоматически воздух |
| derateStartTemperatureK | K; температура начала ограничения токов |
| cutoffTemperatureK | K; должна строго превышать derateStart |

Интегратор: `C dT/dt = Ploss - G(T-Ta)`;
`G = Gbase + Gair × min(|Vpoint-Vwind|, Vmax)`.
Локальная скорость мотор/ESC берётся в точке соответствующего ротора. Для батареи —
COM. Rotor point speed включает вращение корпуса. Это эффективная convection
аппроксимация, а не универсальная корреляция теплоотдачи; mounting, shielding,
ambient density, radiation, humidity и отдельный propwash в G не вычисляются.
Коэффициенты действуют только в явно заданном envelope. Считать базовый G нужно
для оговорённых условий, не складывая повторно уже учтённое охлаждение.

За шаг Ploss/G/Ta считаются постоянными. Используется точное экспоненциальное
решение линейного узла с time constant C/G. При G=0: ΔT=Ploss × dt/C. Малые G×dt/C
считаются устойчивым рядом. Обмен теплом может быть отрицательным: более тёплый
воздух нагревает отключённый компонент. Температура не обрезается произвольным
clamp, который удалял бы энергию. Численные невалидные запросы отклоняются.
Ambient envelope: 100..500 K; initial/protection inputs 150..500 K.

## Потери и обратная связь

Нагрев использует уже рассчитанные потери, без второй покупки энергии у батареи:

- Motor: `Pmotor - Mshaft × omegaEvaluation` — winding/equivalent no-load losses.
- ESC: `Pmotor × (1/etaESC - 1)`.
- Battery: `Ibus² × Rpack`.

BatteryLoss соответствует той же Rpack, что voltage sag; при отключённом
batteryVoltageSag Rpack runtime равна 0, поэтому её Joule heating также 0.
No-load current и motor loss остаются DC/BLDC equivalent estimates, не измеренным
phase RMS. Они не задают отдельную bearing friction/coast loss. При отсутствии
электрического drive в существующей модели motor/ESC heat generation=0; механическое
затухание винта расходует spin energy в воздух, не нагревает узел мотора ещё раз.

Тепловая зависимость сопротивления обмотки:
`Rmotor(T) = Rreference × [1 + alpha × (T - Treference)]`.
Rreference — существующий motorResistanceOhm; Treference и alpha заданы явно в
motor.electrical. alpha=0 отключает зависимость. Проверяется положительное R во
всём допустимом ambient envelope; R обновляется в voltage constraint и copper loss.
Kv/Kt, магнитная деградация и ESC efficiency от температуры не меняются. Для
LiPo Rpack(T), OCV(T), cold capacity и ageing нужны отдельные cell curves: линейный
медный alpha к батарее не применяется.

Protection factor `f=clamp((Tcutoff-T)/(Tcutoff-Tstart),0,1)` действует на:

- допустимый motor current и, если задан, motor maxPower;
- допустимый ESC current отдельно;
- pack maxDischargeCurrent отдельно.

Это выбираемая controller/ESC protection policy, не универсальный закон потери тяги.
Нет произвольного множителя T. На силы влияют новые допустимые обороты/моменты и
сопротивление. Shared-scale governor этапа 11 сохранён: один тесный активный лимит
может ограничить все оставшиеся driven rotors. Если локальный motor/ESC current cap
не превышает no-load current, данный мотор отключён от drive и coasts; остальные
моторы этим не блокируются. Полный cutoff батареи выключает electrical drive всех.

RotorInertia сохраняет вращение/тягу и gyro при thermal shutdown, пока винты
пассивно замедляются. В FirstOrder caller по-прежнему передаёт MotorStep coast;
Unity adapter переключает target=0 для thermally disconnected drive. Manual drive
fault не заменяется температурой; HasDriveFault и ThermalDerated — разные признаки.
Охлаждение продолжается disarmed. Recovery автоматическая по непрерывному f, без
защёлки аварии и hysteresis. Пульт не гарантирует recovery/управляемый полёт при
перегреве одного мотора. ResetMotorState сбрасывает также temperature/history к JSON
initial state, не обязательно к температуре воздуха.

## Порядок шага и энергетическая проверка

1. Sample ambient/flow. Использовать температуры начала шага для R/current caps.
2. Resolve speed/load/supply; trials не меняют temperature, SOC или energy.
3. Prepare thermal candidates по итоговым loss powers.
4. После успешных force queries Commit(dt) обновляет SOC и thermal nodes один раз.
5. Записать telemetry; затем Rigidbody интегрирует движение.

Thermal-enabled Commit требует успешный Resolve и тот же dt; повторный commit и
failed/invalid query не дают дополнительного нагрева/расхода. Воздействие температуры
на токи явно разбито по шагам, поэтому около protection threshold dt convergence
обязательна; recommended dt=0.01 s. Это не nonlinear continuous thermal solver.

Для каждого узла: generatedEnergy = storedEnergyChange + rejectedEnergy.
Накопленная storedEnergyJ = C × (Tend-Tinitial). Thermal energy — перераспределение
уже учтённых motor/ESC/pack losses. Нельзя прибавлять её к bus_energy_j ещё раз.
ChemicalEnergy = TerminalEnergy + pack Joule loss energy сохраняется. Это не полный
баланс всех aero corrections/wake/внутренней химии; ограничения этапов 10–11 остаются.

## Погода для команды UI/карты

Environment weather — optional `{precipitation, intensityMmPerHour, model}`.
precipitation: None/Rain/Snow/Hail; интенсивность — liquid-water equivalent mm/h,
0..200; None требует 0, остальные положительную интенсивность. Единственный
допустимый model — **VisualOnly**: осадки не создают дополнительных forces/heat.
Поля доступны через RuntimeEnvironment и active environment JSON для внешнего VFX.
Изменения temperature/pressure/wind действуют через существующие модели воздуха,
а не через название weather. Осадки не включают скрыто влажность или изменение rho.

| Resource preset | Температура на уровне моря | Средний ветер | Осадки |
|---|---|---|---|
| environment_thermal_warm | 30°C | (2,0,0) m/s | None |
| environment_thermal_snow | −20°C | (2,0,0) m/s | Snow, 2 mm/h |
| environment_thermal_rain | 15°C | (4,0,1) m/s | Rain, 10 mm/h |
| environment_thermal_hail | 10°C | (4,0,1) m/s | Hail, 10 mm/h |

Это лабораторные presets с dry StandardAtmosphere, gust/turbulence/seed, не прогноз
погоды и не нормы интенсивности. Precipitation VFX выполняет команда карты. Water
impacts, icing/ice mass, wet propeller performance, hail collisions/damage,
evaporation/latent heat и humidity требуют отдельных проверенных моделей/данных.
Нет случайного уменьшения тяги по rain checkbox. До визуальной интеграции это API
и presets, а не готовый погодный экран/дождь в сцене.

## Проверка в Unity

1. Pull ветки; запустить EditMode/PlayMode. Новые Unity-сценарии изолированы.
2. Выделить Terrain → **DroneLab → Diagnostics → Create Thermal Acceptance Drone**.
3. Назначить новый дрон камере; сохранить сцену → Play → F → H.
4. HUD показывает °C мотор/ESC/батарея, current-cap factor и thermal derated. В обычном
   quad_test_thermal зависание не должно немедленно вызывать перегрев.
5. Stop Play → в Drone Profile выбрать **quad_test_thermal_stress** → Play → F/H.
   Artificial thermal capacities/low thresholds показывают motor derating за десятки
   секунд; удержание высоты может стать невозможным. Это намеренно ускоренный тест.
6. F выключает drive; RPM затухают, температуры охлаждаются постепенно. Backspace
   сбрасывает температуры и историю к initial state. Температура после reset =20°C.
7. По очереди назначить Environment Profile presets warm/snow/rain/hail. Температура
   воздуха и скорость ветра влияют на heating/cooling; само название осадков — только
   metadata. Visuals автоматически не создаются.

Для своего профиля: выделить Drone root → **DroneLab → Power → Create Profile with
Thermal Model**. Нужен уже Electrical battery profile; у Constant environment явно
задать temperatureK=293.15 либо выбрать погодный/атмосферный preset. Editor задаёт
°C и переводит в JSON K, сохраняет копию и сохраняет существующие геометрию, battery,
motor, aero/gyro настройки. Дефолты thermal editor оценочные, airflow cap=25 m/s.
Battery editor теперь сохраняет существующий battery thermal node.

CSV 1.3.0: rotor motor_temp_k_end/esc_temp_k_end, thermal_authority_end,
motor_resistance_ohm_end; aggregate ambient_temp_k/battery_temp_k_end/authority_end,
thermal_derated_end, thermal_generated_j_end/rejected/stored, precipitation/mmph.
R/current/power рассчитаны с температурами начала шага; `*_end` — состояние для
следующего шага. Unknown thermal state у старых профилей пустой, не фиктивные 0 K.
Analyzer читает текст precipitation, сохраняет старые CSV и выводит max temperatures,
derated samples и max thermal energy balance residual.

## Primary sources и границы

- MIT 6.622 Power Electronics, Lecture 16, pp.6–8: thermal RC/lumped capacitance,
  transient response и границы spatial lumping:
  https://ocw.mit.edu/courses/6-622-power-electronics-spring-2023/1BYB3SGJU-Oi1Vk910805d4bCCzBI3NWZ_transcript.pdf
- maxon Product Range 2026/27, p.94: температура обмотки, монтаж/теплоотдача,
  winding resistance coefficient copper 0.0039/K:
  https://online.flippingbook.com/view/1042987/94/
- maxon Continuous operation range of BLDC: Joule и speed-dependent iron losses,
  тепловые пределы определяются конкретным motor/design, не нашим demo threshold:
  https://support.maxongroup.com/hc/en-us/articles/360000350014-Continuous-operation-range-of-BLDC-EC-Motors

Это основы модели, не свидетельство точности наших estimated coefficients. Политика
linear derating, общий governor и empirical airflow G — конкретная реализация DroneLab.
Тепловой runaway, chemistry, fire/damage и water/ice physics не реализованы. Следующий
этап — descent/wind envelope и независимые численные проверки. Пакеты — в конце.
