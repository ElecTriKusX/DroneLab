# Документация DroneLab

[Главная страница проекта](../README.md)

Документы организованы по задачам. Общий README описывает проект; подробные инструкции и справочники находятся ниже.

## Пользователю

1. [Запуск приложения и проекта](GETTING_STARTED.md).
2. [Обзор реализованных возможностей](FEATURES.md).
3. [Интерфейс](UI/README.md): [главное меню](UI/MAIN_MENU.md), [конфигуратор](UI/DRONE_CONFIGURATOR.md), [сцены и окружение](UI/SCENARIOS_AND_ENVIRONMENT.md), [полёт и маршруты](UI/FLIGHT_VIEWS_F1_F2.md).
4. [Датчики и измерения](SENSORS.md).
5. [Запись, экспорт и анализ](EXPORT_AND_ANALYSIS.md).
6. [Решение проблем](TROUBLESHOOTING.md).

## Разработчику

| Документ | Содержание |
|---|---|
| [DEVELOPMENT.md](DEVELOPMENT.md) | Форк, окружение, расширение приложения и сборка |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Слои, данные, связи между подсистемами |
| [TESTING.md](TESTING.md) | Автоматические проверки, команды, ручная приёмка |
| [UI/SIMULATION_LAUNCH.md](UI/SIMULATION_LAUNCH.md) | Подключение сцены и точки старта |
| [DOCUMENTATION.md](DOCUMENTATION.md) | Назначение файлов и установка комплекта документации |

## Физика

Точка входа — [Physics/README.md](Physics/README.md). Из неё доступны:

- [PHYSICS_REFERENCE.md](Physics/PHYSICS_REFERENCE.md) — уравнения, модели и границы применимости.
- [PHYSICAL_QUANTITIES.md](Physics/PHYSICAL_QUANTITIES.md) — обозначения, единицы и смысл величин.
- [PARAMETERS.md](Physics/PARAMETERS.md) — сгенерированный реестр JSON-полей.
- [VALIDATION.md](Physics/VALIDATION.md) — источники профилей и численные результаты.
- [DEVELOPMENT.md](Physics/DEVELOPMENT.md) — физическое ядро, контракты и UPM-пакеты.
- [THIRD_PARTY.md](Physics/THIRD_PARTY.md) — происхождение внешних данных и атрибуция.

## Хакатон и развитие

[HACKATHON.md](HACKATHON.md) сопоставляет функции с требованиями кейса и предлагает сценарий демонстрации. [ROADMAP.md](ROADMAP.md) описывает следующие направления: CFD, датасеты, другие платформы и прикладные миссии.
