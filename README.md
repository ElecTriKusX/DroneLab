# DroneLab

Unity UAV simulation laboratory. Project version: Unity 6000.3.25f1, HDRP.

Начат модуль параметризованной физики мультироторного дрона. Чистое математическое
ядро отделено от Unity Rigidbody, клавиатуры и редактора.

- [SDK: перенос физики в другой Unity-проект](Docs/Physics/MODULE_INTEGRATION.md)
- [Этапы улучшения физики 9–14](Docs/Physics/PHYSICS_UPGRADES.md)
- [Импорт полного тензора инерции](Docs/Physics/INERTIA_IMPORT.md)
- [Итог восьми этапов, сверка с исследованием и ТЗ](Docs/Physics/PHYSICS_AUDIT.md)
- [Финальная совместная проверка всех поддерживаемых моделей](Docs/Physics/FINAL_ACCEPTANCE.md)
- [Запустить тестовый дрон на Terrain](Docs/Physics/QUICKSTART.md)
- [Этап 2: PID, Angle/Acro и геймпад](Docs/Physics/FLIGHT_CONTROL.md)
- [Этап 3: геометрия, силуэт mesh и поверхности](Docs/Physics/GEOMETRY_AERODYNAMICS.md)
- [Принятая физика, формулы, ограничения](Docs/Physics/SPECIFICATION.md)
- [Полный реестр параметров JSON](Docs/Physics/PARAMETERS.md)
- [План следующих этапов](Docs/Physics/ROADMAP.md)
- [Фактически выполненные проверки](Docs/Physics/VALIDATION.md)
- [Метровые ориентиры и поведение RPM](Docs/Physics/BEHAVIOR_BASELINE.md)

Контракт: `Tools/generate_physics_contract.py`. После его изменения выполнить:

```bash
python Tools/generate_physics_contract.py
dotnet test Tests/DotNet/DroneLab.Physics.Tests.csproj
```

Первый стенд использует синтетический TestQuad 1 kg. Расширенные эффекты сохранены
в контракте и roadmap; включение ещё не реализованных моделей явно отклоняется.

Характеристики винтов и CSV-импорт: [PROPELLER_PERFORMANCE.md](Docs/Physics/PROPELLER_PERFORMANCE.md).

Ground effect и rotor drag: [ROTOR_EFFECTS.md](Docs/Physics/ROTOR_EFFECTS.md).

Аккумулятор, ток, напряжение и ограничения моторов: [POWER_SYSTEM.md](Docs/Physics/POWER_SYSTEM.md).

Этап 7: атмосфера, порывы, турбулентность и пространственный ветер: [ENVIRONMENT.md](Docs/Physics/ENVIRONMENT.md).

Этап 8: CSV, отказ мотора и данные для VFX: [DIAGNOSTICS.md](Docs/Physics/DIAGNOSTICS.md).
