# Разработка пакетов: Windows и Linux, SDK 0.3.3

Ветка **physics-packages** — рабочая ветка физического SDK. Windows — основная рабочая среда команды, Linux — поддерживаемая среда инструментов/CI. Интеграция пакетов в Unity и EXE проверяется отдельно на целевых платформах.

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
Set-Location "C:\Users\elect\Documents\Projects\DroneLab"
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

## 6. Диагностика частых ошибок

| Симптом | Причина / действие |
|---|---|
| Missing source metadata для Documentation~\Data | Старый Windows-сборщик смешал `\` и `/`; обновить до 0.3.3, не создавать .meta документации |
| Missing source directory | Удалена Assets-source или команда запущена с инструментом из другого дерева; вернуть исходники, не собирать урезанный пакет |
| Missing source metadata у .cs/.json runtime | Новый исходный asset без .meta; получить/сохранить метаданные в Unity и повторить сборку |
| Stale generated package | Distribution не соответствует source/version/docs; пересобрать оба пакета |
| Изменения исчезли после пересборки | Редактировался Distribution или cache; восстановить из Git/резервной копии и перенести в source |
| Duplicate assemblies/GUID | Одновременно установлены UPM-пакеты и Assets-source |
| UnicodeDecodeError / искажённый русский текст | Старый script/default codepage; инструменты 0.3.3 читают текст явно в UTF-8 |
| Space не поднимает старый reference-стенд | Старый serialized manualCollectiveFraction; поставить 0.60 либо пересоздать стенд |
| Дрон разгоняется без ограничения | Reference-profile отключает bodyDrag, Cd=0; использовать Combined либо отдельный откалиброванный полётный профиль |
| Тест .1 не компилируется при float API | Использовать `.1f`; пользовательский фикс включён в 0.3.3 |
