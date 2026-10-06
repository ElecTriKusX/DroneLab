# DroneLab Demo / Test Pilot — 0.3.3

`com.dronelab.demo` — управление, HUD, камера и стенды для проверки DroneLab Physics. Зависит от **com.dronelab.physics 0.3.3** и **Unity Input System 1.20.0**. Целевая версия Unity — 6000.3. Демонстрационный пульт является отдельным слоем: он не заменяет физические характеристики аппарата.

## Что реализовано

| Функция | Поведение |
|---|---|
| Angle | Заданный наклон → угловая скорость → angular acceleration через каскад PID |
| Acro | Стики/клавиши задают угловую скорость; отпускание не возвращает горизонт |
| Altitude Hold / H | Высота → вертикальная скорость → ускорение → collective с компенсацией наклона |
| PID | Фильтр производной, ограничение интеграла, conditional anti-windup и reset/transition |
| QuadAllocator | Распределяет collective/roll/pitch/yaw по четырём +Y роторам; приоритет collective и масштабирование torque при saturation |
| RPM inversion | Преобразует требуемую тягу обратно в command/RPM через профильную кривую; allocator использует статическую J=0 ветвь |
| Input | Клавиатура/геймпад, обнаружение смены устройства, потеря фокуса и disarm |
| HUD/камера | Физические показатели, цель высоты, предупреждения, следование за аппаратом |
| Editor Test Bench | Basic, Combined, Module Checks, Reference Drones и Propeller Bench |
| PlayMode | Изолированные physics scenes для проверки Rigidbody/контроллера/эффектов |

Пульт использует идеальное состояние Rigidbody. IMU/GPS/барометр, sensor latency/noise, EKF, firmware, radio link, маршрутный автопилот и удержание X/Z не реализованы. Произвольные rotor count/axes поддерживаются Physics, но не этим QuadAllocator.

## Быстрый запуск

1. Установить **оба** пакета из одного выпуска. В исходном проекте Assets-source уже содержит код — вторую копию UPM туда не устанавливать.
2. Создать сцену с камерой и поверхностью/collider.
3. **DroneLab → Test Bench → Combined Physics Drone**.
4. Назначить созданный дрон целевой камере, сохранить сцену.
5. Play → клик в Game View → F → H.

| Клавиша | Действие |
|---|---|
| F | Arm/disarm |
| WASD | Наклон в Angle; команды угловой скорости в Acro |
| Q/E | Yaw |
| Space/Ctrl при H | Поднять/опустить целевую высоту |
| Space без H | Фиксированная manual collective fraction; отпускание даёт нулевой газ |
| Z | Angle/Acro |
| H | Удержание высоты, без позиции |
| Backspace | Reset позы, привода и управления |

## Меню и профили

| Раздел Test Bench | Назначение |
|---|---|
| Basic Physics Drone | Минимальный аппарат для базового F/Angle/Acro/H |
| Combined Physics Drone | Этап 13: quad_test_descent_wind + environment_dryden_frozen, совместная проверка модулей |
| Module Checks | Отдельные Body and Battery, Rotor Airflow, Rotor Inertia and Power, Thermal |
| Reference Drones | Crazyflie 2.0, Crazyflie Brushless, Hummingbird — частичные профили открытых данных |
| Propeller Bench | Два APC 10×4.7 стенда; носитель синтетический, не дополнительная реальная модель дрона |
| Scale References | Визуальные размерные ориентиры; команда доступна из Physics |

Документы: [MENU](Documentation~/MENU.md), [QUICKSTART](Documentation~/QUICKSTART.md), [FLIGHT_CONTROL](Documentation~/FLIGHT_CONTROL.md), [REFERENCE_DRONES](Documentation~/REFERENCE_DRONES.md).

## Как читать HUD

Тяга — Н, момент — Н·м, скорости — м/с, RPM — об/мин, ток — А, мощность — Вт, температуры — К. Height target относится к world Y, environment altitude — к опорной высоте среды. Ни одна из этих строк сама по себе не является расстоянием до Terrain.

`Saturation` означает недостаточность доступного распределения тяги/моментов; `power limited` — ограничение питания; `thermal authority` — допустимую долю тепловой нагрузки; `fault` — отказ привода. Это разные причины.

`throttle 0%` при H — нулевой ручной ввод, не остановленные моторы. Цель высоты и наклон влияют на команду регулятора. При длительном Ctrl цель может оказаться ниже поверхности: автоматической посадки нет. Выключить/включить H, чтобы захватить текущую высоту.

**Почему скорость может расти:** reference-профили отключают неизвестное сопротивление корпуса и питание. В Acro наклон сохраняется, H компенсирует вертикальную часть тяги, горизонтальная часть продолжает разгонять аппарат. Нужен Combined либо отдельный полётный профиль с аэродинамическими параметрами. Это не доказательство реальной скорости Hummingbird/CF2. После возвращения к горизонту H не тормозит горизонтальную скорость контроллером.

У существующих сцен может остаться слишком малая Manual Collective Fraction. Для старого reference-стенда поставить 0.60 либо пересоздать; новые стенды используют 1.25/TW с ограничениями. Физический смысл — доля максимальной тяги пульта, а не API motor command (доля RPM).

## Подключение тестов

Установить Unity Test Framework. В проектном Packages/manifest.json, рядом с dependencies, добавить:

```json
"testables": ["com.dronelab.physics", "com.dronelab.demo"]
```

Существующий массив дополнить, не создавать второй ключ и не удалять другие пакеты. Найти файл: **DroneLab → Diagnostics → Show Project Manifest (package tests)**. Затем Window → General → Test Runner → EditMode/PlayMode → Run All. Tests/Runtime из пакета копировать в Assets не нужно.

0.3.3 включает пользовательский float-фикс `StepPhysics(.1f)` в DescentWindRigidbodyTests. Python/.NET suite проверяет упаковку/ядро, но не запускает Unity PlayMode. Runtime/EXE и импорт в чистый проект требуют отдельной проверки.

## Физика и разработка

Вся физика описана в [PHYSICS_REFERENCE](Documentation~/PHYSICS_REFERENCE.md), единицы — [PHYSICAL_QUANTITIES](Documentation~/PHYSICAL_QUANTITIES.md), реестр — [PARAMETERS](Documentation~/PARAMETERS.md). Для силовых моделей требуется Physics-пакет.

Исходники Demo: Assets/DronePhysics/Demo, PlayMode: Assets/DronePhysics/Tests/PlayMode. Экспорт — Distribution/Packages/com.dronelab.demo. Порядок разработки/выпуска: [DEVELOPMENT](Documentation~/DEVELOPMENT.md); установка: [MODULE_INTEGRATION](Documentation~/MODULE_INTEGRATION.md).

Лицензия — [GPL-3.0](LICENSE.md).
