# DroneLab Physics Packages

Параметризованная физика мультироторных аппаратов для Unity: силы и моменты роторов,
аэродинамика, ветер, питание, нагрев и телеметрия. Движение корпуса и столкновения
интегрирует Rigidbody/PhysX; параметры аппарата и среды задаются отдельными JSON.

**SDK 0.3.3 · JSON 1.0.0 · CSV 1.4.0 · Unity 6000.3.25f1 · Python 3.10+ · .NET 8.**
Windows — основная платформа разработки, Linux поддерживается инструментами и CI.
Разработческий проект использует HDRP; Physics/Demo не зависят от HDRP.

## Состав

| Пакет | Реализовано |
|---|---|
| `com.dronelab.physics` | Pure C# ядро, Rigidbody-адаптер, JSON-валидация, authoring/import, атмосфера/ветер, питание/тепло, CSV/fault, EditMode-тесты |
| `com.dronelab.demo` | Angle/Acro/H, каскадные PID, QuadAllocator, клавиатура/геймпад, HUD/камера, стенды и PlayMode-тесты |

Physics поддерживает произвольное число роторов и их оси. Demo поддерживает четыре
параллельных ротора +Y. Источник SDK — `Assets/DronePhysics`; UPM-экспорт —
`Distribution/Packages`. В одном Unity-проекте используется одна копия SDK.

## Реализованная физика

| Область | Модели |
|---|---|
| Механика | Масса/COM, AutoBox/ManualPrincipal/CAD-тензор, плечи сил, реактивные моменты, гравитация |
| Винт/привод | OmegaSquared, CtCq, RPM-таблицы, RPM/J-карты, FirstOrder, RotorInertia, выбег, энергия вращения и гироскопические моменты |
| Аэродинамика | Квадратичный drag корпуса, Box/manual/mesh projected area, pressure surfaces, rotor drag, bounded ground effect, axial/lateral corrections и blade flapping |
| Среда | Constant/StandardAtmosphere, местный ветер, Gust, Turbulence, CustomField, DrydenFrozen, диагностика снижения и дискретизации |
| Питание/тепло | SOC/OCV, просадка напряжения, Simple/Electrical motor/ESC, governor токов/мощности/RPM, тепловые узлы, обдув и derating |
| Диагностика | Моторные отказы, CSV 1.4.0, энергетические балансы, operating envelope, weather/VFX metadata |

Дополнительные эффекты активируются профилем. Единицы SI; +X вправо, +Y вверх,
+Z вперёд; root/parents scale=1. Motor command — доля RPM, manual throttle — доля тяги.

## Документация

| Документ | Назначение |
|---|---|
| [PHYSICS_REFERENCE](Docs/Physics/PHYSICS_REFERENCE.md) | Все реализованные формулы, ограничения и возможные расширения физики |
| [PHYSICAL_QUANTITIES](Docs/Physics/PHYSICAL_QUANTITIES.md) | Величины, единицы, коэффициенты и связь с JSON |
| [PARAMETERS](Docs/Physics/PARAMETERS.md) | Полный генерируемый реестр JSON-полей |
| [DEVELOPMENT](Docs/Physics/DEVELOPMENT.md) | Установка, запуск, API, authoring, диагностика, Windows/Linux, границы погодной интеграции |
| [VALIDATION](Docs/Physics/VALIDATION.md) | Профили, контрольные сравнения, точность и результаты тестов |
| [THIRD_PARTY](Docs/Physics/THIRD_PARTY.md) | Источники и атрибуция внешних данных |

## Контрольные сравнения и текущие ограничения

**Статический APC 10×4.7: среднее согласование по тяге 99,64% на семи независимых
контрольных точках.** Метрика — `100% × (1 − средняя относительная ошибка)`.
Для осевого APC — 99,17% на положительной ветви; для CF2 — 96,69%.
Это характеристики винта в условиях стенда, а не точность полной траектории.
Подробные условия, максимальные ошибки и источники — в VALIDATION.

Reference-профили содержат известные параметры открытых источников. Неизвестные
body-drag/power/thermal параметры отключены. Известное ограничение: наклонённый
reference-дрон без сопротивления продолжает горизонтальный разгон; H удерживает
только высоту. Для совместной проверки модулей предназначен Combined Physics Drone.

CFD/BEMT, wake interaction, dynamic inflow/VRS forces, гибкость/разрушение, электрохимия,
датчики/EKF и маршрутный автопилот отсутствуют. Точность полного реального полёта
пока не подтверждена синхронизированными измерениями.

## Запуск и пересборка

Unity: создать сцену с поверхностью/collider → **DroneLab → Test Bench → Combined
Physics Drone** → назначить дрон камере → Play → Game View → F → H.
WASD — наклон, Q/E — yaw, Space/Ctrl — высота, Z — Angle/Acro, Backspace — reset.

Из корня репозитория:

```console
python Tools/build_unity_packages.py
python Tools/build_unity_packages.py --check
python -m unittest discover -s Tests/Python -v
dotnet test Tests/DotNet/DroneLab.Physics.Tests.csproj --configuration Release
```

На Linux допускается `python3`. Проверенный выпуск 0.3.3: на Windows и Ubuntu прошли
18 Python и 482 C# теста, package verification и генерация контракта.
Unity import/PlayMode/EXE требуют отдельного запуска в Unity.

Разработка Physics/Demo — `physics-packages`; погодная интеграция —
`envieroment-packages`. Границы ответственности описаны в DEVELOPMENT.
Лицензия кода: [GPL-3.0](LICENSE). Изменения выпуска: [CHANGELOG](CHANGELOG.md).
