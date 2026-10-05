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
| 9. Переносимый SDK и mass properties | Два UPM packages, physics без Input System/test pilot, independent telemetry interface; полный CAD tensor → ManualPrincipal; разбор flight logs | Existing scene GUID, package resource paths, independent controller recording, tensor reconstruction; реальный Unity import | Реализован; пользователь подтвердил тесты. Перенос UPM в другой проект проверяется при финальной сборке пакетов |
| 10. Расширенная роторная аэродинамика | Согласованный empirical bundle translational lift / axial inflow / blade flapping; per-rotor local flow, параметры/единицы/границы | Сначала primary model formulas и коэффициенты; forces/moments power accounting; no double counting с картами; 50/100/200 Hz и finite domain | Реализован в Assets; ROTOR_FLOW.md, 42 новых pure cases и 6 новых PlayMode scenarios; пользователь подтвердил Unity-тесты этапа 10 |
| 11. Связь винта, двигателя и батареи | Joint RPM/J map load solver, map limits, measured current vs estimated bus current; rotational energy/gyro при Jr; при необходимости motor L/ESC lag | Проверенные кривые, явные состояния/units; charge/energy conservation при arm/disarm/fault, envelope failure paths | Реализован в source: RPM/J load, RotorInertia energy/mount reaction/gyro, отдельные потери; COUPLED_POWER.md. 41 новый pure case; пользователь подтвердил Unity-тесты этапа 11. L/regen/active braking/efficiencyCurve не реализованы |
| 12. Среда и тепловая нагрузка | Motor/battery thermal state, temperature effects; погодные presets и документированные rain/snow/hail упрощения | Thermal capacity/heat transfer/loss model, измеренные либо помеченные estimates; дождь не равен произвольному вычитанию тяги | Реализован: lumped motor/ESC/battery thermal, winding R(T), current derating и weather presets/VisualOnly metadata; THERMAL_WEATHER.md. 48 новых pure cases, 6 новых PlayMode scenarios; пользователь подтвердил Unity-тесты этапа 12; VFX — команда карты/UI |
| 13. Режимы снижения и wind validation | Envelope вертикального/бокового полёта, VRS только с выбранной моделью и данными; turbulence spectrum/Dryden если нужен | Descent/wind tests против независимых данных; clamped valid domain; отсутствие lift при остановленных винтах | Реализован ограниченный descent/wind этап: ReportOnly rotor envelope, finite-band DrydenFrozen, PSD/variance/covariance и analytical descent checks; DESCENT_WIND.md. VRS forces не реализованы: отсутствует согласованный inflow solver/данные; uncontrolled tilt не доказывает VRS |
| 14. Приёмка без собственного оборудования | Открытые измерения винтов, данные производителей, независимые аналитические сравнения, sweep envelope и численная сходимость; итоговый общий профиль и сборка UPM | Источник/условия каждого параметра, ошибки на независимых точках данных, 50/100/200 Hz, hover/yaw/climb/descent/battery/fault; EXE и импорт SDK в чистый проект | Аппаратные тесты не требуются. Проверяем модели/данные и устойчивость; абсолютную точность конкретного аппарата не заявляем |

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

2026-10-05: пользователь подтвердил тесты этапа 9 и уточнил доступные ресурсы.
Собственного измерительного оборудования нет; этап 14 не требует его приобретения.
Реалистичная цель — достаточная физика в объявленном envelope и воспроизводимая
численная проверка. Открытые bench данные, например UIUC Propeller Database
(https://m-selig.ae.illinois.edu/props/propDB.html), позволяют проверить конкретные
винты/условия, но не всю неизвестную сборку. Пересборку Distribution/Packages
откладываем до конца этапов по прямому указанию пользователя.

2026-10-06: пользователь подтвердил этап 10. Этап 11 завершает ограниченную
модель supply/load/spin, не добавляет motor L или ESC lag без оснований. Midpoint
RPM/J и weak-coupling envelope описаны в COUPLED_POWER.md. Тепловое состояние — этап 12.

2026-10-06: пользователь подтвердил этап 11. Этап 12 реализует effective thermal
state и protection policy, а не внутреннюю LiPo chemistry или damage. Старые профили
не включают thermal автоматически. Следующий — этап 13: descent/wind envelope.

2026-10-06: пользователь подтвердил тесты и профиль этапа 12. Этап 13 добавляет
DrydenFrozen и report-only descent/envelope diagnostics, CSV 1.4.0, combined fixture.
VRS остаётся явно не реализованным; следующий — этап 14 и финальная пересборка UPM.
