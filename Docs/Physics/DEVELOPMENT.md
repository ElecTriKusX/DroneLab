# Разработка пакетов: Windows и Linux, SDK 0.3.3

Ветка **physics-packages** — рабочая ветка физического SDK. Windows — основная рабочая среда, Linux — поддерживаемая среда инструментов/CI. Интеграция пакетов в Unity и EXE проверяется отдельно на целевых платформах.

## 1. Источник истины и структура

Текущая версия хранит редактируемые исходники в **Assets/DronePhysics**. Экспортёр копирует их в **Distribution/Packages**. Этот экспорт лежит вне Assets/Packages и не компилируется второй раз в разработческом Unity-проекте.

| Путь исходников | Путь внутри экспортированного пакета |
|---|---|
| `Assets/DronePhysics/Core` | Physics: `Runtime/Core` |
| `Assets/DronePhysics/Unity` | Physics: `Runtime/Unity` |
| `Assets/DronePhysics/Resources` | Physics: `Runtime/Resources` |
| `Assets/DronePhysics/Editor` | Physics: `Editor` |
| `Assets/DronePhysics/Tests/EditMode` | Physics: `Tests/Editor` |
| `Assets/DronePhysics/Demo/Runtime` | Demo: `Runtime` |
| `Assets/DronePhysics/Demo/Editor` | Demo: `Editor` |
| `Assets/DronePhysics/Tests/PlayMode` | Demo: `Tests/Runtime` |
| `Docs/Physics` | `Documentation~`; package README берётся из отдельного документа |

Pure C# Core не должен ссылаться на UnityEngine. Rigidbody/editor/input относятся к отдельным assemblies. Один активный controller пишет команды моторов; не запускайте одновременно экспериментальную физику и DronePhysicsBody на одном аппарате.

### Если код уже изменён в установленном пакете

Сохранить изменённые файлы **и их .meta**, затем перенести их по таблице в Assets-source. Не копировать экспорт целиком поверх Assets: структура Runtime/Core отличается. Не создавать новые GUID существующим компонентам.

Library/PackageCache — кэш, его правки не являются сохранёнными исходниками плагина. Embedded-пакет в настоящей папке Packages можно изменять в отдельном целевом проекте; для выпуска этим экспортёром правки нужно вернуть в Assets-source. Пересборка перезаписывает Distribution. При удалённых исходных папках 0.3.3 завершится явной ошибкой до записи частичных пакетов.

Если позднее Packages станет источником истины, одновременно изменить build tools, csproj, tests, contract generator и документацию. В 0.3.3 эта миграция не выполнялась. В одном Unity-проекте используется одна копия SDK: source **или** установленные пакеты.

## 2. Инструменты

- Unity Hub + **Unity 6000.3.25f1** и нужные build modules.
- Python **3.10+**, доступный как python на Windows / python3 на Linux. Инструменты пакетов и CSV используют стандартную библиотеку; pip install для них не нужен.
- .NET SDK **8** — только для запуска mathematical core tests/ReferenceValidation вне Unity.
- Git. Работать из корня репозитория, где находятся Tools, Assets, Docs и LICENSE.

Проверка версий:

```console
python --version
git --version
dotnet --version
```

### Windows / PowerShell

```powershell
Set-Location "C:\Projects\DroneLab"
python -m venv .venv
.\.venv\Scripts\python.exe Tools/build_unity_packages.py
.\.venv\Scripts\python.exe Tools/build_unity_packages.py --check
.\.venv\Scripts\python.exe -m unittest discover -s Tests/Python -v
```

Уже активированная venv: достаточно `python`. Прямой вызов её python.exe не требует изменения ExecutionPolicy или активации PowerShell-скрипта.

### Linux

```bash
cd /path/to/DroneLab
python3 -m venv .venv
.venv/bin/python Tools/build_unity_packages.py
.venv/bin/python Tools/build_unity_packages.py --check
.venv/bin/python -m unittest discover -s Tests/Python -v
```

На дистрибутивах, где модуль venv устанавливается отдельно, можно выполнить те же инструменты системным python3: внешних Python-зависимостей нет.

## 3. Как изменить и выпустить плагин

1. Сохранить пользовательские правки и обновить рабочую ветку physics-packages. Если git pull сообщает конфликт, разрешить его, сохранив намеренные изменения.
2. Изменять Assets-source, tests и документы соответствующей модели. Исходные .meta сохранять, новые Unity-assets создавать с .meta.
3. При изменении JSON-контракта отредактировать **Tools/generate_physics_contract.py**, затем запустить:

   ```console
   python Tools/generate_physics_contract.py
   ```

   В коммит вместе входят Profiles.Generated.cs, обе schema.json и PARAMETERS.md. Не переименовывать поля/единицы скрытно. Optional feature support в контракте не означает поддержку runtime.
4. Задать следующую semver в `VERSION` файла Tools/build_unity_packages.py. Он выставляет одну версию обоим package.json и зависимости Demo → Physics. Обновить README/changelog.
5. Собрать пакеты, запустить --check и Python suite. Пересборка не создаёт DLL: Unity самостоятельно компилирует C# после импорта.
6. Запустить ядро:

   ```console
   dotnet test Tests/DotNet/DroneLab.Physics.Tests.csproj --configuration Release
   ```

   Первый запуск требует доступа к NuGet. Это mathematical core/EditMode-код, не Unity Test Runner/PlayMode.
7. В Unity: Window → General → Test Runner → EditMode/PlayMode → Run All. В исходном проекте тесты в Assets доступны без добавления собственных пакетов в testables.
8. Проверить Combined Physics Drone, затем чистый импорт обоих пакетов в другом Unity-проекте и целевой EXE. Проверки редактора/сборки не подменять Python/.NET-результатами.
9. Изучить diff, включить source + docs + tests + оба Distribution пакета в коммит и push.

   ```console
   git diff --check
   git status --short
   git add Assets/DronePhysics Docs/Physics Tools Tests Distribution/Packages README.md CHANGELOG.md .gitattributes .github
   git commit -m "Release DroneLab packages 0.3.3"
   git push -u origin physics-packages
   ```

Перед git add проверить, что в перечисленных папках нет личных полётных логов или иных файлов, не предназначенных для публикации. Сама команда не выбирает только изменённый SDK.

## 4. Обновление другого проекта

Embedded: сохранить локальные изменения, заменить две папки Packages/com.dronelab.* папками из Distribution одного коммита. Не копировать Assets-source поверх установленного SDK.

Git: в проектном Packages/manifest.json указать оба URL с одинаковым SHA:

```json
"com.dronelab.physics": "https://github.com/ElecTriKusX/DroneLab.git?path=/Distribution/Packages/com.dronelab.physics#COMMIT_SHA",
"com.dronelab.demo": "https://github.com/ElecTriKusX/DroneLab.git?path=/Distribution/Packages/com.dronelab.demo#COMMIT_SHA"
```

COMMIT_SHA — полный SHA нужного опубликованного коммита, не буквальный текст. На время разработки можно использовать #physics-packages. Git package lock фиксирует resolved SHA: движение ветки само по себе не гарантирует обновление. Повторно установить Git URL через Package Manager либо заменить fragment на новый SHA.

Для тестов установленных пакетов добавить на верхнем уровне manifest:

```json
"testables": ["com.dronelab.physics", "com.dronelab.demo"]
```

Если массив уже есть, дописать элементы, сохранив остальные. Установить Unity Test Framework. Найти manifest: DroneLab → Diagnostics → Show Project Manifest (package tests), либо Проводник → корень проекта → Packages → manifest.json. Не менять Library/PackageCache/package.json.

## 5. Переносимость и контроль целостности

Логические пути файлов внутри пакета всегда POSIX с `/`, независимо от ОС. Реальные файловые операции используют pathlib. Документация под Documentation~ исключена из asset import, поэтому .meta там не генерируются.

Исходные GUID скриптов/папок сохраняются. Метаданные сгенерированных root-файлов имеют стабильный UUID5. Текст экспортируется как UTF-8 без BOM с LF; Git checkout с CRLF не меняет канонические SHA-256. Бинарные байты нормализации не подвергаются.

`--check` сравнивает полное множество файлов и канонические текстовые байты. Он допускает только эквивалентную BOM/CRLF-нормализацию; изменение содержимого или отсутствующий .meta вызывает ошибку. source-files.sha256.json хранит hash канонических экспортированных байтов и не хеширует сам себя. Перед byte-level проверкой файлов после Windows-checkout учитывать нормализацию, либо пересобрать экспорт.

CI [physics-packages.yml](https://github.com/ElecTriKusX/DroneLab/blob/physics-packages/.github/workflows/physics-packages.yml) запускает package verification, Python regression и .NET core tests на windows-latest/ubuntu-latest. Он не запускает Unity и не доказывает совместимость EXE. Проверка кириллицы/пробелов, BOM/CRLF и Windows logical paths также есть в Python suite.


## Подключение и запуск в Unity

В целевом проекте создать GameObject с Rigidbody, Collider и DronePhysicsBody;
назначить drone/environment TextAssets. Один Rigidbody в иерархии, без frozen axes;
root/parents scale=1. Mesh добавляется дочерним визуальным объектом.
Newtonsoft нужен Physics; Input System нужен только Demo.

```csharp
if (body.Initialize(droneAsset, environmentAsset))
{
    body.SetArmed(true);
    body.SetMotorCommand(0, 0.5); // доля max RPM, не тяги
}
```

Это обращение к API, не полноценный controller: команды распределяются всем роторам.
Обычно FixedUpdate вызывает StepPhysics автоматически. Для внешнего пошагового стенда
задать AutomaticSimulation=false и вызывать StepPhysics(float dt) вручную.
Начальный Fixed Timestep — 0,01 с; сходимость проверяется отдельно.

Для готового стенда: сцена с collider → DroneLab → Test Bench → Combined Physics Drone.
Назначить дрон камере, сохранить сцену, Play → Game View → F → H.
Reference Drones создаёт частичные профили, Propeller Bench — APC-стенды,
Module Checks — отдельные проверки аэродинамики, привода/питания и тепла.

## Управление

| Режим / ввод | Поведение |
|---|---|
| Angle, WASD | Заданный наклон → attitude PID → rate PID; нейтральный ввод выравнивает аппарат |
| Acro, WASD | Целевая угловая скорость; после отпускания достигнутый наклон сохраняется |
| Q/E | Yaw rate; удержание курса отсутствует |
| H | Высота → vertical speed → acceleration → collective; позиция X/Z не удерживается |
| Space/Ctrl при H | Изменение высотной цели |
| Space без H | Manual collective fraction; отпускание даёт нулевой target RPM |
| F / Z / Backspace | Arm-disarm / Angle-Acro / reset |

Demo использует идеальное состояние Rigidbody и QuadAllocator для четырёх +Y роторов.
Распределение сохраняет collective, затем ограничивает моменты; команды RPM находятся
инверсией статической тяги. PID содержит фильтр D, предел I и conditional anti-windup.
При H наклон компенсируется до upright=0,35; перевёрнутый полёт с H не поддерживается.
Параметры — DroneTestPilot Inspector; стартовые gains не являются калибровкой аппарата.
Gamepad выбирается в Input Device: правый стик roll/pitch, левый X yaw, RT throttle,
левый Y climb при H; отключение устройства/потеря фокуса вызывает disarm.

Текущий controller остаётся в com.dronelab.demo рядом с регрессионными стендами.
Ветка не нужна для разделения зависимостей: Physics уже работает без Demo/Input System.
Маршрутный автопилот, датчики и estimator целесообразно реализовать отдельным пакетом
поверх motor-command API, используя тот же Physics и общие интеграционные тесты.
На одном аппарате допускается один активный writer команд моторов.

## Authoring и диагностика

| Команда DroneLab | Назначение |
|---|---|
| Geometry → Create Markers from Profile | COM/ротора/drag markers из профиля |
| Geometry → Export Profile … | Marker geometry, Box drag, mesh silhouette или Surfaces; площадь mesh не определяет Cd |
| Mass Properties → Import CAD Inertia Tensor | Симметричный тензор относительно COM, масса/COM в SI и осях Unity; [пример](Examples/cad_inertia_test.json) |
| Propellers → Import Performance CSV | RPM/T/Q или RPM/J/Ct/Cq; строгие названия колонок и значения SI |
| Rotors / Power / Environment | Создание профилей роторных эффектов, батареи, тепла и среды |
| Diagnostics → Add Flight Recorder and Motor Fault Scenario | Запись CSV 1.4.0, активных JSON и manifest; сценарий отказа |
| Diagnostics → Open Flight Recordings Folder | Папка записей для анализа |
| Test Bench → Scale References (meters) | Размерные ориентиры; материал создаётся в Assets/DronePhysics |

CSV винта использует разделитель `,` и десятичную точку. RpmTable: обязательные
`rpm,thrustN,torqueNm`, опциональная `currentA`. PerformanceMap: обязательные
`rpm,advanceRatio,ct,cq`, опциональная `reynolds`. `cq` не является `cp`; vendor aliases
и угадывание единиц отсутствуют. До 4096 строк; итоговая карта проходит проверку
прямоугольной сетки, диапазонов и физической совместимости профиля.

CAD-ввод должен быть заранее приведён к COM, метрам/килограммам и actual tensor signs;
автоматического угадывания CAD-системы нет. Импорт сохраняет прочие параметры профиля.
CSV анализируется `python Tools/analyze_flight.py --help`; reset создаёт новый segment.
RPM/current/power/temperature/fault/envelope — разные показатели, их причины не смешиваются.

Известное ограничение reference-профилей: body drag и неизвестное питание выключены,
поэтому реалистичная максимальная скорость/flight time не подтверждены. При H строка
throttle 0% означает ручной ввод, а не отсутствие тяги. При насыщении проверить T/W,
наклон, ограничения батареи/тепла и активный fault. Duplicate assemblies означает
одновременное подключение Assets-source и UPM. Stale package требует пересборки;
новым импортируемым assets нужны .meta. Правки PackageCache не являются исходниками.

## Границы Physics и погодной интеграции

| Разработка | Ответственность |
|---|---|
| physics-packages / Physics | SI, сухая атмосфера, плотность/давление/температура, запрос местного ветра, аэродинамические силы, охлаждение, физические ограничения |
| envieroment-packages | Купленный погодный плагин, небо/облака/осадки/освещение, получение реальных метеоданных, время/координаты, адаптер к Physics |
| Demo | Ручное управление, HUD/камера и совместные регрессионные стенды |

envieroment-packages создаётся от согласованного SDK. Погодные изменения возвращаются
отдельным пакетом/адаптером; перенос всей ветки среды в Physics не является способом
обновления физического ядра. Платный плагин подключается отдельно; его код сейчас не
входит в этот SDK. На текущей основе интеграция с внешним метеосервисом не реализована.

Готовая точка подключения ветра — IWindProvider.Sample(DVector3 worldPositionM,
double timeS), возвращающая м/с в мировых осях Unity. Для неё профиль использует
windMode=CustomField, windInteraction=true; customWindProvider/CustomWindProvider
назначается до Initialize. Sample не изменяет состояние и не зависит от порядка
запросов. Источник заранее кэширует значения; HTTP-запросы не выполняются в physics step.

Текущий внешний интерфейс изменяет только ветер. Температура/давление/плотность среды
фиксируются валидированным snapshot при Initialize; live-обновление этих величин
потребует расширения адаптера/контракта. Повторный Initialize сбрасывает состояния
привода и питания и не подходит для незаметного обновления погоды в полёте.

Метеоданные приводятся к SI: °C→К, гПа→Па, км/ч→м/с. Направление метеоветра «откуда»
преобразуется в вектор «куда» с учётом севера сцены. Давление на станции и sea-level
pressure различаются: StandardAtmosphere ожидает sea-level inputs. Поддержка влажности
отсутствует. Один набор погодных данных управляет визуалом и адаптером; погодный плагин
не должен дополнительно прикладывать те же силы. Для воспроизводимости сохраняются
место, время, источник, возраст данных и использованный snapshot; offline fallback
задаётся явно. Описание внешнего плагина/API относится к ветке среды, формулы сил —
к PHYSICS_REFERENCE.
