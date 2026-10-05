# Этап 13: снижение и спектральный ветер

Реализовано в Assets; Distribution/Packages пересобирается при итоговой приёмке.
Пользователь подтвердил Unity-тесты и профиль этапа 12. Новый профиль — synthetic
regression fixture, не измеренный FPV аппарат. JSON schemaVersion остаётся 1.0.0:
добавлены optional поля и новый явно выбранный windMode, старые режимы не меняются.

## Ручная проверка

1. Остановить Play, выбрать Terrain/точку старта.
2. **DroneLab → Diagnostics → Create Descent and Wind Acceptance Drone**.
3. Получатся `quad_test_descent_wind` + `environment_dryden_frozen`, recorder,
   fault scenario и geometry markers. Подключить камеру и сохранить сцену.
4. Play → Game View → F. Включить H; наблюдать высоту, ветер, SOC и температуры.
   H не удерживает X/Z: боковой снос ожидаем. Поднять дрон на безопасную высоту.
5. С H нажать Ctrl для плавного снижения, отпустить для торможения. Проверить
   positive-thrust descent; это знак местного потока, не диагноз VRS.
6. Проверить Fault и остановку моторов как в FINAL_ACCEPTANCE.md. При F off
   винты сначала выбегают из-за Jr; нулевую тягу требуем после остановки, не сразу.
7. Завершить запись; `Tools/analyze_flight.py flight.csv` покажет превышения
   envelope и недостаточную частоту дискретизации, отдельно от тяги и энергии.
8. Запустить DroneLab EditMode/PlayMode. Новые PlayMode тесты здесь не запускались.

## DrydenFrozen: что моделируется

Primary source: Michael M. Madden, *Verifying Implementation of the Dryden
Turbulence Model and MIL-F-8785 Gust Gradient*, NASA (2019):
https://ntrs.nasa.gov/api/citations/20190000875/downloads/20190000875.pdf
Использованы пространственные PSD и критерии mean/variance/spectrum; это собственная
конечнополосная спектральная реализация, не фильтры или MIL angular gradients NASA.

Односторонний пространственный спектр, k ≥ 0 в rad/m, L в m, sigma в m/s:

```
Phi_u(k) = sigma_u² * 2 L_u / pi / (1 + (L_u k)²)
Phi_v,w(k) = sigma² * L / pi * (1 + 3(Lk)²) / (1 + (Lk)²)²
integral_0^infinity Phi(k) dk = sigma²
F_u(q) = 2 atan(q) / pi
F_v,w(q) = [2 atan(q) - q/(1+q²)] / pi, q = Lk
```

Каждая компонента получает N логарифмических полос q_min..q_max. Энергия полосы
E = sigma² [F(q_hi)-F(q_lo)], амплитуда sqrt(2E), одна синусоида на геометрическом
центре k и фиксированная random phase. Seed и номер полосы задают фазы независимо
от порядка запросов. `s = dot(worldPosition, direction) - advectionSpeed * time`;
сумма гармоник в s добавляется к mean wind. Все три компоненты используют один s.

u направлена вдоль единичного горизонтального advectionDirectionWorld; w вдоль
мировой +Y; v = up × u. Для u=+X, v=-Z. AdvectionSpeed задаётся явно и может отличаться
от среднего ветра. Нулевой advection допустим: неподвижный замороженный рисунок;
движущийся дрон всё равно пересекает его. Mean wind не ограничивает амплитуды флуктуаций.

| Параметр | Значение и границы |
|---|---|
| sigmaUvwMps | Полный спектральный RMS каждой компоненты, 0..30 m/s; не peak bound |
| lengthScaleUvwM | Три L принятой формулы, 0.1..1e6 m; не автоматические MIL altitude presets |
| advectionDirectionWorld | Единичная горизонтальная ось XZ |
| advectionSpeedMps | 0..100 m/s |
| modesPerComponent | 8..128, всего 3N гармоник |
| minDimensionlessWaveNumber | 0.00001..1 |
| maxDimensionlessWaveNumber | 1..10000, строго больше min |
| turbulenceSeed | Обязательный явный seed; старый Turbulence использует свой прежний алгоритм |

Полосы .02..20 сохраняют около 95.5% sigma² продольной и 94.6% поперечной/вертикальной
компоненты. Энергия хвостов намеренно не перенормируется. Доступны immutable Bands,
RetainedVariance и ComponentAmplitudeBound. В editor: **Environment → Create
Environment Profile → DrydenFrozen**, затем явные sigma/L/axis/speed/seed/band/N.
Порыв можно наложить отдельно: прежний raised cosine, не часть спектра Dryden.

Это дискретные спектральные линии, приближающие интегральную энергию полос. Больше N
даёт более плотный спектр, но не гарантирует независимую турбулентность на всех временах.
Конечная сумма ограничена, не имеет полного гауссовского распределения и может повторять
квазипериодический рисунок. Среднее/дисперсия проверены по ансамблю seed; короткий полёт
одного seed не обязан иметь точно нулевое среднее или sigma². Поперечная когерентность
бесконечна: точки с одинаковым s имеют одинаковый ветер. Это **не полный 3D CFD**, не
вихри около зданий/terrain, не divergence-free поле и не MIL gust-gradient модель.
Локальный ветер всё же отдельно запрашивается у каждого ротора и CP/surface.

## Частота дискретизации

f_max = max(active k) * |dot(pointVelocity, direction)-advectionSpeed| / (2pi).
`wind_sampling_nyquist_ratio = f_max / (1/(2dt))`; при gust overlay учитывается
также его частота. Максимум берётся по COM, роторам и активным drag points; вращение
дрона входит в GetPointVelocity. Для иных windMode значение неизвестно/пустое.

>0.5: менее четырёх отсчётов на самый быстрый цикл, advisory UNDER-RESOLVED;
>1: выше Nyquist, aliasing. Это необходимая, не достаточная проверка численной точности:
нужна и сходимость динамики. Диагностика не обрезает ветер, не меняет dt/силы автоматически.
Уменьшить Fixed Timestep, сузить band или увеличить L; увеличить N само по себе не помогает.
Default L=20/20/10 m, speed=5 m/s достаточно медленные для 50/100/200 Hz около hover.

## Снижение и envelope

`rotors[].operatingEnvelope` optional, model=ReportOnly; все три max speed обязательны
при наличии, 0..200 m/s. В общем профиле synthetic пределы: climb=6, descent=3,
lateral=15 m/s. Они не являются измеренными пределами аппарата и не управляют пилотом.

Скорость до любого clipping: point velocity - local wind. Для unit thrust axis a:
axial = dot(v_air,a), lateral = |v_air-a*axial|. Положительный axial — climb вдоль a;
отрицательный — descent относительно воздуха. При наклоне это не world vertical speed.
Превышение: axial>maxClimb или -axial>maxDescent или lateral>maxLateral.
Без declared envelope flag неизвестен, а не false. Значение на границе допустимо.

`vi_hover = sqrt(T_base / (2*rho*pi*D²/4))`; `descent_vi_ratio = -axial/vi_hover`.
T_base — нагрузка performance model при том же force RPM/flow/density, до empirical
correction и ground effect. vi — диагностическая reference, не динамическое inflow state.
При stop или T≤0 ratio пустой; в climb он отрицательный. Regime codes:
0 Stopped, 1 NonPositiveThrust, 2 HoverOrClimb, 3 PositiveThrustDescent. Режим 3 включает
выбегающий винт с положительной тягой и не означает, что motor electrical drive активен.

CSV 1.4.0 добавляет пять rotor columns (axial_air_mps, lateral_air_mps, descent_vi_ratio,
flow_regime_code, envelope_exceeded), два global sampling columns. Старые колонки,
energy convention и единицы сохранены. Новый внешний recorder interface не обязателен:
старый поставщик RotorTelemetry оставляет optional diagnostics пустыми.

## Почему нет VRS force

Primary source: Wayne Johnson, NASA/TP-2005-213477, *Model for Vortex Ring State
Influence on Rotorcraft Flight Dynamics* (2005):
https://ntrs.nasa.gov/api/citations/20060024029/downloads/20060024029.pdf
Модель описывает mean inflow с данными rotorcraft и momentum extensions. Текущий
DroneLab считает T/Q из performance data и bounded empirical rotor flow, без решателя
inflow/blade element. Произвольное снижение тяги при -V/vi не воспроизводит эту модель;
прямое добавление может повторно учитывать inflow и нарушить shaft power accounting.

VRS, взаимодействие следов винтов и динамический inflow **не реализованы**. Мы проверяем
объявленный envelope, отсутствие тяги при остановке и пассивность drag. Это закрывает
ограниченный этап descent/wind validation, но не калибрует режим вихревого кольца.
За пределами объявленных данных/model domain абсолютную точность не заявляем.

## Проверки

Независимая Simpson quadrature спектра против интегральной энергии полос; PSD в известной
точке и sigma² полного спектра; фактические wind samples против гармоник; 2048-seed
mean/variance и lag covariance против интеграла PSD*cos(k*U*t); advection, query order,
immutable snapshots, zero/disabled mode, invalid configuration и sampling ratio.

Падение без тяги: m*dv/dt=-mg-c*v*|v|, v(0)=0, аналитически
v=-sqrt(mg/c)*tanh(sqrt(g*c/m)*t). Production body drag с midpoint RK2 сравнивается
через 5 s на 50/100/200 Hz. Это проверка force law и независимого интегратора, не PhysX.
Wind-driven drag пассивен относительно воздуха; translation сравнивается с 1000 Hz.
Шесть isolated PlayMode scenarios проверяют wiring, local point flow, deep descent,
rotor sampling warning, stopped drag, combined H flight/reset. Их выполнение требует Unity.
Следующий этап: итоговые независимые данные/численная приёмка и пересборка пакетов.
