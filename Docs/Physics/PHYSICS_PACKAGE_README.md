# DroneLab Physics 0.3.4

`com.dronelab.physics` — параметрическое физическое ядро мультироторного аппарата и его Unity-адаптер. Чистый C# рассчитывает силы, моменты, роторы, питание и тепло; Rigidbody/PhysX интегрирует корпус и контакт.

JSON-контракт 1.0.0, физический CSV 1.4.0. Система координат Unity: +X вправо, +Y вверх, +Z вперёд; величины SI. Input System и HDRP для ядра не требуются.

Поддерживаются разные характеристики винтов, привод, сопротивление, атмосфера и ветер, приближённые роторные эффекты, энергетические/тепловые ограничения. Дополнительные модули включаются профилем. Оценочные коэффициенты не являются калиброванной моделью реального аппарата.

## Документация

Локальный раздел пакета находится в `Documentation~/README.md`. В репозитории доступны:

- [Физический раздел](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/README.md).
- [Уравнения и границы](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/PHYSICS_REFERENCE.md).
- [Величины](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/PHYSICAL_QUANTITIES.md) и [JSON-поля](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/PARAMETERS.md).
- [Установка и API](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/DEVELOPMENT.md).
- [Численная валидация](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/VALIDATION.md).
- [Источники](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/THIRD_PARTY.md).

Пакет экспортируется из `Assets/DronePhysics`; изменения вносятся в исходники и пересобираются. Внешний controller подаёт команды моторов; для тестовых стендов доступен `com.dronelab.demo`. Не устанавливайте экспорт поверх тех же классов в `Assets`.

Лицензия исходников — GPL-3.0-only; в пакете находится `LICENSE.md`, оригинал — [LICENSE репозитория](https://github.com/ElecTriKusX/DroneLab/blob/main/LICENSE). У внешних материалов сохраняются собственные условия.
