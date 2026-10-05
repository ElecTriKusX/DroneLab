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

## Проверка этапа 5 у пользователя и исправление теста — 2026-10-05

Пользователь сообщил: все EditMode прошли; в PlayMode повторяемо не прошёл только
WindCreatesSeparateBodyAndRotorDragForces. Unity верно выдала 0.673749983 N, но тест
ожидал 0.6125 N: в ожидаемой величине был пропущен CdX=1.1 из quad_test_rotor_drag.json.
Правильная независимая оценка корпуса: 0.5 × 1.225 × 1.1 × 0.04 × 5² = 0.67375 N.
Rotor drag отдельно составляет 4 × 0.0001 × (5000 × 2π/60) × 5 ≈ 1.047197551 N.
Исправлено только ожидание PlayMode; допуск 1e-6 сохранён. Физика и профиль не менялись.

Добавлен EditMode WindFixtureProducesIndependentBodyAndRotorDrag: загружает тот же
профиль/ветер, проверяет обе силы, их направления и отсутствие суммарного момента.
Фактический запуск Core + EditMode через Roslyn/NUnitLite: **166 passed, 0 failed**.
Syntax check всех 45 C# файлов: 0 ошибок. Исправленный PlayMode здесь не запускался;
необходим повтор этого теста в Unity у пользователя.

На двух скриншотах H активен, Saturation=false, сумма тяги≈9.81 N, вертикальная
скорость≈0. При высоте роторов 0.850 m ground gain округляется до 1.000; при 0.152 m
составляет 1.011, RPM немного ниже при той же тяге. Это согласуется с R=0.0635 m
и формулой ground effect. Новый Assets/drone_rotor_effects.json принят текущим
loader и QuadAllocator: 4 ротора, 13 LUT, T/W=4.

## Подтверждение этапа 5, input isolation и этап 6 — 2026-10-05

Пользователь сообщил, что после исправления ожидаемого Cd все тесты прошли.
Однако KeyboardMapsSignsActionsAndZeroThrottleOnRelease и
GamepadMapsTwoSticksTriggerAndModeButtons иногда проваливались по bool действий,
после повторов проходили. Поэтому input-тесты переведены с общей Input System state
на официальный InputTestFixture: отдельный runtime, ручной update, полное восстановление
состояния и предварительная регистрация wasPressedThisFrame свежих ButtonControl.
Документация Unity допускает пропуск быстрых нажатий при первом чтении этого свойства;
это согласуется со сбоем, но здесь исходный сбой и исправление в Unity не воспроизведены.
Physics/Rigidbody тесты уже используют отдельные local physics scenes; новая сцена
вручную не нужна. Ссылки на первичные источники и TestFramework setup — POWER_SYSTEM.md.

Первый новый скриншот: rotor height≈0.035 m, gain=1.206, суммарная тяга≈7.00 N,
vy=0, WorldY≈0.03 m. Это контакт с землёй: опора поддерживает часть веса, не свободное
висение. Второй: height≈0.167 m, gain=1.009, сумма T≈9.81 N, vy=0, RPM выше;
согласуется со свободным висением с меньшим ground effect. Оба Saturation=false.

| Проверка этапа 6 | Фактический результат |
|---|---|
| Roslyn C# 8 / .NET 8 / NUnitLite 3.14 / Newtonsoft 13.0.2 | **217 passed, 0 failed**; 51 новых случаев |
| Simple Qω/efficiency, no-load loss; Electrical Kv/R/I0, back-EMF, ESC loss | Прошло против независимых формул |
| Корень constant-power sag, R=0 / очень малое R, невозможная нагрузка | Прошло |
| SOC/OCV, mAh, energy balance, отсутствие двойного подсчёта потерь | Прошло |
| Battery/motor/ESC current, maxPower, low voltage, empty pack, residual charge bounds | Прошло |
| Disarm, reset, disabled discharge, immutable snapshot, ошибочные настройки | Прошло |
| Совместимость CtCq / RPM table, ток CSV отдельно; явный reject battery+map/curve | Прошло |
| 500 случайных асимметричных команд на каждый режим | Ток/энергия/voltage/RPM/SOC bounds прошли |
| SOC: постоянная и переменная OCV, 50/100/200 Hz | Прошло |
| Altitude PID + allocator + motor lag + battery, оба режима, 50/100/200 Hz | Прошло: через 10 s высота в ±0.025 m, скорость <0.01 m/s |
| JSON Schema Draft 2020-12 | Обе схемы и 17 профилей прошли, включая 3 профиля пользователя |
| Пользовательский drone_rotor_effects.json + demo battery Simple / Electrical | Обе временные версии приняты текущим loader и QuadAllocator; исходный профиль сохранён |
| Roslyn syntax всех 50 C# файлов | 0 syntax errors; не заменяет Unity-компиляцию |
| 6 новых PowerRigidbodyTests (36 PlayMode случаев проекта всего) | Добавлены; здесь не запускались |
| InputTestFixture повтор, Unity импорт/editor/полёт с батареей | Ожидается у пользователя |

Контракт JSON 1.0.0 не менялся. Электрическая модель квазистационарная и не включает
индуктивность, rotor kinetic energy, regen/thermal или нагрузку бортовой электроники.
Governor ограничивает фактические RPM, а PID учитывает power limited для anti-windup.
Ограничения battery+RPM/J map / efficiency curve фиксируются явно, не скрываются.
Настройка и формулы — [POWER_SYSTEM.md](POWER_SYSTEM.md).

## Подтверждение этапа 6 и этап 7 — 2026-10-05

Пользователь подтвердил все тесты этапа 6. Ручной сценарий: при Pack current limit=4 A
взлёт невозможен; при 9 A и малой ёмкости сначала подъём, затем снижение SOC и потеря
возможности набирать высоту с постепенным снижением на землю.
Скриншот 4 A: Vbus≈15.96 V, Pbus≈63.8 W, Power limited=true; четыре тяги
1.45+1.63+1.33+1.15≈5.56 N при массе1 kg. Тяга ниже веса, контакт с землёй закономерен.
Это подтверждение пользовательского сценария, не сравнение с измеренным реальным дроном.

| Проверка этапа 7 | Фактический результат |
|---|---|
| Roslyn C# 8 / .NET 8 / NUnitLite 3.14 / Newtonsoft 13.0.2 | **260 passed, 0 failed**, 43 новых случая |
| Тропосфера: 0/3000/11000 m, идеальный газ, монотонность и диапазоны | Прошло против независимых численных значений |
| CtCq/PerformanceMap: изменение плотности ровно один раз, J неизменен | Прошло; measured kT/kQ и RPM tables явно отвергают переменную плотность |
| Axis/ProjectedArea/Surfaces: density scaling и диссипативность | Прошло |
| Gust: период, амплитуда, направление, fallback при нулевом среднем ветре | Прошло |
| Seeded field: повтор seed, другой seed, порядок запросов, пространственные отличия | Прошло |
| 500 точек: граница нормы, непрерывность, перенос поля; overlay bound | Прошло |
| Поле на 50/100/200 Hz и интегрирование сходимости относительно 400 Hz | Прошло |
| LinearWindField / отсутствие custom provider / disabled wind | Прошло |
| CtCq Qω и батарея с текущей плотностью | Прошло |
| Altitude PID + density-aware allocator + motor lag, MSL 3000 m, 50/100/200 Hz | Прошло: высота ±0.025 m, скорость <0.01 m/s через 15 s |
| JSON Schema Draft 2020-12 | Обе схемы и **26** профилей прошли, включая текущие JSON пользователя |
| Loader + QuadAllocator | Все 26 профилей приняты; atmosphere fixture проверен с CtCq |
| Unity .meta для новых файлов, GUID uniqueness | Полнота и уникальность подтверждены |
| Roslyn syntax всех 56 C# файлов | 0 ошибок; не заменяет Unity-компиляцию |
| 12 новых EnvironmentRigidbodyTests (48 PlayMode случаев всего) | Добавлены в изолированных physics scenes; **здесь не запускались** |
| Unity импорт/editor, фактический ветер/атмосфера и повтор полного Test Runner | Ожидается у пользователя |

Физические силы используют ветер в каждой точке приложения; плотность берётся один раз
в COM на шаг и передаётся также в энергетику. Некорректный provider проверяется до
передачи сил Rigidbody и расхода заряда. Проверка этого пути добавлена в PlayMode,
но фактический запуск требует Unity. Новый экспорт сохраняет отдельный environment JSON
и не меняет геометрию/моторы пользователя. JSON-контракт 1.0.0 не менялся.
Турбулентность — bounded analytic Fourier field, не валидированная метеорологическая
модель Dryden/CFD. Формулы и инструкция — [ENVIRONMENT.md](ENVIRONMENT.md).

## Исправление импорта PlayMode этапа 7 — 2026-10-05

При открытии проекта пользователь получил CS0246 для Newtonsoft и JObject в
EnvironmentRigidbodyTests. В PlayMode asmdef отсутствовала явная ссылка на
Newtonsoft.Json.dll, которая уже есть в Core, Editor и EditMode. Исправлено:
overrideReferences=true, precompiledReferences=[Newtonsoft.Json.dll, nunit.framework.dll],
по существующей настройке EditMode. Ссылки на Input System/TestFramework сохранены.
JSON asmdef и наличие прямых Newtonsoft-ссылок во всех использующих его сборках
проверены локально; git diff --check прошёл. Физические модели не менялись.
Полная Unity-компиляция и PlayMode здесь не запускались; требуется повтор у пользователя.

## Подтверждение этапа 7 и этап 8 — 2026-10-05

Пользователь подтвердил стабильную работу после исправления PlayMode asmdef.
Два пропущенных WindowsInput mouse tests принадлежат Input System 1.20.0:
в официальных IntegrationTests.cs оба явно помечены Ignore("Unstable due to 1252825").
Это штатный пропуск тестов пакета; DroneLab/PilotInputTests менять не требуется.
Ссылка на проверенный первичный источник — DIAGNOSTICS.md.

| Проверка этапа 8 | Фактический результат |
|---|---|
| Roslyn C# 8 / .NET 8 / NUnitLite 3.14 | **276 passed, 0 failed**, 16 новых случаев |
| Drive loss: экспоненциальный выбег, 50/100/200 Hz | Прошло |
| Половина target RPM → четверть quadratic T | Прошло |
| Simple/Electrical: failed motor bus current=0, shaft accounting, остаточная тяга | Прошло |
| Все приводы отказали / пустая батарея | Ток/расход=0, выбег не останавливается мгновенно |
| Invalid authority, reset | Прошло |
| CSV ru-RU/en-US, escaping rotorId, empty vs zero, invalid/nonfinite frames | Прошло |
| Hover induced velocity estimate | Прошло; helper не создаёт дополнительную силу |
| Python analyzer: reset segments, energy baseline, incomplete/NaN/time reversal | **4 passed, 0 failed** |
| Core CSV writer → файл → Python CLI | Успешно; две строки, T=mg=9.81 N, energy error=0 J |
| Roslyn syntax всех 63 C# файлов | 0 ошибок; не заменяет Unity-компиляцию |
| Прямые Newtonsoft references и .meta для новых ассетов | Полнота/корректность проверены; GUID уникальны |
| 6 новых DiagnosticsRigidbodyTests (54 PlayMode случаев всего) | Добавлены; **здесь не запускались** |
| Unity file lifecycle/manifest/segments, реакция Rigidbody и сравнение 50/100/200 Hz | Требуют запуска у пользователя |
| Реальный профиль, калибровка по независимым измерениям, VFX Graph | Процедура/API подготовлены; реальные измерения и визуальная интеграция не выполнены |

CSV публикуется до интегратора Rigidbody; SOC/charge/energy имеют суффикс _end.
Активные JSON сохраняются из Initialize. Recorder использует buffered synchronous writer;
ошибка записи завершает recorder, без нового расчёта сил/ветра/энергии. Reset разделяет
сегменты, новый runtime snapshot — папки. Manifest фиксирует настройки на начало записи,
не заменяет сцену/collider/конфигурацию внешнего программного provider.
Drive authority — сценарная runtime-настройка; контракт JSON 1.0.0 не изменён.
Recovery на трёх моторах, jam/broken propeller, rotational-energy dynamics и CFD
не объявляются реализованными. Подробная инструкция — [DIAGNOSTICS.md](DIAGNOSTICS.md).

## Финальный аудит и сквозная регрессия — 2026-10-05

Пользователь подтвердил все тесты этапа 8 и ручное поведение drive fault во всех
режимах. Наблюдение наклона при снижении с исправными моторами не интерпретируется
как валидация VRS: этой модели нет. В сохранённой PhysTest из пользовательского
коммита 2d5d96d включён automaticFault через 5 s; для baseline его нужно отключить.
Коммит пользователя получен fast-forward, его сцена/профили сохранены.

| Проверка | Фактический результат |
|---|---|
| Core + EditMode, Roslyn C# 8 / .NET 8 / NUnitLite | **284 passed, 0 failed**, 8 новых случаев |
| Combined CtCq/GE/atmosphere/Electrical/lag budget, 50/100/200 Hz | Qω и terminal/loss balance, charge integral и steady hover thrust прошли |
| Drag dissipation, failed motor current/coast в общем профиле | Прошло |
| Surfaces alternative + Electrical; RpmTable/PerformanceMap/Simple branches | Прошло |
| Python flight analyzer regression | **4 passed, 0 failed** |
| JSON Schema Draft 2020-12 | Обе схемы и **29** профилей прошли, включая новый пользовательский environment_test |
| Production ProfileLoader + QuadAllocator | **29** допустимых профиль/среда комбинаций приняты |
| Roslyn syntax всех **65** C# файлов | 0 ошибок; не заменяет Unity-компиляцию |
| Unity metadata | Новые .meta присутствуют, 244 GUID уникальны |
| git diff --check | Прошло |
| Новые FinalAcceptanceRigidbodyTests | **5** сценариев добавлены; 59 PlayMode случаев суммарно; **здесь не запускались** |
| Unity compilation / final coupled hover / inertia pulse / marker + inverted axes | Нужен запуск у пользователя; предыдущая suite подтверждена пользователем |
| Real-world accuracy / calibration | Не измерена, проценты точности не заявляются |

Основной fixture: quad_test_final_acceptance + environment_final_acceptance.
Включены только совместимые реализованные модели; unsupported module flags остаются false.
Остальные альтернативы проверяются отдельно, не суммируются для double counting.
Новое Editor menu создаёт общий стенд, recorder и manual fault. Marker-only export
сохраняет выбранную аэродинамику, батарею и enable flags; пересчитывает rotor/COM/CP
authoring geometry, удаляет stale derived cache. Для mesh/surfaces нужен соответствующий export.
JSON contract 1.0.0 не изменён, новых динамических сил не добавлено.

Итог исследования и ТЗ, ограничения и proposal runtime-маршрута —
[PHYSICS_AUDIT.md](PHYSICS_AUDIT.md). Воспроизводимая инструкция —
[FINAL_ACCEPTANCE.md](FINAL_ACCEPTANCE.md).

## SDK / mass properties, этап 9 — 2026-10-06

Пользователь подтвердил финальные Unity-тесты предыдущего этапа.
Ниже перечислены проверки кода SDK и синтетических inertia fixtures.

| Проверка | Фактический результат |
|---|---|
| Core/EditMode / Roslyn C# 8 / .NET 8 / NUnitLite | **300 passed, 0 failed**; 16 новых inertia cases |
| Random physical tensors from 20 point masses about COM | 600 матриц, 3 масштаба, реконструкция полного tensor прошла |
| Repeated eigenvalues, symmetry, SPD, physical triangle inequality, quaternion basis | Прошло |
| CAD import: explicit units/axes/reference, mass/COM/inertia, preserved motor/aero/power, validated output | Прошло; runtime JSON 1.0.0 не менялся |
| Resources-based fixtures | Pure regression прошла; hardcoded Assets path удалён для установки в package |
| Python analyzer + package checks | **8 passed, 0 failed** |
| Package output / --check | Physics 181 files + Demo 55 files, 0.2.0; воспроизводимость/stale detection прошли |
| Assembly dependency graph | 7 assemblies, DAG; Physics Runtime не ссылается на Input System/Demo |
| GUID/source .meta | 257 GUID уникальны в Assets, новые .meta присутствуют; старые pilot/input/camera GUID сохранены |
| Syntax parse | 73 C# files, 0 syntax errors; не заменяет Unity semantic compilation |
| Новые ControlBoundaryTests | 2 scenarios добавлены: no-pilot direct motor/CSV и independent controller telemetry/manifest; здесь не запускались |
| Actual Unity import обоих UPM packages, existing scene references, новый CAD Editor workflow / EXE | Ожидают запуска у пользователя |
| Physically calibrated drone / precision percentage | Не получены; синтетический профиль остаётся regression fixture |

Изменения сил/контроллерных gains отсутствуют. Полный tensor importer повышает точность
введения mass properties при наличии достоверных исходных данных. Assembly relocation
сохраняет MonoScript GUID и names/fields; проверить Missing Scripts при Unity-импорте.
Pure PID/allocator utilities остаются core; управляющие MonoBehaviours — optional Demo.
Recorder зависит только от IFlightControlTelemetry, не конкретного DroneTestPilot.
UPM export создаётся Tools/build_unity_packages.py; исходные scripts — Assets/DronePhysics.
Пользовательские FBX, scenes, профили и настройки Packages не изменялись этим этапом.

## Этап 10 — 2026-10-05

Пользователь подтвердил предыдущие тесты этапа 9. Новые результаты относятся к
исходникам Assets/DronePhysics; финальную пересборку UPM откладываем по его решению.

| Проверка | Фактический результат |
|---|---|
| Core + EditMode, Roslyn / .NET 8 / NUnitLite | **342 passed, 0 failed**, включая **42** новых случай RotorFlow |
| Знаки осевой тяги/flapping, lateral lift, arbitrary axis, rho, stop/windmilling | Прошло |
| Speed/thrust/moment caps, snapshot, flags, запрет axial/lift с RPM/J map | Прошло |
| Независимая линейная осевая динамика, 50/100/200 Hz | Прошло, ошибка относительно exp(-c*t) <0.001 m/s |
| Rate PID + local rotor forces/flapping, 50/100/200 Hz | Прошло; остаточная rate <0.025 rad/s, постоянная разница RPM для компенсации момента |
| Height PID + motor lag + battery + ground gain + lift/inflow, 50/100/200 Hz | Прошло; ошибка высоты <0.005 m, вертикальная скорость <0.005 m/s; одноосевой математический стенд |
| Python flight analyzer | **5 passed, 0 failed**, старые столбцы и новые dT/M/clip не дублируют thrust |
| JSON Schema Draft 2020-12 | 2 схемы и **21** resource profile прошли |
| Production ProfileLoader + QuadAllocator, advanced_rotors + final atmosphere/wind | Принят; 4 ротора, 13 LUT axes, static T/W=4; estimates/energy limitations явно предупреждаются |
| Roslyn syntax | **76 C# files**, 0 syntax errors; не Unity semantic compilation |
| Unity .meta | 121 GUID уникальны в DronePhysics, 261 во всех Assets; новые .meta присутствуют |
| git diff --check | Прошло |
| Новые RotorFlowRigidbodyTests | **6** isolated scenarios, **67** PlayMode cases суммарно; здесь не запускались |
| Unity import, HUD/menu, фактический coupled flight / Rigidbody | Требуется проверка у пользователя |
| Distribution/Packages rebuild / --check | Намеренно отложены до конца улучшений; snapshots 0.2.0 содержат этап 9 |
| Измеренная точность всей сборки / coupled aero power | Не подтверждена; этап 14 заменён открытыми данными/численной приёмкой, load solver — этап 11 |

Формулы, коэффициенты, clipping, совместимость и инструкция — ROTOR_FLOW.md.
CSV версии 1.1.0 хранит correction до GE и pure flap moment отдельно. Q/current не
получают выдуманную поправку из dT: батарея остаётся квазистационарной. Новые физические
профили не подменяют сцены/профили пользователя. FBX и UPM snapshots не изменялись.


## Этап 11 — 2026-10-05

Пользователь подтвердил Unity-тесты этапа 10. Проверки ниже относятся к исходникам
Assets/DronePhysics. Пересборка UPM остаётся отложенной до конца улучшений.

| Проверка | Фактический результат |
|---|---|
| Core + EditMode / Roslyn / .NET 8 / NUnitLite | **383 passed, 0 failed**; **41** новый случай CoupledPower |
| RPM/J load, Clamp, монотонность Q, maps + Simple/Electrical battery | Прошло; отрицательные/неподдержанные нагрузки отклоняются |
| Midpoint spin dynamics, acceleration reaction, motor/ESC losses, SOC, reset, alias safety | Прошло; shaft = propeller work + spin energy rate, потери отдельно |
| Disarm / failed motor / empty SOC / limits | Прошло; нет мгновенного удаления RPM, рекуперации или активного торможения |
| Независимый аналитический quadratic coast, 50/100/200 Hz | Прошло; ошибка конечной скорости <0.05 rad/s за 1 s |
| Rate PID + allocator + lag + motor inertia + rotor drag, 50/100/200 Hz | Прошло; слежение 70°/s, остаточная rate после release <0.03 rad/s |
| Gyroscopic signs / CW-CCW cancellation / moment power orthogonality | Прошло |
| CSV 1.2.0 | Новые shaft/spin/loss/gyro поля и отдельный force_rpm прошли; FirstOrder сохраняет семантику |
| Python flight analyzer | **5 passed, 0 failed** |
| JSON Schema Draft 2020-12 | **2** схемы, **23** resource profiles прошли |
| Production ProfileLoader + QuadAllocator | Оба новых coupled_power / coupled_power_map с final atmosphere/wind приняты: 4 ротора, 13 LUT axes, static T/W=4 |
| Roslyn syntax | **79 C# files**, 0 syntax errors; не заменяет Unity semantic compilation |
| Unity .meta | **126** уникальных GUID в DronePhysics, **266** во всех Assets; все .meta присутствуют |
| git diff --check | Прошло |
| Новые CoupledPowerRigidbodyTests | **6** isolated scenarios добавлены, **73** PlayMode cases суммарно; здесь не запускались |
| Actual Unity import / Rigidbody / menu / combined flight | Требуется запуск у пользователя |
| UPM snapshots | Не пересобирались по решению пользователя, версия 0.2.0 содержит этап 9 |
| Измеренная точность / полная энергетика аэродинамики | Не подтверждены; fixtures синтетические, limitations зафиксированы в COUPLED_POWER.md |

RotorInertia — явно выбираемый режим; старые JSON сохраняют FirstOrder. Силы,
карта Q, flow и spin momentum в инерционном режиме используют середину шага; CSV rpm
сохраняет конечную скорость, force_rpm — скорость оценки сил. Spin balance относится
к вращению относительно корпуса, а не всему аппарату. Weak-coupling guard sum(Jr)
≤5% минимальной locked-body inertia ограничивает применимость упрощения, не даёт
обещания точности 5%. Thrust-only ground/lift/flapping corrections остаются
эмпирическими; нет обещания полного aero power balance. Пользовательский FBX не
включён в коммит. Следующий этап — thermal / derating.


## Этап 12 — 2026-10-06 (Asia/Yekaterinburg)

Пользователь подтвердил Unity-тесты этапа 11. Здесь проверялись source Assets,
а не отложенные UPM snapshots. Реальный Unity Runner в окружении недоступен.

| Проверка | Фактический результат |
|---|---|
| Core + EditMode / Roslyn C# 8 / .NET 8 / NUnitLite | **431 passed, 0 failed**; **48** новых thermal cases |
| Independent exponential heating / adiabatic heat / cooling sign / no overshoot | Прошло; 50/100/200 Hz для constant-loss analytical response |
| Airflow cooling/cap / winding R(T) / immutable snapshot / optional disabled mode | Прошло; прежний electrical result сохранён без thermal |
| Motor/ESC/battery separate current limits, cutoff/coast/recovery boundary | Прошло; motor cap <= no-load current не блокирует остальные моторы |
| Coupled governor numerical sweep, 50/100/200 Hz против dt=0.001 s | Прошло: конечные omega в пределах 5 rad/s, T в пределах 0.25 K, SOC в пределах 0.0002 после 12 s; synthetic stress fixture |
| Heat balance и chemical/terminal/Joule loss energy, SOC/reset | Прошло; тепло не добавлено повторно к bus energy |
| Repeated trials/Resolve, failed queries, duplicate/mismatched Commit | Прошло; thermal/SOC state меняются один раз на успешный шаг |
| FirstOrder и RotorInertia thermal disarm/cutoff | Прошло; FirstOrder сохраняет caller coast, RotorInertia сохраняет spin energy |
| Weather VisualOnly invariance / cold vs warm ambient | Прошло; precipitation не создаёт дополнительные силы/мощность |
| Python flight analyzer | **7 passed, 0 failed**, включая CSV weather/heat и отсутствие double counting |
| JSON Schema Draft 2020-12 | **2** схемы, **29** resource profiles прошли |
| Production ProfileLoader + QuadAllocator | Thermal и thermal_stress с warm environment приняты: 4 ротора, 13 LUT axes, static T/W=3.802 |
| Roslyn syntax | **84 C# files**, 0 syntax errors; не Unity semantic compilation |
| Unity .meta | **137** unique GUID в DronePhysics, **277** в Assets; все .meta присутствуют |
| git diff --check | Прошло |
| Новые ThermalRigidbodyTests | **6** isolated scenarios добавлены; **79** PlayMode cases суммарно; здесь не запускались |
| Unity import/HUD/thermal editor/manual combined flight | Требуется пользовательский прогон |
| Package rebuild / measured precision / precipitation VFX | Не выполнялись; UPM deferred, коэффициенты synthetic/estimated, VFX — внешняя интеграция |

Основной профиль quad_test_thermal не предназначен для немедленного перегрева в H.
quad_test_thermal_stress имеет искусственно низкую C и пороги для быстрого наблюдения
derating. Это не рекомендация для настоящего двигателя/аккумулятора. Формулы,
источники, valid domain, energy semantics и инструкция — THERMAL_WEATHER.md.
