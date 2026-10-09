# DroneLab Demo 0.3.4

`com.dronelab.demo` — тестовый пилот, ввод, HUD/камера и регрессионные стенды для Unity. Зависимости: Physics 0.3.4 и Input System 1.20.0.

Angle/Acro и удержание высоты реализованы каскадными PID. MultirotorAllocator поддерживает управляемые схемы с ≥4 параллельными +Y роторами; для четырёх сохраняется QuadAllocator. Контроллер использует физическое состояние Rigidbody.

## Запуск стенда

В сцене с collider выберите **DroneLab → Test Bench → Combined Physics Drone**, назначьте дрон камере и запустите Play с фокусом Game View. F включает моторы, Space подаёт тягу, H включает высоту; WASD задаёт наклон, Q/E — yaw, Z переключает Angle/Acro, Backspace сбрасывает тело.

Reference Drones и Propeller Bench предназначены для контрольных задач. Некоторые reference-профили имеют отключённое неизвестное сопротивление/питание; их нельзя использовать для неподтверждённой оценки максимальной скорости или длительности.

## Границы пакета

Demo является стендом физики. Полные меню, галерея, инженерные экраны, оборудование и маршрутные функции находятся в приложении DroneLab; установка Demo не добавляет их автоматически. H удерживает высоту, а не горизонтальную позицию. Полноценный firmware-автопилот и компенсация любых отказов не поставляются.

## Документация

В пакете физический раздел находится в `Documentation~/README.md`. В репозитории доступны [установка и API](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/DEVELOPMENT.md), [формулы](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/PHYSICS_REFERENCE.md), [параметры](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/PARAMETERS.md) и [валидация](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/Physics/VALIDATION.md).

Test Runner пакетов включается через `testables` проекта; PlayMode запускается только в Unity. Лицензия — GPL-3.0-only, файл `LICENSE.md` пакета и [LICENSE репозитория](https://github.com/ElecTriKusX/DroneLab/blob/main/LICENSE).
