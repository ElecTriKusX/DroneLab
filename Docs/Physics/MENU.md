# Меню DroneLab после этапа 14

| Раздел | Назначение |
|---|---|
| Test Bench / Basic Physics Drone | Минимальный дрон для ручного F/Angle/Acro/H |
| Test Bench / Scale References (meters) | Размерные ориентиры для сцены в метрах |
| Test Bench / Combined Physics Drone | Один синтетический аппарат с совместной проверкой airflow, батареи, инерции роторов, тепла, ветра и диагностики спуска |
| Test Bench / Module Checks | Отдельные стенды Body and Battery, Rotor Airflow, Rotor Inertia and Power, Thermal для поиска причины ошибки |
| Test Bench / Reference Drones | Три частичных профиля опубликованных аппаратов |
| Test Bench / Propeller Bench | Два APC стенда измеренных характеристик винта |
| Geometry / Mass Properties / Propellers / Rotors / Power / Environment | Подготовка и импорт профиля, а не новые сцены тестирования |
| Diagnostics | Запись/просмотр диагностики и fault инструменты |

Test Bench появляется при подключении Demo. Authoring/Diagnostics физики доступны в Physics. Сцену можно сохранить в своём проекте; меню не создаёт обязательную SDK сцену. Автоматические тесты запускаются через Unity Test Runner и создают изолированные стенды. Отдельная пользовательская сцена для них не нужна.

Physics markers — authoring инструмент. Runtime читает **snapshot JSON**, а не Transform маркеров каждый кадр. После перестановки/разворота маркера повторно экспортировать профиль, назначить обновлённый JSON и переинициализировать дрон. Перевёрнутый axis физика поддерживает; штатный QuadAllocator тестового pilot требует четыре параллельных +Y ротора и такой аппарат отклонит. Для произвольной конфигурации нужен другой controller.

После переноса пакетов: `Test Bench → Combined Physics Drone`, Play, F, H; проверить meter reference, ветер, SOC/voltage, thermal authority, rotor fault и CSV. Затем проверить три Reference Drones. Нельзя считать этот smoke test доказательством точности всех эффектов по реальным измерениям.

## Стенд этапа 13 и тесты из UPM

Стенд этапа 13 перенесён в **DroneLab → Test Bench → Combined Physics Drone**.
Он назначает `quad_test_descent_wind` и `environment_dryden_frozen`; включает
все предыдущие модули. Отдельные автоматические сценарии этапа 13 —
`DescentWindRigidbodyTests` (PlayMode) и `DescentWindTests` (EditMode).

1. Установить Physics и Demo, а также Test Framework через Package Manager.
2. Открыть **DroneLab → Diagnostics → Show Project Manifest (package tests)**:
   команда выделяет проектный `manifest.json` в Проводнике. Открыть его в VS Code
   или другом текстовом редакторе. Файл обычно не отображается как asset в окне Project.
   Без этой команды: ПКМ по папке Assets в Project → Show in Explorer → перейти
   на один уровень выше, где рядом лежат Assets, Packages и ProjectSettings →
   Packages → manifest.json. Не использовать Library/PackageCache.
3. Рядом с `dependencies`, на верхнем уровне JSON, добавить `testables`.
   Если поле уже есть, дописать два пакета в существующий массив, сохранив другие элементы.
   Это не manifest записи полёта и не package.json:

   ```json
   "testables": [
     "com.dronelab.physics",
     "com.dronelab.demo"
   ]
   ```

   Между соседними полями JSON нужна запятая. В исходном проекте DroneLab уже есть
   `com.unity.inputsystem` в testables; сохранить его. Исходные Tests из Assets не
   требуют подключения собственных UPM-пакетов через testables.
4. Дождаться компиляции. Открыть Window → General → Test Runner.
5. EditMode → Run All; затем PlayMode → Run All. Unity сама создаёт
   изолированные сцены. Не копировать Tests в Assets поверх установленных пакетов.

## Space и реальные reference аппараты

В 0.3.0 все стенды наследовали Manual Collective Fraction = 0.38. Это
доля **максимальной тяги**, поэтому у Crazyflie 2.0 (T/W≈2.14) и Hummingbird
(T/W≈2.10) Space давал меньше веса: аппарат не взлетал.
В 0.3.1 новые reference стенды выбирают `1.25 / T/W`, с ограничением 0.1…0.95.
Это настройка пульта, физические JSON не изменены. Существующая сцена
сохраняет старое значение; выставить вручную **0.60** либо пересоздать стенд.
Без H отпускание Space задаёт нулевой газ. Для зависания включить H.

H удерживает только высоту. WASD задаёт наклон, отпускание возвращает горизонт
в Angle, но не останавливает горизонтальное перемещение. У reference профилей
bodyDrag/rotorDrag отключены из-за отсутствия достоверных коэффициентов.
Это частичные стендовые профили, а не полный полётный двойник.
Ctrl двигает цель высоты вниз даже после касания земли: нет автопосадки и
ограничения цели Terrain. Если цель ушла под землю, выключить H и включить
снова, чтобы захватить текущую высоту; затем удержать Space. HUD 0.3.1
показывает целевую высоту и предупреждает о недостаточной ручной тяге.


## Исправления упаковки 0.3.2

Scale References создаёт `Assets/DronePhysics` перед сохранением материала, если
папки ещё нет в UPM-проекте. Материал остаётся пользовательским asset и повторно
используется при следующем вызове. Сборщик добавляет стабильные `.meta` к README,
LICENSE, CHANGELOG, package.json и source-files.sha256.json в обоих пакетах.
Documentation~ исключена из Unity asset import и не требует метаданных.
Для обновления заменить обе embedded папки либо обновить обе Git-зависимости до
одного нового SHA. Unity Library/PackageCache вручную не редактировать.
