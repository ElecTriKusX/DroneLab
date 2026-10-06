# Перенос DroneLab Physics между проектами

SDK 0.3.2 предназначен для Unity 6000.3 (разработческий стенд 6000.3.25f1).
JSON profile version остаётся 1.0.0. Проекты карты, меню и модели используют
одну зафиксированную версию SDK, а не копируют изменённые скрипты друг у друга.

## Границы

| Пакет | Содержимое | Зависимости |
|---|---|---|
| `com.dronelab.physics` | Pure C# core, Rigidbody/ветер/ground probes, profiles/schemas, markers/authoring/import, CSV/fault, EditMode tests | Newtonsoft 3.2.1, стандартные Unity physics/terrain/JSON/IMGUI modules |
| `com.dronelab.demo` | DroneTestPilot (Angle/Acro/H), keyboard/gamepad, camera, test-drone menus, PlayMode regression | Physics 0.3.2, Input System 1.20 |

Physics не ссылается на Demo или Unity.InputSystem. Pure PID/allocator/input-value
утилиты пока остаются в математическом core как доступные функции; сами они не
управляют дроном и не требуют Input System. Исполняемый test pilot необязателен.
Новый route autopilot позже станет самостоятельным controller поверх того же API.
Общий полёт допускает только одного writer команд моторов на дроне.

Зависимостей от HDRP, карты, Asset Store drone, чужого меню и экспериментальной
Aerodynamics package нет. Материалы/модель/colliders/сцены остаются в проекте команды.
SDK сохраняет текущую лицензию репозитория (GPL v3), не меняет её на другую.

## Установка в другой проект, самый простой путь

1. Взять папки `Distribution/Packages/com.dronelab.physics` и, для ручного теста,
   `Distribution/Packages/com.dronelab.demo` из одного опубликованного коммита.
2. Скопировать их в `Packages/` целевого проекта. Unity обнаружит embedded packages;
   package manifests задают зависимости. Сначала добавить обе папки, затем открыть Unity.
3. Проверить Console. Input System нужен только при добавлении Demo.
4. Создать объект: Rigidbody + Collider + DronePhysicsBody, назначить drone/environment
   TextAssets. Physics root и родители scale=1. Визуальный mesh — дочерний объект.
5. Для теста с клавиатурой добавить DroneTestPilot и камеру либо воспользоваться
   `DroneLab → Test Bench → Basic Physics Drone`. Эта команда появляется при установке Demo.
6. После Play проверить физику и запись CSV. Для схемы JSON/UI использовать тот же
   профильный контракт, не строить второй расчёт тяги/заряда в меню.

Если проект уже содержит `Assets/DronePhysics`, сначала выбрать один способ подключения:
Assets-source ИЛИ UPM. Две копии дают duplicate assemblies/classes/resources/GUID.
В development-проекте этого репозитория оставить source в Assets; Distribution здесь
лежит вне Assets/Packages и не компилируется. Для миграции существующего fork сохранить
пользовательские scenes/JSON/визуальные assets, заменить source-модуль пакетами и проверить
Missing Scripts. GUID исходных скриптов сохранены; Verify scene references после импорта
всё равно обязателен. Не импортировать весь Unity-проект поверх проекта карты.

В embedded режиме обновление = заменить две SDK-папки из одной новой версии,
не менять их вручную. Source изменяется в `Assets/DronePhysics`, затем экспортируется.

## Подключение через Git / Package Manager

Unity Package Manager также поддерживает Git package с `?path=`. В project manifest
нужно указать оба custom package явно: UPM не скачает unpublished Physics из реестра
только по semver зависимости Demo. Для теста можно использовать ветку:

```json
"com.dronelab.physics": "https://github.com/ElecTriKusX/DroneLab.git?path=/Distribution/Packages/com.dronelab.physics#codex/physics-foundation",
"com.dronelab.demo": "https://github.com/ElecTriKusX/DroneLab.git?path=/Distribution/Packages/com.dronelab.demo#codex/physics-foundation"
```

Для командной сборки заменить fragment после `#` на один и тот же полный commit SHA.
Version 0.3.2 — версия содержимого package.json, SHA однозначно фиксирует артефакт.
Не смешивать Git-installed и embedded копии. Весь Git repo содержит тяжёлые model assets;
локальные embedded SDK-папки проще для первого переноса.

Официальная инструкция Unity: https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/cus-pkg-development/cus-share.
UPM-папки можно установить и через Add package from disk, выбрав package.json.
Этот способ read-only cached/local dependency не предназначен для редактирования SDK.

## API для своего управления

- `Initialize(droneTextAsset, environmentTextAsset)` загружает новый snapshot; проверять
  return value/IsReady. Менять профиль до нового полёта, не во время записи без явного reset.
- `SetArmed(bool)` и `SetMotorCommand(rotorIndex, value)` — вход физики.
- value — target RPM / max RPM, 0…1; **не тяга и не мощность**. При ω² command 0.5 даёт 25% T.
- Пульт/автопилот отправляет команды до physics FixedUpdate (порядок −100 против 0).
- Physics рассчитывает ω/T/Q/drag/power и отдаёт это UI/VFX/recorder.
- `AutomaticSimulation=false` + `StepPhysics(dt)` только для внешнего шагающего стенда;
  не вызывать его второй раз при AutomaticSimulation=true.
- UI читает Parameters/Air/Power/GetRotorTelemetry и StepPrepared; не телепортирует дрон.

Controller может реализовать `IFlightControlTelemetry` для mode/input/desired rates/
settings JSON в записи. Recorder автоматически выбирает первый такой компонент на корне.
В одном активном режиме должен существовать один выбранный источник управления;
арбитраж режимов маршрутного controller будет отдельной задачей. Без controller CSV
сохраняет физические данные с mode=None. Interface не требует конкретного пульта или Input System.

## Проверки после установки

- EditMode: enabled package tests; профили читаются через Resources, без пути Assets.
- Полный PlayMode: добавить Demo и обе custom packages в `Packages/manifest.json` testables
  (`com.dronelab.physics`, `com.dronelab.demo`), обеспечить установленный Unity Test Framework.
- Проверить существующий prefab/scene на Missing Scripts и ручной F/H.
- Отдельно проверить EXE целевого проекта. Наличие package.json не доказывает Unity build.

На этой стороне проверены pure .NET tests, assembly graph, GUID/resource layout и
воспроизводимость пакетов; импорт этих новых пакетов в Unity здесь не выполнялся.

## Обновление SDK разработчиком

```bash
python Tools/build_unity_packages.py
python Tools/build_unity_packages.py --check
python -m unittest discover -s Tests/Python -v
```

`Distribution/Packages` — автоматически получаемый результат; в коммит входят source
и regenerated UPM-папки. SHA-256 каждого экспортированного файла хранится внутри пакета.
Новый commit SDK требует smoke test обоих пакетов. Публичный JSON/API меняется с
документированной совместимостью; удаление поля или смена единиц требует major version.

## Финальная сборка этапа 14

Distribution/Packages обновлены до 0.3.0 и включают этапы 10–14, JSON 1.0.0,
CSV 1.4.0, public benchmarks, новые профили, меню и итоговые документы.
`python Tools/build_unity_packages.py --check` сверяет каждый файл с source.
Чистый Unity import, PlayMode и EXE проверяет команда: Unity в среде сборки не установлен.
Пошаговый smoke test и новое меню — MENU.md; измерения/модели — REFERENCE_DRONES.md.


## Исправления SDK 0.3.2

Включает hotfix 0.3.1 (ручная тяга reference стендов и HUD) и исправляет создание
ScaleReferences.mat в чистом UPM-проекте. Все импортируемые файлы и папки имеют
стабильные .meta, исходные GUID компонентов сохранены. Для поиска manifest.json
использовать DroneLab → Diagnostics → Show Project Manifest (package tests).
Подробная настройка Test Runner и обновления существующих сцен — MENU.md.
