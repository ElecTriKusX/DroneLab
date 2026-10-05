# Финальная сквозная проверка физики

Эта проверка проверяет совместную работу реализованных моделей и альтернативные
ветви. Она не заменяет bench/flight calibration реального дрона.

## Основной профиль

- Drone: `Assets/DronePhysics/Resources/DronePhysics/quad_test_final_acceptance.json`.
- Environment: `Assets/DronePhysics/Resources/DronePhysics/environment_final_acceptance.json`.
- Синтетический quad: 1 kg, 0.4×0.1×0.4 m, плечи ±0.14 m, четыре 0.127 m винта.
- Manual principal inertia (0.014, 0.026, 0.018) kg·m², повёрнутые главные оси.
- CtCq, exponential motor lag, ProjectedArea directional LUT и CP +0.01 m по Y.
- Rotor drag, bounded ground effect, 4S/1.3 Ah Electrical, OCV/SOC/sag и I/P limits.
- StandardAtmosphere, mean wind +X 2 m/s, bounded turbulence peak 0.4 m/s,
  gust overlay, 3 s timescale, seed 48271.

LUT этого стенда аналитически получен для коробки, не измерен по настоящему FPV mesh.
Коэффициенты винтов/drag/батареи синтетические. Unsupported flapping/induced/gyro
выключены; «все модули» означает все совместимые реализованные эффекты.

## Создание и ручная приёмка

1. Открыть тестовую сцену с Terrain и выбрать Terrain/объект возле старта.
2. `DroneLab → Diagnostics → Create Final Acceptance Drone`.
   Создаётся `DroneLab Final Acceptance` с профилями, pilot, recorder, manual fault
   scenario и Physics Markers. Камеру подключить к этому новому корню.
3. Отключить другие активные тестовые дроны; у нового Automatic Fault выключен.
   В существующей PhysTest этот флаг сохранён включённым (через 5 s), поэтому для
   baseline с исправными моторами его надо выключить. Сохранить сцену. Physics root scale (1,1,1).
4. Play → клик Game View → F → H. Наблюдать 15–20 s; зафиксировать Mode/Armed/H.
   H держит Y, но ветер может уносить аппарат по X/Z — это ожидаемо.
5. Подлететь к земле (без контакта collider), затем подняться дальше 50 радиусов винта
   от поверхности (>3.175 m для 5 inch). У поверхности gain>1, вдали gain=1;
   при H controller меняет RPM. У самой поверхности контакт может влиять на движение.
6. A/D, W/S, Q/E → отпустить. Проверить знаки наклона/yaw и торможение вращения;
   в Acro отпускание не означает возврат к горизонту. F/H/B — по QUICKSTART/FLIGHT_CONTROL.
7. В Inspector выбрать `DroneMotorFaultScenario` → component context menu `Inject selected motor fault`.
   Один привод перестаёт питаться, RPM затухает с TauDown, возникает несбалансированный
   момент. После этого устойчивость/удержание высоты не являются acceptance requirement.
8. Backspace reset → повтор F/H. Fault очищен, SOC/clock сброшены; CSV имеет новый segment.
9. Stop Play → `DroneLab → Diagnostics → Open Flight Recordings Folder`.
   Проверить flight.csv, manifest.json, drone_profile.json и environment_profile.json.
   Recorder записывает по умолчанию; при Play reset файл делится на segments, не теряется.
10. Для теста ограничений скопировать профиль и уменьшить maxDischargeCurrentA до 4 A;
    ожидать ограничения RPM/взлёта. Для discharge испытания уменьшить capacityAh и
    наблюдать SOC/OCV/available RPM; не требовать заданного времени без measured hardware.

Перед сравнениями вернуть исходный профиль и initial state. Не сохранять fault или
ограничения случайно в общий профиль. Профиль и условия каждой записи входят в manifest.

## Автоматическая приёмка

Unity Test Runner → EditMode → все тесты, затем PlayMode → все DroneLab tests.
Новые группы: `FinalAcceptanceTests` и `FinalAcceptanceRigidbodyTests`.
PlayMode использует отдельные local PhysicsScene и ручной fixed stepping;
реальная Terrain-сцена и фокус Game View не влияют на эти новые проверки.

| Проверка | Что доказывает |
|---|---|
| Supported profile + allocator | Совместимость layout, DTO/schema/runtime и enabled effects |
| Budget at 50/100/200 Hz | Motor lag + CtCq + atmosphere + GE + electrical power: Qω, terminal/loss баланс, charge integral, hover thrust; wind drag dissipative |
| Alternative model cases | RpmTable/constant density, PerformanceMap/atmosphere, Simple battery, Surfaces+Electrical не потеряны за основной комбинацией |
| Coupled hover + CSV + fault | Реальный Unity Rigidbody, controller, ground probes, оба drag, CP moment, SOC, CSV и fault/reset в одном сценарии |
| Inertia pulse | Начальный ω-отклик на известный torque соответствует повёрнутому I⁻¹ с допуском 2% |
| Symmetric powered descent | Исправный симметричный quad при недостаточной тяге снижается без искусственного torque/VRS |
| Scene marker edit / inverted JSON | Marker не меняет snapshot; JSON axis меняет силу; pilot явно отклоняет inverted layout |

Полное покрытие alternative forces/interpolation/contacts/input/recording формирует
существующая suite, а не один «волшебный» полёт. Полный прогон обязателен.
Два ignored `WindowsInput_*MouseMovements*` из Unity InputSystem — отдельные tests пакета
с пометкой `Unstable due to 1252825`, не ошибка DroneLab.

Здесь выполнены 284 Core/EditMode test cases в .NET/NUnitLite и 4 Python tests;
Unity-движка здесь нет. Пять новых PlayMode scenarios ожидают вашего запуска.
Точное текущее состояние проверок: [VALIDATION.md](VALIDATION.md).

## Для передачи команде

HUD читает физическую телеметрию, а не вычисляет собственную тягу/заряд.
Загрузка из UI проходит через ProfileLoader и явный Initialize до нового полёта;
редактирование поля Inspector/marker во время Play не является hot reload.
Схемы и список supported models нужны конфигуратору. Карта предоставляет colliders,
world-origin/масштаб, terrain height и опционально IWindProvider.
Новые погодные/сенсорные значения должны иметь самостоятельную модель и единицы.
Маркеры в финальном стенде помогают authoring; в EXE путь задаётся runtime FreeCam UI,
предложенным в [PHYSICS_AUDIT.md](PHYSICS_AUDIT.md), а не Unity Editor.
