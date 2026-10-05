# DroneLab

Unity UAV simulation laboratory. Project version: Unity 6000.3.25f1, HDRP.

Начат модуль параметризованной физики мультироторного дрона. Чистое математическое
ядро отделено от Unity Rigidbody, клавиатуры и редактора.

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
