# DroneLab Physics — 0.3.3

`com.dronelab.physics` — параметризованная физика мультироторного аппарата для Unity 6000.3. Pure C# ядро вычисляет силы, моменты, RPM, питание и температуры; Rigidbody/PhysX интегрирует движение корпуса и столкновения. Управление задаётся внешним controller: Input System и DroneLab Demo этому пакету не нужны.

Версии: **package 0.3.3 / drone+environment JSON 1.0.0 / CSV 1.4.0**. Source-проект: Unity 6000.3.25f1. Пакет не требует HDRP, карты или стороннего mesh.

## Карта документации

| Вопрос | Документ в пакете |
|---|---|
| Вся физика, все формулы, простое объяснение и ограничения | [PHYSICS_REFERENCE](Documentation~/PHYSICS_REFERENCE.md) |
| Физические величины, единицы, коэффициенты и входы/выходы | [PHYSICAL_QUANTITIES](Documentation~/PHYSICAL_QUANTITIES.md) |
| Полный список JSON-полей | [PARAMETERS](Documentation~/PARAMETERS.md) |
| Контракт, оси и совместимость моделей | [SPECIFICATION](Documentation~/SPECIFICATION.md) |
| Установка и runtime API | [MODULE_INTEGRATION](Documentation~/MODULE_INTEGRATION.md) |
| Изменение/выпуск пакетов на Windows/Linux | [DEVELOPMENT](Documentation~/DEVELOPMENT.md) |
| Что подтверждено тестами/измерениями | [VALIDATION](Documentation~/VALIDATION.md), [REFERENCE_DRONES](Documentation~/REFERENCE_DRONES.md) |

Documentation~ не импортируется как Unity-assets; откройте эти Markdown-файлы в текстовом редакторе.

## Реализованные модели

| Модель | Параметры / результат | Физический смысл |
|---|---|---|
| Твёрдое тело | Масса, COM, главные моменты/оси инерции, силы, moments, colliders | Движение и вращение корпуса; AutoBox или ManualPrincipal/CAD import |
| Геометрия роторов | Положение, единичная ось, CW/CCW | Тяга в заданной точке, плечо r×F, реактивный yaw-момент |
| OmegaSquared | kT/kQ, RPM | `T=kT ω²`, `Q=kQ ω²` |
| CtCq | Ct/Cq, ρ, диаметр | `T=Ct ρ n² D⁴`, `Q=Cq ρ n² D⁵` |
| RpmTable | RPM→T/Q, measured current | Интерполяция стендовой кривой; ток таблицы остаётся отдельной телеметрией |
| PerformanceMap | RPM/J→Ct/Cq | Билинейная карта осевого потока с Clamp/Reject и явными границами |
| FirstOrder | τup/τdown | Экспоненциальная раскрутка/замедление |
| RotorInertia | Jr, electrical drive, propeller load | Энергия раскрутки, выбег, midpoint load, reaction корпуса |
| Rotor gyro | Угловой импульс роторов | Дополнительный момент `−Ω×H` |
| AxisApproximation | Cd и площади по осям | Квадратичное сопротивление по local X/Y/Z |
| ProjectedArea | Cd, Box или manual/mesh LUT | Квадратичное сопротивление с directional silhouette |
| Surfaces | Нормали, позиции, площади, Cd | Двусторонние pressure patches и их моменты; не lifting surfaces крыла |
| Rotor drag | Kr, RPM, боковой поток | Линейная по боковому потоку тормозящая сила ротора |
| Ground effect | h/R, коэффициент/cap, collider probes | Ограниченный прирост положительной тяги у поверхности |
| Rotor flow corrections | Axial/lateral коэффициенты, reference density, caps | Translational lift, axial correction и blade flapping; эмпирические модели |
| Constant / StandardAtmosphere | g, ρ или T/p/reference altitude | Среда с постоянной плотностью или сухая тропосфера |
| Wind | Constant/Gust/Turbulence/CustomField/DrydenFrozen | Местный ветер в точках корпуса и роторов |
| Battery Simple / Electrical | OCV, SOC, R, capacity, currents, Kv/Kt, losses | Просадка, расход заряда, нагрузка моторов/ESC и совместное ограничение RPM |
| Thermal | Cth, cooling/airflow, температуры и пороги | Нагрев/охлаждение, Rmotor(T), current derating мотора/ESC/батареи |
| Motor fault | Authority отдельного drive | Деградация/отказ и пассивный выбег; не обещание контролируемого recovery |
| Descent envelope | Axial/lateral speeds и hover inflow estimate | Только report-only диагностика снижения/границ, без VRS force |
| Weather | Температура/ветер и precipitation metadata | VFX-интеграция; дождь/снег/град не создают дополнительные силы |

Полный справочник содержит также формулы интерполяции, gyro/energy balance, спектров Dryden, теплообмена и governor. Некоторые сочетания моделей отклоняются loader: в частности, измеренные kT/RPM tables требуют reference density, а совместный battery+RPM/J solver требует разрешённую монотонную нагрузку и Clamp. Не отключайте validation ради принятия несовместимого профиля.

## Единицы и оси

| Данные | Единица |
|---|---|
| Масса, координаты, диаметр, площадь | кг, м, м, м² |
| Инерция | кг·м² |
| Скорость воздуха/ветра/аппарата | м/с |
| Тяга, момент, мощность, энергия | Н, Н·м, Вт, Дж |
| RPM / внутренняя угловая скорость | об/мин / рад/с |
| Плотность, давление, температура | кг/м³, Па, К |
| Напряжение, ток, сопротивление, ёмкость | В, А, Ом, А·ч |
| Cd/Ct/Cq/J/SOC | безразмерные |

Unity +X вправо, +Y вверх, +Z вперёд. Root/parents scale=1, профильные координаты уже в метрах. Скорость потока — скорость точки с вращательной частью **минус местный ветер**. Wind не прикладывается как произвольная одинаковая сила ко всем дронам.

## Минимальное подключение

1. Установить пакет из Git/distribution. Dependencies в package.json задают Newtonsoft и стандартные Unity modules.
2. Создать GameObject с Rigidbody, Collider и DronePhysicsBody. Назначить drone/environment TextAssets. Модель добавить дочерним визуальным объектом.
3. Проверять `Initialize(...)`/`IsReady` и ошибки ProfileLoader. Во время полёта используется валидированный snapshot, не live-перестановка authoring markers.
4. Внешний controller вызывает `SetArmed(bool)` и `SetMotorCommand(index, value)` до шага физики. Команда 0…1 — доля целевых **RPM**, не мощности/тяги.
5. UI/VFX читает параметры и телеметрию, не применяет повторно силы/заряд.

Пример обращения к готовым ссылкам:

```csharp
if (body.Initialize(droneAsset, environmentAsset))
{
    body.SetArmed(true);
    body.SetMotorCommand(0, 0.5); // половина max RPM данного ротора
}
```

Это пример API, не полноценный controller: нужно распределить команды всем роторам. Physics поддерживает их произвольное число, но штатный Demo allocator рассчитан только на четырёхроторную схему.

При обычном использовании StepPhysics вызывается адаптером из FixedUpdate. При внешнем пошаговом стенде сначала `AutomaticSimulation=false`, затем `StepPhysics(float dt)`; не запускать одновременно автоматический и ручной шаг. Рекомендуемый начальный Fixed Timestep — 0.01 с с последующей проверкой сходимости; SDK не меняет глобальный шаг тайно.

## Профили и отключённые эффекты

Модули задаются `physicsConfiguration.modules`. Среди них motorResponse/bodyDrag/windInteraction/groundEffect/rotorAerodynamics/bladeFlapping/inducedDrag/batteryDischarge/batteryVoltageSag/motorElectrical/gyroscopicRotorEffects. Тепло включается отдельно через `powerSystem.thermalEnabled`; режим привода — `rotors[].motor.dynamicsModel`.

Три reference-аппарата являются частичными bench/model-профилями. Неизвестные сопротивление, электроника и тепло там выключены. Наклонённый аппарат без drag не имеет реалистичной предельной скорости. Combined Physics Drone использует синтетический профиль с включёнными модулями; его coefficients тоже не измеренные свойства конкретного FPV.

## Тесты и ограничения

EditMode-код находится в Tests/Editor. Для installed package добавить `com.dronelab.physics` в testables проектного Packages/manifest.json и установить Test Framework. PlayMode-проверки Rigidbody находятся в отдельном Demo-пакете. Само наличие Tests не означает, что они запущены в вашей Unity.

Нет CFD/BEMT, wake interaction, dynamic inflow/VRS сил, гибких лопастей/рамы, разрушения, электрохимии, датчиков/EKF или firmware. Геометрия не определяет автоматически массу/Cd/Ct/Kv. Численные regression и propeller bench comparisons не доказывают абсолютную точность полной траектории.

Разработка идёт в Assets-source ветки physics-packages; этот пакет — экспорт. Не менять его copy/cache как единственные исходники. Лицензия — [GPL-3.0](LICENSE.md), внешние данные — [THIRD_PARTY](Documentation~/THIRD_PARTY.md).
