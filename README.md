# DroneLab Environment Packages

Ветка `envieroment-packages` предназначена для интеграции окружения с физикой дронов.
Добавлен адаптер Enviro 3: выбор погодного профиля, ветер и живой воздух для DroneLab Physics.
Windows — основная платформа разработки; сборщики и математические тесты поддерживают Linux.
Unity 6000.3.25f1, HDRP, Python 3.10+, .NET 8.

## Состав

| Компонент | Ответственность |
|---|---|
| `Assets/Enviro 3 - Sky and Weather` | Импортированный сторонний продукт: небо, освещение, облака, погодные профили, сезон/время, эффекты |
| `Assets/DroneEnvironment` | Код DroneLab: неизменяемое состояние среды, перевод единиц, адаптер ветра/воздуха, API управления и окно диагностики |
| `Assets/DronePhysics` | Силы/моменты, аэродинамика, ветер, привод, аккумулятор, тепло, телеметрия; SDK 0.3.4 |
| Demo | Контроллер Angle/Acro/H, управление, HUD, камера и стенды; SDK 0.3.4 |
| `Distribution/Packages` | Экспорт Physics, Demo и Environment; Enviro в экспорт не включается |

Разработка силового ядра продолжается в `physics-packages`. Адаптер среды находится
в этой ветке, отдельно от чужих исходников. API живого воздуха и атмосферная математика находятся в Physics; Enviro-контроллер —
в Environment. Экспорт этой ветки содержит Physics/Demo 0.3.4 и Environment 0.2.0. Контроллер остаётся отдельным Demo-пакетом,
доступным для совместной проверки полёта в этой ветке.

## Реализовано

- `WeatherSnapshot`: ветер в мировых осях, температура K, доли wetness/snow и время снимка.
- `EnviroWeatherController`: профили, плавный/мгновенный переход, ручной ветер, время суток,
  передача ветра/воздуха через `IWindProvider` и `IAirProvider` без переинициализации дрона.
- `AtmosphereColumn`: локальные температура/давление, высота, плотность сухого воздуха;
  общий образец для сил, привода и теплообмена. CtCq/PerformanceMap поддерживают live density.
- Меню **DroneLab → Environment → Connect Selected Drone to Enviro**: проверка профиля,
  копия JSON с `CustomField`, назначение провайдера и сохранение остальных параметров среды.
- UPM `com.dronelab.environment` 0.2.0, исходные `.meta`, проверка SHA-256,
  тесты осей/единиц/границ и переносимости экспорта.

Enviro `windSpeed` — число 0…1, а не м/с. Полная шкала по умолчанию 20 м/с задаётся
адаптером и требует осознанной настройки. Передаётся однородный горизонтальный ветер.
Температура берётся из Enviro, давление задаётся явно на контрольной высоте, плотность
рассчитывается как `rho=p/(R*T)`. До первого снимка используется JSON; при ошибке —
последний валидный снимок. Осадки — явно привязанные metadata/VFX, без дополнительных сил.
Интернет-провайдер погоды ещё не подключён.

## Запуск

1. Добавить настроенный Enviro prefab, назначить камеру, включить Weather/Environment.
2. **DroneLab → Test Bench → Combined Physics Drone**.
3. Выделить дрон → **DroneLab → Environment → Connect Selected Drone to Enviro** →
   сохранить новую копию JSON и сцену.
4. Play → окно **DroneLab Weather / Wind** → профиль или Manual wind override.
   Для полёта: Game View → F → H; WASD — наклон, Q/E — yaw, Space/Ctrl — высота.

Подробные единицы, API, порядок исполнения, ограничения и установка UPM:
[README адаптера](Assets/DroneEnvironment/README.md).

## Проверки и пересборка

```console
python Tools/build_unity_packages.py
python Tools/build_environment_package.py
python Tools/build_unity_packages.py --check
python Tools/build_environment_package.py --check
python -m unittest discover -s Tests/Python -v
dotnet test Tests/DotNet/DroneLab.Physics.Tests.csproj --configuration Release
dotnet test Tests/Environment/DroneLab.Environment.Tests.csproj --configuration Release
```

На Linux допускается `python3`. CI выполняет эти проверки на Windows и Ubuntu.
Математические тесты не запускают Unity: импорт, визуальные переходы и влияние ветра
на Rigidbody требуют отдельной проверки в редакторе.

## Дальнейшая разработка

1. Реальные метеоданные: координаты, единицы, обновление, кэш и работа без сети.
2. Полное меню условий, физические порывы, запись состояния среды и Unity-проверки.

## Документация физики

| Документ | Содержание |
|---|---|
| [PHYSICS_REFERENCE](Docs/Physics/PHYSICS_REFERENCE.md) | Законы, реализованные модели, ограничения и возможные расширения |
| [PHYSICAL_QUANTITIES](Docs/Physics/PHYSICAL_QUANTITIES.md) | Величины, единицы и коэффициенты |
| [PARAMETERS](Docs/Physics/PARAMETERS.md) | Генерируемый JSON-контракт |
| [DEVELOPMENT](Docs/Physics/DEVELOPMENT.md) | Установка, API физики, контроллер, стенды и диагностика |
| [VALIDATION](Docs/Physics/VALIDATION.md) | Проверки, профили и контрольные сравнения |
| [THIRD_PARTY](Docs/Physics/THIRD_PARTY.md) | Источники физических данных |

Статический APC 10×4.7: **99,64% среднего согласования тяги на семи независимых
контрольных точках**. Это стендовая характеристика винта; точность полной траектории
и погодного адаптера этой метрикой не подтверждается.

Код DroneLab: [GPL-3.0](LICENSE). Enviro — отдельный сторонний продукт со своей лицензией.
