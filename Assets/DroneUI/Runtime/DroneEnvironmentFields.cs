using System;
using System.Collections.Generic;
using System.Globalization;
using DroneLab.Physics;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    /// <summary>UI limits and explanations; SI keys and the physics contract remain unchanged.</summary>
    public static class DroneEnvironmentFields
    {
        public sealed class Rule
        {
            public readonly double Min, Max;
            public readonly string Description, Default, Limits;
            public Rule(double min, double max, string description, string standard, string limits = null)
            { Min = min; Max = max; Description = description; Default = standard; Limits = limits ?? $"{min.ToString("G", CultureInfo.InvariantCulture)}…{max.ToString("G", CultureInfo.InvariantCulture)}"; }
            public string Help(string standard = null) => Description + "\n\nСтандартное значение: " + (standard ?? Default) + ".\nДопустимо: " + Limits + ".";
        }
        public static readonly Dictionary<string, Rule> Rules = new Dictionary<string, Rule> {
            ["name"] = new Rule(1,100,"Название профиля в общей библиотеке условий. Не привязывает профиль к карте.","Новый профиль","1–100 символов"),
            ["visual.weatherPresetId"] = new Rule(0,0,"Только визуал. Основа неба, освещения, облаков и тумана. Осадки выбираются отдельно. Ручная правка визуальных полей показывает «Кастомный»; исходный пресет сохраняется. Выбор новой основы сбрасывает визуальные правки.","Из сцены","пресеты каталога"),
            ["visual.timeOfDay"] = new Rule(0,23.999,"Только визуал. Время суток меняет положение солнца, небо и освещение.","14:00","00:00–23:59, формат ЧЧ:ММ"),
            ["visual.simulateTime"] = new Rule(0,1,"Только визуал. Продолжает ход часов симуляции после запуска. Скорость суток задаёт модуль времени сцены.","Выключено","Включено / Выключено"),
            ["weather.precipitation"] = new Rule(0,0,"Только визуал и метаданные. Вид атмосферных частиц: дождь, снег или град. Для выбранного вида нужен соответствующий эффект в сцене.","Нет","Нет / Дождь / Снег / Град"),
            ["weather.intensityMmPerHour"] = new Rule(0,200,"Только визуал и метаданные. Интенсивность в эквиваленте жидкой воды. 20 мм/ч соответствует полной настроенной эмиссии VFX; шкала частиц демонстрационная, без физической калибровки. При отсутствии осадков значение должно быть 0, иначе больше 0.","0 мм/ч"),
            ["windMode"] = new Rule(0,0,"Источник и модель физического ветра. Штиль: ветер выключен. Постоянный: средний вектор. Порывы: средний ветер плюс периодические усиления. Турбулентность: пространственные колебания. Модель Драйдена: воспроизводимое спектральное поле. Внешнее поле: поток задаёт провайдер сцены. Выбирается ровно одна модель: простая турбулентность и модель Драйдена — альтернативы. Периодические порывы можно отдельно добавить к среднему ветру или к выбранной турбулентности. Итоговый горизонтальный поток передаётся погодному контроллеру.","Штиль","одна из шести моделей"),
            ["meanWindSpeed"] = new Rule(0,100,"Скорость среднего горизонтального физического ветра. Порывы и колебания прибавляются выбранной моделью. Это тот же средний вектор, что X/Z в расширенных параметрах. Внешнее поле задаёт собственную скорость.","0 м/с"),
            ["meanWindBearing"] = new Rule(0,360,"Направление, ОТКУДА приходит ветер. 0°/360° — север (+Z мира), 90° — восток (+X), 180° — юг. Стрелка компаса указывает источник ветра.","0°"),
            ["airDensityMode"] = new Rule(0,0,"Постоянная плотность: плотность задаётся независимо от температуры и давления. Стандартная атмосфера: плотность рассчитывается по высоте; температура и давление заданы на уровне моря. Для переменной плотности дрону нужен CtCq или PerformanceMap.","Постоянная плотность","одна из двух моделей"),
            ["temperatureK"] = new Rule(200,330,"Температура физического воздуха: влияет на тепловые модели и плотность стандартной атмосферы. Передаётся также в погодную систему. При стандартной атмосфере это значение НА УРОВНЕ МОРЯ; локальная температура зависит от высоты. В UI используются °C, JSON хранит К.","15 °C","−73,15…56,85 °C; локально для системы погоды также 200–330 К"),
            ["airDensityKgM3"] = new Rule(.01,10,"Плотность физического воздуха: влияет на сопротивление и модели тяги CtCq/PerformanceMap. Используется только при постоянной плотности; в стандартной атмосфере вычисляется автоматически.","1,225 кг/м³"),
            ["pressurePa"] = new Rule(1000,200000,"Давление физического воздуха. В стандартной атмосфере задаётся НА УРОВНЕ МОРЯ, участвует в расчёте плотности; при постоянной плотности — метаданные воздуха.","101325 Па"),
            ["altitudeM"] = new Rule(-500,11000,"Высота точки отсчёта относительно уровня моря, а не положение Terrain. В стандартной атмосфере меняет локальные температуру, давление и плотность; высота полёта добавляется к ней.","0 м"),
            ["gravityMps2"] = new Rule(.01,30,"Физическое ускорение свободного падения. Меняет вес и необходимую тягу дрона.","9,81 м/с²"),
            ["windVelocityWorldMps"] = new Rule(-100,100,"Средний физический ветер по мировым осям Unity: X — восток, Y — вверх, Z — север. Это тот же ветер, что скорость и направление сверху, в другой записи; значения синхронизируются. Y добавляет восходящий/нисходящий поток. Горизонтальная часть итогового потока управляет визуальным ветром.","0 / 0 / 0 м/с","−100…100 м/с на ось; горизонтальная скорость до 100 м/с"),
            ["gustEnabled"] = new Rule(0,1,"Добавляет плавные повторяющиеся усиления вдоль среднего ветра. Например, средний ветер 5 м/с и амплитуда 3 м/с дают 5–8 м/с. Это добавка к среднему потоку, а не второй независимый ветер. При штиле и внешнем поле выключено; в режиме «Порывы» обязательно включено. Может дополнять постоянный ветер, турбулентность и модель Драйдена.","Выключено","Включено / Выключено с учётом режима"),
            ["gustIntensityMps"] = new Rule(0,100,"Для периодических порывов — максимальное усиление среднего ветра. Для пространственной турбулентности — масштаб колебаний. Изменяет физический поток; его горизонтальный результат передаётся погодному контроллеру. Для модели Драйдена собственную силу колебаний задают u/v/w, а эта амплитуда действует только при включённых периодических порывах.","0 м/с"),
            ["gustTimeScaleS"] = new Rule(.01,1e6,"Период периодического порыва; для пространственной турбулентности — временной масштаб, определяющий размер неоднородностей. Рекомендуемый рабочий диапазон 0,1–60 с.","2 с"),
            ["turbulenceSeed"] = new Rule(0,int.MaxValue,"Начальное число генератора случайного поля. Одинаковое число и одинаковые параметры позволяют повторить турбулентность при одинаковой траектории и времени.","48271","целое 0…2147483647"),
            ["dryden.sigmaUvwMps"] = new Rule(0,30,"Среднеквадратичные колебания скорости спектральной модели Драйдена: u вдоль переноса, v поперёк, w вертикально. Это RMS, не максимальный порыв. Работает только в режиме «Модель Драйдена», который заменяет простую турбулентность. Средний ветер сохраняется; модель добавляет колебания вокруг него. Например, u=1 м/с задаёт среднеквадратичный разброс, а не предел ±1 м/с.","0,4 / 0,4 / 0,25 м/с"),
            ["dryden.lengthScaleUvwM"] = new Rule(.1,1e6,"Характерные размеры неоднородностей для продольной, поперечной и вертикальной компонент модели Драйдена. Больший масштаб — более протяжённые структуры. Рекомендуется 1–1000 м.","20 / 20 / 10 м"),
            ["dryden.advectionDirectionWorld"] = new Rule(-1,1,"Горизонтальная единичная ось переноса замороженного поля Драйдена. Это направление ДВИЖЕНИЯ поля, а не направление «откуда дует». Y=0; X²+Z²=1.","1 / 0 / 0","X/Z −1…1, Y=0, длина вектора=1"),
            ["dryden.advectionSpeedMps"] = new Rule(0,100,"Скорость переноса пространственного поля Драйдена относительно мира. Независима от среднего ветра. 0 останавливает перенос, но движение дрона по полю по-прежнему меняет поток.","5 м/с"),
            ["dryden.modesPerComponent"] = new Rule(8,128,"Число спектральных гармоник на каждую компоненту. Больше гармоник — более подробное поле и больше вычислений. Рекомендуется 32.","32","целое 8–128"),
            ["dryden.minDimensionlessWaveNumber"] = new Rule(1e-5,1,"Нижняя граница безразмерного волнового числа kL: ограничивает самые крупные структуры. Должна быть меньше верхней границы.","0,02"),
            ["dryden.maxDimensionlessWaveNumber"] = new Rule(1,10000,"Верхняя граница kL: ограничивает самые мелкие структуры. Высокое значение требует достаточной частоты физического шага; подробность не означает большую точность.","20"),
            ["visual.windTurbulence"] = new Rule(0,10,"Только визуал. Дополнительное раскачивание совместимой растительности.","из выбранной погодной основы"),
            ["visual.wetness"] = new Rule(0,1,"Только визуал. Целевая влажность совместимых материалов: 0 — сухо, 1 — мокро.","из выбранной погодной основы"),
            ["visual.snow"] = new Rule(0,1,"Только визуал. Снежный покров совместимых материалов: 0 — нет, 1 — полный. Независим от падающего снега.","из выбранной погодной основы"),
            ["visual.cloudCoverage"] = new Rule(0,1,"Только визуал. Доля покрытия неба объёмными облаками.","из выбранной погодной основы"),
            ["visual.cloudDensity"] = new Rule(0,10,"Только визуал. Плотность и непрозрачность объёмных облаков.","из выбранной погодной основы"),
            ["visual.fogDistance"] = new Rule(1,100000,"Только визуал. Дистанция ослабления HDRP-тумана. Меньше значение — гуще туман. Нужен активный модуль Fog и поддержка тумана в HDRP.","из выбранной погодной основы"),
            ["visual.fogBaseHeight"] = new Rule(-10000,10000,"Только визуал. Нижняя высота HDRP-тумана в мировых координатах. Не связана с опорной высотой физической атмосферы. Должна быть ниже верхней границы.","из выбранной погодной основы"),
            ["visual.fogMaxHeight"] = new Rule(-10000,10000,"Только визуал. Верхняя высота HDRP-тумана в мировых координатах. Должна быть выше нижней границы.","из выбранной погодной основы")
        };
        public static Dictionary<string,string> Errors(DroneEnvironmentDocument document, DroneScenarioCatalog catalog = null)
        {
            var errors = new Dictionary<string,string>();
            var e = DroneEnvironmentProfiles.Effective(document.environment);
            foreach (var pair in Rules) {
                string key = pair.Key; var source = key.StartsWith("visual.") ? document.visual : e;
                string path = key.StartsWith("visual.") ? key.Substring(7) : key;
                var token = source.SelectToken(path);
                if (token == null || token.Type == JTokenType.Boolean || token.Type == JTokenType.String) continue;
                var values = token is JArray array ? (IEnumerable<JToken>)array : new[] { token };
                foreach (var v in values) if ((v.Type != JTokenType.Integer && v.Type != JTokenType.Float) ||
                    !Finite((double)v) || (double)v < pair.Value.Min || (double)v > pair.Value.Max) {
                    errors[key] = "Допустимо: " + pair.Value.Limits; break;
                }
            }
            if (string.IsNullOrWhiteSpace(document.name) || document.name.Length > 100) errors["name"] = "Название: 1–100 символов.";
            var vector = e["windVelocityWorldMps"].ToObject<double[]>();
            if (vector.Length == 3 && Math.Sqrt(vector[0]*vector[0]+vector[2]*vector[2]) > 100) errors["windVelocityWorldMps"] = errors["meanWindSpeed"] = "Горизонтальная скорость не больше 100 м/с.";
            string mode = (string)e["windMode"]; bool gust = (bool)e["gustEnabled"];
            if ((mode == "Gust" && !gust) || ((mode == "None" || mode == "CustomField") && gust)) errors["gustEnabled"] = "Порывы несовместимы с выбранным режимом.";
            if (((string)e["weather"]["precipitation"] == "None") != ((double)e["weather"]["intensityMmPerHour"] == 0)) errors["weather.intensityMmPerHour"] = "Без осадков — 0; с осадками — больше 0.";
            if (mode == "DrydenFrozen") {
                var axis = DVector3.From(e["dryden"]["advectionDirectionWorld"].ToObject<double[]>());
                if (Math.Abs(axis.Y) > 1e-8 || Math.Abs(axis.Length - 1) > 1e-5) errors["dryden.advectionDirectionWorld"] = "Нужен горизонтальный единичный вектор.";
            }
            if ((double)e["dryden"]["minDimensionlessWaveNumber"] >= (double)e["dryden"]["maxDimensionlessWaveNumber"]) errors["dryden.maxDimensionlessWaveNumber"] = "Верхняя граница должна быть больше нижней.";
#if ENVIRO_HDRP
            var preset = catalog?.Weather((string)document.visual["weatherPresetId"])?.preset;
            double baseHeight = (double?)document.visual["fogBaseHeight"] ?? preset?.fogOverride?.baseHeight ?? 0;
            double maxHeight = (double?)document.visual["fogMaxHeight"] ?? preset?.fogOverride?.maxHeight ?? 250;
            if (baseHeight >= maxHeight) errors["visual.fogMaxHeight"] = errors["visual.fogBaseHeight"] = "Верхняя граница тумана должна быть выше нижней.";
#endif
            if (errors.Count == 0 && (string)e["airDensityMode"] == "StandardAtmosphere") {
                try { var air = Atmosphere.Troposphere((double)e["altitudeM"],(double)e["temperatureK"],(double)e["pressurePa"]); new AtmosphereColumn(air.TemperatureK, air.PressurePa,air.AltitudeM); }
                catch (ArgumentException) { errors["altitudeM"] = errors["temperatureK"] = "Локальный воздух выходит за диапазон системы погоды. Проверьте высоту, температуру и давление."; }
            }
            return errors;
        }
        public static bool Finite(double number) => !double.IsNaN(number) && !double.IsInfinity(number);
    }
}
