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
