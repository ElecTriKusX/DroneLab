# DroneLab Demo 0.3.4

`com.dronelab.demo`: тестовый controller, ввод, HUD/камера и регрессионные стенды.
Зависимости: Physics 0.3.4 и Input System 1.20.0; Unity 6000.3.

Angle/Acro/H реализованы каскадными PID и QuadAllocator для четырёх +Y роторов.
Используется идеальное состояние Rigidbody; датчики/EKF, маршрутный автопилот,
удержание X/Z и гарантированное восстановление после отказа отсутствуют.

## Запуск

Сцена с collider → DroneLab → Test Bench → Combined Physics Drone → назначить
дрон камере → Play → Game View → F → H. WASD — наклон/rate, Q/E — yaw,
Space/Ctrl при H — высота, Z — Angle/Acro, Backspace — reset.
Reference Drones — частичные реальные профили; Propeller Bench — APC-стенды.

Известное ограничение reference-профилей: без drag наклонённый аппарат продолжает
разгоняться. H удерживает только высоту; throttle 0% в HUD показывает ручной ввод.
Для совместного теста модулей предназначен Combined Physics Drone.

## Документация

- [DEVELOPMENT](Documentation~/DEVELOPMENT.md): установка, управление, тесты и архитектура.
- [PHYSICS_REFERENCE](Documentation~/PHYSICS_REFERENCE.md): реализованные модели и ограничения.
- [PHYSICAL_QUANTITIES](Documentation~/PHYSICAL_QUANTITIES.md): единицы.
- [PARAMETERS](Documentation~/PARAMETERS.md): JSON-поля.
- [VALIDATION](Documentation~/VALIDATION.md): профили и контрольные результаты.

Статические контрольные точки APC: среднее согласование тяги **99,64%** (1−MAPE).
Процент не относится к точности controller или траектории.
Тесты установленных пакетов включаются через testables проектного Packages/manifest.json;
PlayMode запускается только в Unity. Источник Demo — Assets/DronePhysics/Demo,
PlayMode — Assets/DronePhysics/Tests/PlayMode. Лицензия — [GPL-3.0](LICENSE.md).
