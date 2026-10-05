# Улучшение физики после восьми этапов

2026-10-06 (Asia/Yekaterinburg): пользователь подтвердил финальные Unity-тесты
и попросил сначала завершать физику, затем маршрут.
Источники и границы исходного исследования: PHYSICS_AUDIT.md.

«Идеал» переводим в измеримые критерии для оговорённого flight envelope. Без данных
конкретного аппарата нельзя гарантировать высокую абсолютную точность любым числом
optional effects. Два направления идут совместно: полнота моделей и проверка данных.
Контроллер при этом является внешним потребителем физики, а не источником новых сил.

| Этап | Результат | Приёмка / зависимости | Статус |
|---|---|---|---|
| 9. Переносимый SDK и mass properties | Два UPM packages, physics без Input System/test pilot, independent telemetry interface; полный CAD tensor → ManualPrincipal; разбор flight logs | Existing scene GUID, package resource paths, independent controller recording, tensor reconstruction; реальный Unity import | Реализован, 300 Core/EditMode tests; новый Unity import/2 PlayMode cases ожидают проверки |
| 10. Расширенная роторная аэродинамика | Согласованный empirical bundle translational lift / axial inflow / blade flapping; per-rotor local flow, параметры/единицы/границы | Сначала primary model formulas и коэффициенты; forces/moments power accounting; no double counting с картами; 50/100/200 Hz и finite domain | Следующий физический этап; не включён в текущие профили |
| 11. Связь винта, двигателя и батареи | Joint RPM/J map load solver, map limits, measured current vs estimated bus current; rotational energy/gyro при Jr; при необходимости motor L/ESC lag | Проверенные кривые, явные состояния/units; charge/energy conservation при arm/disarm/fault, envelope failure paths | Запланирован; не считать coast-down текущего lag полноценной energy dynamics |
| 12. Среда и тепловая нагрузка | Motor/battery thermal state, temperature effects; погодные presets и документированные rain/snow/hail упрощения | Thermal capacity/heat transfer/loss model, измеренные либо помеченные estimates; дождь не равен произвольному вычитанию тяги | Запланирован, погодные visuals — команда карты/UI |
| 13. Режимы снижения и wind validation | Envelope вертикального/бокового полёта, VRS только с выбранной моделью и данными; turbulence spectrum/Dryden если нужен | Descent/wind tests против независимых данных; clamped valid domain; отсутствие lift при остановленных винтах | Запланирован; uncontrolled tilt не доказывает VRS |
| 14. Профиль реального аппарата и калибровка | Настоящие mass/COM/I, propulsion/current/lag/drag curves; fitting и отдельные validation logs | RMSE + envelope/coverage, reproduce bench points, step/hover/yaw/climb/descent/endurance; один EXE smoke run | Требует real hardware/bench data; синтетические fixtures останутся regression tests |

Импорт сенсоров/GPS/IMU/barometer/rangefinder/camera и radio signal — обязательная
функциональная задача ТЗ и отдельный слой измерений. Она не является route autopilot,
но без неё нельзя назвать весь конкурсный прототип завершённым. UI/map/camera/VFX,
EXE/video/presentation выполняются параллельно командой; физика публикует общий API.

Маршрут/position hold/FreeCam editor откладываем по просьбе пользователя до оговорённой
физической приёмки. Готовый Angle/Acro/H controller остаётся тестовым инструментом;
его поведение не меняется этим этапом. Дрон в пакете Physics допускает прямые команды
любого controller и не зависит от наличие клавиатуры/геймпада.

Каждый этап — отдельный проверяемый commit. Для новой силы сначала фиксируются
формула, знак, units, domain, происхождение coefficients, energy implications и tests.
Старая схема/модели не меняют смысл молча. Пустой коэффициент не становится якобы
«измеренным» default. Unsupported model остаётся явной ошибкой до реализации.
