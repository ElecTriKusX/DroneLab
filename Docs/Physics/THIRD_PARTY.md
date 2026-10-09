# Источники, геометрия и атрибуция

DroneLab source сохраняет GPL-3.0-only репозитория. Это не смена лицензии внешних материалов. SDK содержит схемы/профили/числовые reference данные с атрибуцией, а не внешние крупные meshes или копии статей. Ссылки/commits закреплены в Data/reference-source-manifest.json; геометрия и SHA-256 — Data/reference-model-manifest.json.

| Источник | Использование | Условия / атрибуция |
|---|---|---|
| Bitcraze firmware documentation, PWM to thrust | Числовая таблица экспериментального стенда 2015 | Bitcraze; ссылка на оригинал и смысл измеренных колонок сохранены. Полный текст страницы не распространяется |
| UIUC Propeller Database, Volume 1 | CT/CP/RPM/J опубликованных APC 10×4.7 SF измерений | University of Illinois UIUC Applied Aerodynamics Group; источник указан. Числа и ограниченные bench excerpts, не перепечатка статьи. См. публикации базы: https://m-selig.ae.illinois.edu/props/propDB.html |
| gym-pybullet-drones | CF2 URDF параметры; внешний DAE, загружаемый отдельно | MIT, Copyright 2020 Jacopo Panerati; loader сохраняет исходный LICENSE |
| CrazyflieBrushJAX | Идентифицированные T/Q коэффициенты; внешний MuJoCo schematic | MIT, Copyright 2025 DSME; loader сохраняет исходный LICENSE. Статья авторов лицензируется отдельно и в SDK не перепечатывается |
| RotorS rotors_description | Внешний Hummingbird DAE и геометрические оценки | package.xml объявляет ASL 2.0 (Apache-2.0): https://www.apache.org/licenses/LICENSE-2.0 . Авторы пакета Fadri Furrer, Michael Burri, Mina Kamel, Janosch Nikolic, Markus Achtelik; исходный package.xml сохраняется. Репозиторий не содержит отдельного LICENSE файла; при дальнейшей упаковке meshes сохранить notice/полный Apache license и проверить требуемую атрибуцию |
| Wang et al., 2011, Klose2011a.pdf | Масса, инерция, thrust coefficient и аналитические reference значения | Jian Wang, Thomas Bierling, Leonhard Höcht, Florian Holzapfel, Sebastian Klose, Alois Knoll. Цитирование числовых параметров; полный PDF не распространяется |

Внешние assets имеют свои единицы/оси и требуют проверки при Unity импорте. Наличие mesh не означает идентифицированную аэродинамику или полную digital twin. Готовые визуалы меню DroneLab — primitives, явно обозначенные schematic.
