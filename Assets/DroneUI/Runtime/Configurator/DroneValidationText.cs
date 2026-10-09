using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DroneLab.UI
{
    /// <summary>Presentation only: the physics loader remains the authority for accepted values.</summary>
    public static class DroneValidationText
    {
        private static readonly Dictionary<string,string> Messages=new Dictionary<string,string> {
            ["Required field is missing."]="Обязательный параметр отсутствует.",
            ["Required for the selected model."]="Этот параметр нужен для выбранной модели расчёта.",
            ["Required for an enabled battery."]="Параметр обязателен при включённой батарее.",
            ["Unknown field."]="Контракт физики не поддерживает этот параметр.",
            ["Invalid array length."]="Неверное число компонентов вектора или строк таблицы.",
            ["Number is not finite or is outside the allowed range."]="Число должно быть конечным и входить в допустимый диапазон.",
            ["String must not be empty."]="Введите непустой текст.",
            ["Three positive components are required."]="Все три компонента должны быть больше нуля.",
            ["A unit quaternion is required."]="Кватернион ориентации должен иметь длину 1. Используйте нормализацию.",
            ["Axis must be normalized and nonzero."]="Ось тяги должна быть ненулевым вектором длины 1. Используйте нормализацию.",
            ["A unit direction is required."]="Направление должно иметь длину 1. Используйте нормализацию.",
            ["A unit normal is required."]="Нормаль должна иметь длину 1. Используйте нормализацию.",
            ["Principal moments must satisfy triangle inequalities."]="Каждый главный момент инерции не должен превышать сумму двух остальных.",
            ["Duplicate rotor ID."]="Идентификаторы роторов должны быть уникальными.",
            ["Duplicate surface ID."]="Идентификаторы поверхностей должны быть уникальными.",
            ["Require 0 <= minRpm <= idleRpm < maxRpm."]="Обороты должны удовлетворять условию: 0 ≤ минимальные ≤ холостые < максимальные.",
            ["This mode is reserved in contract 1.0.0 but not implemented by this runtime."]="Этот режим зарезервирован в схеме, но ещё не реализован в текущей физике.",
            ["Ground effect settings are required when the module is enabled."]="Для включённого экранного эффекта добавьте его параметры в аэродинамике.",
            ["Aerodynamic settings are required for every rotor when its module is enabled."]="При включённом модуле аэродинамики задайте его параметры для каждого ротора.",
            ["Measured kT/kQ require their reference density. Use CtCq for density scaling."]="Измеренные kT/kQ работают при плотности воздуха калибровки. Для зависимости от плотности используйте Ct/Cq.",
            ["Measured thrust/torque/current tables require their reference density; no implicit scaling."]="Измеренная RPM-таблица работает при плотности калибровки; автоматического пересчёта по плотности нет.",
            ["Explicit outOfRangePolicy Clamp or Reject is required."]="Выберите поведение за границами таблицы: граничные значения или запрет.",
            ["rpmTable requires 2..4096 rows."]="RPM-таблица должна содержать от 2 до 4096 строк.",
            ["performanceMap requires 2..4096 rows."]="Карта RPM/J должна содержать от 2 до 4096 строк.",
            ["Duplicate RPM rows are not allowed."]="Каждому значению RPM должна соответствовать одна строка.",
            ["RPM, thrust, torque and current must be finite and nonnegative."]="RPM, тяга, момент и ток должны быть конечными и неотрицательными.",
            ["An explicit zero RPM / zero thrust / zero torque (and zero current if supplied) origin is required."]="Добавьте начальную строку: RPM = 0, тяга = 0, момент = 0 и ток = 0, если указан.",
            ["currentA must be supplied for all rows or omitted for all rows."]="Ток должен быть указан во всех строках таблицы либо отсутствовать во всех.",
            ["Map RPM must be positive; RPM/J/Ct/Cq must be finite."]="RPM карты должен быть больше нуля; RPM, J, Ct и Cq должны быть конечными.",
            ["Map requires a complete rectangular RPM x J grid with at least two J values and no duplicate cells."]="Карта должна содержать полную прямоугольную сетку RPM × J: минимум два значения J, без повторов ячеек.",
            ["Map must include a J=0 column for static capacity/reference calculations."]="Добавьте столбец J = 0 для расчёта статической тяги.",
            ["Reject table must cover motor maxRpm."]="При запрете выхода за таблицу её диапазон должен включать максимальные RPM двигателя.",
            ["Reject map must cover motor maxRpm."]="При запрете выхода за карту её диапазон должен включать максимальные RPM двигателя.",
            ["Require 2..256 sorted points covering SOC 0 and 1."]="OCV-кривая должна содержать 2–256 упорядоченных точек, включая SOC = 0 и SOC = 1.",
            ["SOC must strictly increase and pack voltage must not decrease."]="SOC должен строго возрастать; напряжение всей батареи не должно уменьшаться.",
            ["Battery modules require Simple or Electrical battery mode."]="Модулям батареи нужен упрощённый или электрический режим питания.",
            ["Enable exactly for Electrical battery mode; disable for Simple."]="Электрический модуль двигателя включается в электрическом режиме питания и выключается в упрощённом.",
            ["Motor power settings required for every rotor with battery enabled."]="Для работы с батареей добавьте электрические параметры каждого двигателя.",
            ["No-load current must be below both current limits."]="Ток холостого хода должен быть меньше предельного тока двигателя и ESC.",
            ["Load-dependent efficiencies await a validated governor; use constant motorEfficiency for now."]="Регулятор с кривой КПД ещё не поддерживается. Используйте постоянный КПД двигателя.",
            ["RotorInertia requires Electrical battery mode and motorResponse."]="Баланс моментов ротора требует электрического режима батареи и включённой динамики двигателя.",
            ["Gyroscopic rotor effects require RotorInertia dynamics with spin inertia."]="Гироскопический эффект требует динамики «Баланс моментов» и инерции вращающихся частей.",
            ["All rotors must select RotorInertia with positive rotatingInertiaKgM2; mixed dynamic models are unsupported."]="Все роторы должны использовать баланс моментов и положительную инерцию вращения; смешанные модели не поддерживаются.",
            ["Thermal model requires Electrical mode for explicit motor, ESC and battery losses."]="Тепловой расчёт требует электрического режима питания с потерями двигателя, ESC и батареи.",
            ["Explicit thermal node required; no silent defaults."]="Добавьте тепловые параметры компонента. Автоматических значений для включённого расчёта нет.",
            ["cutoffTemperatureK must exceed derateStartTemperatureK."]="Температура отключения должна быть выше температуры начала снижения мощности.",
            ["Thermal mode requires explicit constant ambient temperature 100..500 K."]="Для теплового расчёта задайте в окружении температуру воздуха от 100 до 500 K.",
            ["Thermal mode requires explicit resistance reference temperature and coefficient (0 disables the temperature dependence)."]="Задайте температуру измерения сопротивления и температурный коэффициент. Нулевой коэффициент отключает зависимость сопротивления от температуры.",
            ["Winding resistance must remain positive throughout ambient envelope 100..500 K."]="Сопротивление обмоток должно оставаться положительным во всём диапазоне 100–500 K.",
            ["Empirical thrust-only gain: base performance must be measured out of ground effect; torque/current are unchanged."]="Экранный эффект меняет только тягу. Базовые измерения должны быть сделаны вне экранного эффекта; момент и ток этим модулем не меняются.",
            ["Axial inflow is already represented by the RPM/J map; inducedDrag must be disabled."]="Карта RPM/J уже учитывает осевой поток. Отключите отдельную поправку индуцированной тяги.",
            ["This empirical lift bundle requires static base performance; do not stack it on an RPM/J map."]="Эмпирическая поправка подъёмной силы применяется к статическим характеристикам; её нельзя складывать с картой RPM/J.",
            ["Test pilot requires four rotors."]="Текущему пилоту нужны четыре ротора.",
            ["Test pilot requires +Y rotor axes."]="Текущему пилоту нужны оси тяги +Y у всех роторов.",
            ["Rotor layout cannot control all four axes."]="Расположение роторов не позволяет независимо управлять общей тягой и тремя моментами.",
            ["Rotor layout cannot produce positive collective with zero torque."]="Эта геометрия не позволяет получить положительную общую тягу без результирующего момента."
        };
        public static string Message(string text)
        {
            if(Messages.TryGetValue(text,out var result))return result;
            if(text.StartsWith("Expected "))return "Неверный тип данных: ожидается "+text.Substring(9)+".";
            if(text.StartsWith("Unknown value: "))return "Неизвестный вариант: "+text.Substring(15);
            if(text.StartsWith("Bounded empirical airflow corrections:"))return "Поправки аэродинамики ограничены заданной областью скоростей. Коэффициентам нужны собственные источники; момент и ток остаются в базовой модели, без связанного энергетического расчёта.";
            if(text.StartsWith("Effective lumped motor/ESC/battery temperatures"))return "Температуры двигателя, ESC и батареи описывает модель сосредоточенных тепловых узлов с оценочным охлаждением и снижением тока. Она не описывает химию ячеек, потерю ёмкости на холоде, тепловой разгон, внутренние градиенты и активное торможение. Коэффициентам нужны источники.";
            if(text.StartsWith("Estimated DC/BLDC equivalent currents;"))return "Токи оцениваются эквивалентной моделью двигателя без рекуперации и индуктивности; измеренный CSV-ток хранится отдельно. Баланс моментов учитывает энергию раскрутки, первый порядок — нет. Электрическая модель выводит потери из Kv, R и тока холостого хода; постоянный КПД применяется в упрощённой.";
            if(text.StartsWith("Reject map cannot cover RPM=0 startup:"))return "Запрет выхода за карту может остановить расчёт при раскрутке: малые положительные RPM ниже первой строки не покрываются. Для динамики двигателя рекомендуется использовать граничные значения.";
            return text;
        }
        public static string Path(string path)=>Regex.Replace(path.Replace("$.",""),@"[A-Za-z_][A-Za-z_0-9]*",m=>DroneParameterSchema.Name(m.Value)).Replace("."," → ");
    }
}
