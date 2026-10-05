# Проверка физики и управления

Дата: 2026-10-04.

| Проверка | Результат |
|---|---|
| Чистое ядро C#, компиляция Roslyn с C# 8 | Успешно |
| NUnit 3.14: 31 тестовый случай | 31 passed, 0 failed |
| Newtonsoft.Json 13.0.2 (версия, используемая официальным Unity-пакетом) | Тесты выполнены с этой версией |
| JSON Schema Draft 2020-12: проверка самих схем | Успешно |
| Basic, CtCq, Calm, Wind fixtures по JSON Schema | 4 профиля успешно проверены |
| Компиляция и импорт исходного этапа в Unity 6000.3.25f1 | Пользователь подтвердил работу проекта и физики |
| 4 Unity Play Mode теста: hover, free fall, wind, motor spin-down | Добавлены; здесь не запускались |
| Ручной полёт с моделью из ассета / Terrain | Пользователь подтвердил работу; сообщил об ощущении масштаба и равных RPM |
| Одноосевой yaw: 50/100/200 Hz, motor lag, allocator | Установившаяся скорость 70.000°/s, RPM spread <0.001; тесты прошли |
| Одноосевой Angle hold | 20.000°, RPM spread ≈0.0005; тест прошёл |
| Yaw braking и ускорение при tilt 20° | Тесты прошли; начальное ускорение ≈3.57055 m/s² |
| Ещё 3 Play Mode теста реального пульта/масштаба | Добавлены; здесь не запускались |
| Импорт новых ориентиров и новой телеметрии в Unity | Требуется запуск у пользователя |

Обычный `dotnet test` в текущем контейнере остановился до сборки из-за ошибки среды
`System.Diagnostics.Process.GetStat/GetProcessName`. Для фактической проверки те же
Core-файлы и EditMode-тесты напрямую скомпилированы Roslyn и выполнены через NUnitLite
на .NET 8. Код проекта для обхода этой ошибки не менялся. В обычной среде команда
`dotnet test Tests/DotNet/DroneLab.Physics.Tests.csproj` использует стандартный test adapter.

Прохождение математических тестов не подтверждает импорт ассетов, UI, физический
интегратор Unity или настройку конкретной сцены. Первый этап считается проверенным
полностью после успешного запуска Unity-тестов и сценариев из QUICKSTART.md.

## Этап 2 — 2026-10-04

| Проверка | Фактический результат |
|---|---|
| Roslyn C# 8, чистое ядро + все EditMode исходники | Компиляция успешна |
| NUnitLite 3.14 / .NET 8 / Newtonsoft 13.0.2 | 47 passed, 0 failed; 16 новых случаев |
| Production rate PID + allocator + motor lag, 50/100/200 Hz | Через 5 s: 70.497–70.499°/s; торможение прошло |
| Attitude PID, наклон и выравнивание, 50/100/200 Hz | Прошло, точность ±0.5° после 8 s |
| Height + vertical velocity PID с motor lag, 50/100/200 Hz | Цель 2 m → 1.9998–2.0001 m после 12 s |
| Постоянный внешний момент, anti-windup, D kick, reset | Прошло |
| Allocator при насыщении | Сохраняет collective и направление torque, commands в 0…1 |
| Unity PlayMode: ещё 3 сценария пульта и 2 теста ввода | Добавлены; не запускались здесь |
| Импорт этапа 2 и полёт у пользователя | 2026-10-05: пользователь подтвердил |

Подробности: [FLIGHT_CONTROL.md](FLIGHT_CONTROL.md). Одноосевые модели не заменяют
проверку трёхмерной динамики и столкновений в Unity. Существующие 31 случая сохраняются
как физическая регрессия, в том числе прежний P-пульт для сравнения с PID.

## Подтверждение этапа 2 и этап 3 — 2026-10-05

Пользователь подтвердил ручные сценарии, EditMode без ошибок и полный повторный
PlayMode-прогон. На первом запуске HoverHasNoTranslationOrRotation показал 0.0969805 m/s,
HeldYawTracksRateAndReleaseBrakes — RPM spread=0. Повторные запуски прошли.
По исходникам вероятны лишний шаг gravity до Initialize и focus-triggered disarm;
это гипотезы, здесь исходные сбои не воспроизведены.
Стенды теперь работают в отдельных local physics scenes с явными шагами и теми же
допусками. Обычный полёт сохраняет FixedUpdate и disarm при потере фокуса.

| Проверка этапа 3 | Фактический результат |
|---|---|
| Roslyn C# 8: Core + EditMode исходники, NUnitLite 3.14, .NET 8 | 78 passed, 0 failed; 31 новый случай |
| Box mesh silhouette по осям и диагоналям, sphere против πR² | Прошло, допуск 2.5% при raster resolution=128 |
| Дубли/перекрытия triangle mesh, scale², translation, ±direction | Прошло |
| LUT: положительность, диапазон, симметрия, exact samples, непрерывность | Прошло |
| CP speed с вращением, момент r×F, энергия pressure patches | Прошло |
| Прежний Axis drag и отключение bodyDrag | Прошло |
| Валидация conditional fields, normals, ID и sample axis duplicates | Прошло |
| Новые JSON presets: Projected box/LUT, Surfaces | Загрузка и семантическая валидация прошли |
| Полный JSON Schema Draft 2020-12, jsonschema | Обе схемы и 7 профилей прошли |
| Roslyn syntax check всех 34 C# файлов DronePhysics | 0 синтаксических ошибок; не заменяет Unity-компиляцию |
| 15 Unity PlayMode случаев, включая 3 новых | Добавлены/обновлены; здесь не запускались |
| Импорт новых скриптов, маркеры, mesh bake и экспорт в Unity | Ожидается у пользователя |

Настройка и границы модели: [GEOMETRY_AERODYNAMICS.md](GEOMETRY_AERODYNAMICS.md).

## Этап 4 — 2026-10-05

Пользователь подтвердил Box/Mesh export и работу физики этапа 3. Присланный
`drone_geometry_mesh.json` дополнительно проверен текущим loader и QuadAllocator:
принят, 4 ротора, 13 LUT образцов, статический T/W=4. Полный новый Unity Test Runner
прогон этапа 3 не заявлен.

| Проверка этапа 4 | Фактический результат |
|---|---|
| Roslyn C# 8: Core + EditMode, .NET 8, NUnitLite 3.14, Newtonsoft 13.0.2 | 118 passed, 0 failed; 40 новых случаев |
| RPM knots, линейные силы/момент/ток, явный нулевой узел | Прошло |
| Map knots, bilinear RPM/J, singleton RPM, signed Ct/Cq | Прошло |
| Density scaling только Ct/Cq, table reference density | Прошло |
| Clamp/Reject, motor max coverage, отсутствие extrapolation | Прошло |
| Immutable snapshot, валидация сеток/дублей/optional current | Прошло |
| SI CSV: сортировка, ru-RU locale, неверные заголовки/NaN/пропуски | Прошло |
| Нелинейный allocator: T/Q, saturation, unequal rotors, COM offset | Прошло |
| RPM-table rate PID + motor lag: 50/100/200 Hz, yaw и торможение | Прошло |
| Full JSON Schema Draft 2020-12 | Обе схемы и 10 профилей, включая присланный mesh JSON, прошли |
| Roslyn syntax всех 40 C# DronePhysics файлов | 0 syntax errors; не заменяет Unity-компиляцию |
| 5 новых Unity PlayMode тестов (20 всего) | Добавлены; здесь не запускались |
| Unity импорт/CSV окно/полёт с таблицами и картами | Ожидается у пользователя |

JSON-контракт остаётся 1.0.0. Генератор DTO обновлён: optional currentA теперь double?,
чтобы отсутствие измерения отличалось от 0 A; JSON-поля, schema и единицы не изменены.
Ограничения физики и пульта: [PROPELLER_PERFORMANCE.md](PROPELLER_PERFORMANCE.md).

## Подтверждение этапа 4 и этап 5 — 2026-10-05

Пользователь сообщил, что полёт и тесты этапа 4 прошли. Скриншот H показывает
Alt hold=true, Saturation=false, vy=0.00 m/s, tilt/yaw=0, сумму T≈9.81 N для 1 kg;
Vhorizontal≈0.49 m/s не противоречит отсутствию position hold. Разные RPM согласуются
с геометрией и статическим nonlinear allocator. Файл Assets/drone_performance.json
из нового коммита пользователя также принят loader и QuadAllocator: 4 ротора, 13 LUT, T/W=4.

| Проверка этапа 5 | Фактический результат |
|---|---|
| Roslyn C# 8: Core + EditMode, .NET 8, NUnitLite 3.14, Newtonsoft 13.0.2 | 165 passed, 0 failed; 47 новых случаев |
| Ground multiplier, height/normal/range clamps и smooth fade | Прошло |
| No surface / stopped / windmilling, static capacity/Q не меняются | Прошло |
| Rotor drag: sign, axes, RPM/K scaling, zero/axial flow | Прошло |
| Point velocity, offset COM, rotational damping, energy dissipation | Прошло, включая 500 воспроизводимых velocity/rotation случаев |
| Ground height PID + motor lag, 50/100/200 Hz | Прошло |
| Rate PID с постоянным rotor drag, 50/100/200 Hz | Прошло; постоянный RPM spread компенсирует torque, отпускание тормозит |
| Совместимость RPM-таблиц / CtCq-карт, disabled flags, immutable snapshot | Прошло |
| Full JSON Schema Draft 2020-12 | Обе схемы и 14 профилей прошли, включая 2 профиля пользователя |
| Roslyn syntax всех 45 C# файлов | 0 syntax errors; не заменяет Unity-компиляцию |
| 10 новых PlayMode тестов (30 всего) | Добавлены; здесь не запускались |
| Terrain, nearest hit, self/trigger filtering, buffer growth, layer mask, slope | Проверяются новыми PlayMode сценариями; фактический запуск ожидается у пользователя |
| Editor profile window / новый полёт на Terrain | Ожидается у пользователя |

Контракт 1.0.0 не менялся. Flapping/induced/gyroscopic modules по-прежнему отклоняются.
Ground gain не корректирует Q/current; исходные характеристики должны быть free-air.
Модель и click-by-click инструкция — [ROTOR_EFFECTS.md](ROTOR_EFFECTS.md).
