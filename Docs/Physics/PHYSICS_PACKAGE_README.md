# DroneLab Physics 0.3.4

`com.dronelab.physics`: параметризованная мультироторная физика Unity 6000.3.
Pure C# ядро вычисляет силы, моменты, RPM, питание и тепло; Rigidbody/PhysX интегрирует
корпус и столкновения. JSON 1.0.0; телеметрия CSV 1.4.0. Input System/HDRP не требуются.

Реализованы масса/COM/инерция, OmegaSquared/CtCq/RPM/RPM-J, отклик и инерция привода,
gyro, drag корпуса/роторов, ground/flow/flapping corrections, атмосфера/ветер/Dryden,
батарея/governor, тепловые узлы/derating, faults и диагностика снижения.
Эффекты активируются профилем; оси +X вправо/+Y вверх/+Z вперёд, единицы SI.

## Документация

| Документ | Содержимое |
|---|---|
| [PHYSICS_REFERENCE](Documentation~/PHYSICS_REFERENCE.md) | Все формулы, ограничения, возможные расширения |
| [PHYSICAL_QUANTITIES](Documentation~/PHYSICAL_QUANTITIES.md) | Величины и единицы |
| [PARAMETERS](Documentation~/PARAMETERS.md) | Полный реестр JSON |
| [DEVELOPMENT](Documentation~/DEVELOPMENT.md) | Установка/API/authoring, Windows/Linux, погодный адаптер |
| [VALIDATION](Documentation~/VALIDATION.md) | Профили и контрольные сравнения |
| [THIRD_PARTY](Documentation~/THIRD_PARTY.md) | Источники/атрибуция |

Статические контрольные точки APC согласуются по тяге в среднем на **99,64%**
(1−MAPE). Это стендовое сравнение винта, а не точность полного полёта.
Reference-профили имеют известные ограничения: неизвестные drag/power/thermal
параметры отключены; реалистичная максимальная скорость и длительность не подтверждены.

Источник — Assets/DronePhysics ветки physics-packages; этот пакет — экспорт.
Команды моторов подаются внешним controller. Для ручного тестирования доступен
com.dronelab.demo; произвольные схемы требуют собственного allocator.
Documentation~ исключена из импорта Unity. Лицензия — [GPL-3.0](LICENSE.md).
