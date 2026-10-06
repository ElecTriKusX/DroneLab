# Открытые аппараты и проверка по опубликованным данным — этап 14

Версия SDK 0.3.3 (численные reference-данные приёмки 0.3.0 сохранены). Это воспроизводимые **частичные профили**, а не полностью откалиброванные цифровые двойники. Неизвестные электрические, тепловые и аэродинамические эффекты отключены: выдуманные коэффициенты не должны выглядеть измеренными. Источник, единицы и оговорки находятся в `parameterProvenance` каждого JSON.

## Три аппарата

| Аппарат / ресурс JSON | Масса | Что взято из источников | Что остаётся оценкой |
|---|---:|---|---|
| Crazyflie 2.0 / `reference_crazyflie20` | 0.027 кг | Таблица Bitcraze RPM–суммарная тяга; масса, инерция и расположение из CF2X URDF | Одинаковость четырёх роторов; Q/T, задержка, размеры оболочки, шаг винта |
| Crazyflie Brushless с защитой / `reference_crazyflie_brushless` | 0.044 кг | Идентифицированные инерция, шаг мотора 0.05 с, полиномы T/Q из статьи и авторского кода | Размеры визуальной оболочки, шаг винта; низкооборотное соединение с нулём |
| AscTec Hummingbird / `reference_hummingbird` | 0.68 кг | Идентифицированные масса, инерция, плечо, тяга и Q/T из статьи, связанной с аппаратом в RotorS | Внешние размеры, шаг винта; перенос постоянной времени **тяги** на модель **RPM** — приближение |

Основные источники: [Bitcraze, PWM to thrust](https://www.bitcraze.io/documentation/repository/crazyflie-firmware/master/functional-areas/pwm-to-thrust/), [How to Model Your Crazyflie Brushless](https://arxiv.org/abs/2603.05944), [авторский код BrushJAX](https://github.com/Data-Science-in-Mechanical-Engineering/CrazyflieBrushJAX/tree/5e449f116b03218e803e728f2a9d8f68f60b05fe), [Wang et al., Novel Dynamic Inversion Architecture Design for Quadrocopter Control](https://archive.air.in.tum.de/Main/Publications/Klose2011a.pdf), [CF2X URDF](https://github.com/utiasDSL/gym-pybullet-drones/blob/7ebad1ecabd28a7000add2d05f888aa2e837c2cc/gym_pybullet_drones/assets/cf2x.urdf).

У Hummingbird статья даёт `5.7e-8 N/RPM²`, а RotorS — другой коэффициент в `N/(rad/s)²`. Принят коэффициент статьи с явным преобразованием единиц, не механическое копирование RotorS. Максимальные RPM вычислены из 3.5 Н на ротор. Масса статьи уже общая; массы роторов второй раз не добавляются. Для Brushless взята масса/инерция варианта с защитой из таблицы статьи, а не отличающиеся defaults кода.

ROS x-forward/y-left/z-up преобразованы в Unity x-right/y-up/z-forward: `(-y,z,x)`. Инерция переставлена соответственно. Корень физики и родители имеют scale=1; 1 Unity unit = 1 м.

## Реальные контрольные значения

Часть исходных точек не попадает в профиль и используется только при сравнении — holdout. `Tools/ReferenceValidation` вызывает тот же `ProfileLoader` и `PropellerPerformance`, что симуляция, и сохраняет подробный результат в `Data/reference-validation-results.json`.

| Проверка | Независимые точки | Результат и допустимая трактовка |
|---|---:|---|
| Bitcraze 2015: тяга четырёх моторов, делённая на 4 | 7 | Средняя относительная ошибка 3.313%; максимум 16.153% на 7570 RPM. Это ошибка интерполяции разреженной таблицы, а не ошибка траектории |
| UIUC APC 10×4.7 SF, статический стенд | 7 | Максимум абсолютной ошибки CT 0.0010400; CP 0.0004472 |
| UIUC APC 10×4.7 SF, осевой поток, 4014 RPM | 8 | Максимум абсолютной ошибки CT 0.0008000; CP 0.0004000 |
| Brushless, опубликованный идентифицированный полином | 53 промежуточных RPM | Максимум ошибки представления T ≈0.00003688 Н, Q ≈0.0000000575 Н·м. **Не независимая экспериментальная проверка** |
| Hummingbird, опубликованные предельные моменты | Аналитические значения | 0.544/0.007 = 77.714 рад/с² против округлённых 77.7; 0.102/0.012 = 8.5 рад/с². Проверка единиц и переноса параметров, не эксперимент траектории |

APC — два **винтовых стенда** на условном носителе Hummingbird, а не ещё два реальных аппарата: `bench_apc_10x47_static`, `bench_apc_10x47_axial`. Исходные [статические](https://m-selig.ae.illinois.edu/props/volume-1/data/apcsf_10x4.7_static_kt0835.txt) и [осевые](https://m-selig.ae.illinois.edu/props/volume-1/data/apcsf_10x4.7_kt0836_4014.txt) значения опубликованы [UIUC Propeller Database](https://m-selig.ae.illinois.edu/props/propDB.html). CP преобразуется в CQ как `CP/(2π)`. Пересчёт в Н/Н·м использует принятую плотность 1.225 кг/м³; это не дополнительно измеренные силы.

Bitcraze ток измерялся для всего стенда, включая плату. Нельзя делить его на четыре и считать током каждого мотора. Падение напряжения внешнего питания нельзя использовать как внутреннее сопротивление аккумулятора. Поэтому батарея отключена. У CF2 момент пока оценочный; yaw не прошёл независимую калибровку.

В осевой карте APC точка J=0 построена из статической таблицы; она не измеренная осевая точка. Отрицательная измеренная тяга при больших J сохранена; использование с активной электрической моделью требует соблюдения её ограничений. За измеренным диапазоном нет подтверждённой точности: применяются stop/clamp политики профиля. Brushless полином непригоден у нуля, поэтому участок ниже 250 рад/с исключён из сравнения. Статическая идентификация не подтверждает манёвры, быстрый спуск или погоду.

## Модели и запуск

| Аппарат | Найденная геометрия | Формат / оговорка |
|---|---|---|
| CF2 | [cf2.dae](https://github.com/utiasDSL/gym-pybullet-drones/blob/7ebad1ecabd28a7000add2d05f888aa2e837c2cc/gym_pybullet_drones/assets/cf2.dae) | COLLADA; Z_UP, метры; MIT проекта gym-pybullet-drones |
| Hummingbird | [hummingbird.dae](https://github.com/ethz-asl/rotors_simulator/blob/cd813b7a8c375d677352aa20ad20047feb661126/rotors_description/meshes/hummingbird.dae) | COLLADA; Y_UP; Apache-2.0 проекта RotorS; визуал не источник коэффициента тяги |
| Brushless | [quadcopter.xml.jinja](https://github.com/Data-Science-in-Mechanical-Engineering/CrazyflieBrushJAX/blob/5e449f116b03218e803e728f2a9d8f68f60b05fe/environment/quadcopter.xml.jinja) | Схематическая MuJoCo геометрия, MIT; полноценная производительская CAD/mesh модель не найдена |

`python Tools/fetch_reference_models.py --out ReferenceModels` скачает закреплённые исходники вместе с лицензиями; большие внешние meshes не включены в SDK. Для Unity DAE можно открыть в Blender, проверить единицы/оси и экспортировать FBX. XML.jinja — шаблон MuJoCo, не Unity mesh. Импорт конвертированных моделей в Unity здесь не проверялся.

Готовый запуск без внешних meshes: `DroneLab → Test Bench → Reference Drones → ...`. Создаётся размерная **схематическая** модель корпуса/плеч/дисков, Rigidbody, pilot и назначенный JSON. APC находится в `Test Bench → Propeller Bench`. Визуальную оболочку можно заменить дочерним mesh; не добавлять ему собственный Rigidbody и ненужные colliders.

Для одновременной проверки всех опциональных модулей использовать `Test Bench → Combined Physics Drone` (синтетический acceptance profile). Частичные реальные профили намеренно не включают неизвестную батарею/температуру: это разные виды проверки.

## Воспроизведение

```bash
python Tools/build_reference_profiles.py
dotnet run --project Tools/ReferenceValidation/ReferenceValidation.csproj -- Assets/DronePhysics/Resources/DronePhysics Docs/Physics/Data/reference-validation-results.json
```

Затем EditMode `ReferenceProfileTests` и PlayMode `ReferenceProfileRigidbodyTests`. Первый набор проверяет holdout, единицы, ограничения и шаг мотора; второй — спокойное зависание в изолированном physics rig. Holdout ошибки не подтягиваются обратно в профиль. Benchmark JSON имеет собственный formatVersion=1.0.0; это не drone profile. Полный список ссылок/закреплённых commits — `Data/reference-source-manifest.json`.

Для следующего уровня проверки нужны реальные синхронизированные flight logs, параметры контроллера/датчиков и условия эксперимента; опубликованный [nanodrone-sysid-benchmark](https://github.com/idsia-robotics/nanodrone-sysid-benchmark) — возможное дальнейшее направление, **в текущие результаты не входит**.
