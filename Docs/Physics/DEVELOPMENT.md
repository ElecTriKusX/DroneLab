# Разработка и подключение физического SDK

[Раздел физики](README.md) · [Формулы](PHYSICS_REFERENCE.md) · [Контракт](PARAMETERS.md) · [Валидация](VALIDATION.md)

Разработка всего приложения и форка описана в [общем руководстве](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/DEVELOPMENT.md). Эта страница посвящена ядру, Unity-адаптеру и пакетам.

## Пакеты

| Пакет | Ответственность |
|---|---|
| `com.dronelab.physics` | Чистая физика, JSON, Unity-адаптер, authoring и диагностика |
| `com.dronelab.demo` | Тестовый пилот, ввод, HUD/камера и стенды |
| `com.dronelab.environment` | Адаптация источников среды и визуального окружения |

Physics/Demo имеют версию 0.3.4. Physics использует Newtonsoft JSON; Demo дополнительно требует Input System. HDRP не является зависимостью чистого физического ядра. Полноценные меню, галерея, датчики UI и маршрутное управление приложения не поставляются одним пакетом Demo.

## Подключить в другой Unity-проект

Рекомендуемая версия для интеграции — Unity 6000.3.25f1. Используйте один из вариантов:

1. **Локальный пакет:** в Package Manager выберите **Add package from disk** и укажите `package.json` нужного каталога `Distribution/Packages`. Для Demo сначала подключите Physics.
2. **Git package:** добавьте URL через Package Manager. Для воспроизводимости замените `COMMIT_SHA` на полный SHA выбранного опубликованного коммита:

```text
https://github.com/ElecTriKusX/DroneLab.git?path=/Distribution/Packages/com.dronelab.physics#COMMIT_SHA
https://github.com/ElecTriKusX/DroneLab.git?path=/Distribution/Packages/com.dronelab.demo#COMMIT_SHA
```

Для разработки пакета используйте отдельную редактируемую копию или embedded package. Не правьте кэш UPM. В исходном DroneLab эти же классы уже находятся в `Assets`; дополнительное подключение их экспортов создаёт дубли.

Git lock фиксирует resolved SHA. Новая версия ветки сама по себе не гарантирует обновление зависимостей: обновляйте URL/lock через Package Manager и проверяйте фактически разрешённый коммит.

## Минимальный API тела

Создайте объект с `Rigidbody`, `Collider` и `DronePhysicsBody`. Назначьте JSON аппарата и среды как TextAsset. В иерархии должен быть один рабочий Rigidbody, без замороженных управляющих осей; корень и родители — масштаб 1. Визуальная геометрия находится отдельно.

```csharp
if (body.Initialize(droneAsset, environmentAsset))
{
    body.SetArmed(true);
    body.SetMotorCommand(0, 0.5);
}
```

Это пример вызова, не готовый полётный контроллер. Команда 0.5 означает долю максимальных RPM, а не половину тяги; полный контроллер распределяет команды всем роторам.

Обычно `FixedUpdate` вызывает шаг автоматически. Для внешнего пошагового стенда задайте `AutomaticSimulation=false` и вызывайте `StepPhysics(float dt)` самостоятельно. Не запускайте оба пути одновременно. Начальная настройка fixed timestep 0,01 с требует проверки сходимости для конкретной задачи.

## Источники воздуха и ветра

`IWindProvider.Sample(DVector3 worldPositionM, double timeS)` возвращает мировой вектор в м/с. Для внешнего поля используются `windMode=CustomField` и `windInteraction=true`; источник назначается до инициализации. Запрос не должен зависеть от порядка обращений или выполнять HTTP внутри физического шага.

`IAirProvider.TrySampleAir(...)` предоставляет воздух. Один применённый снимок используется для аэродинамики, привода и теплообмена. `IWeatherProvider` предоставляет визуальные метаданные осадков. Требования к атмосфере и совместимости плотности описаны в [формулах](PHYSICS_REFERENCE.md).

Ручной `RuntimeWindOverride` подменяет действующий источник. `null` возвращает исходный источник; выключенное `windInteraction` не обходится override. Только физическое тело прикладывает силы.

## Authoring и стенды

Меню **DroneLab → Test Bench** создаёт контрольные сцены/объекты. **Combined Physics Drone** предназначен для совместного тестирования модулей; reference-аппараты и APC-стенды имеют свои области применения из [VALIDATION.md](VALIDATION.md).

Для ручного теста Demo назначьте дрон камере, включите Game View, моторы и удержание высоты. Полная клавиатура приложения описана в [GETTING_STARTED.md](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/GETTING_STARTED.md).

## Изменить модель или контракт

1. Сверьте уравнения, единицы и область применения.
2. Разместите чистую математику в `Assets/DronePhysics/Core`, Unity-взаимодействие — в адаптере.
3. При изменении профиля правьте `Tools/generate_physics_contract.py` и пересоздайте DTO, обе схемы и `PARAMETERS.md` вместе.
4. Обновите валидатор, подсказки UI, загрузку/сохранение и соответствующие тесты.
5. Обновите справочник моделей и контрольные результаты только на основании реально выполненных проверок.

Не подменяйте неизвестный коэффициент произвольным значением без отметки происхождения. Модель, зарезервированная JSON-контрактом, не обязательно имеет runtime-реализацию.

## Экспорт и проверка

Команды выполняются в корне исходного репозитория:

```bash
python Tools/build_unity_packages.py
python Tools/build_environment_package.py
python Tools/build_unity_packages.py --check
python Tools/build_environment_package.py --check
```

Экспорт копирует исходники и физическую документацию, сохраняет GUID и создаёт хеши содержимого. Текст канонизируется в UTF-8/LF; `Documentation~` исключается из Unity asset import и не требует `.meta`. После правки MD также нужен новый экспорт.

Тесты: [TESTING.md](https://github.com/ElecTriKusX/DroneLab/blob/main/Docs/TESTING.md). Для Test Runner установленных пакетов внесите требуемые пакеты в `testables` своего `Packages/manifest.json`. PlayMode исполняется в Unity, а не командой `dotnet test`.
