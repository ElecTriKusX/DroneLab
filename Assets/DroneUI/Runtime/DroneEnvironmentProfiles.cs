using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DroneLab.Physics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DroneLab.UI
{
    /// <summary>UI envelope. Environment remains the unmodified physics 1.0.0 contract.</summary>
    public sealed class DroneEnvironmentDocument
    {
        public int documentVersion = 1;
        public string id;
        public string name;
        public JObject environment;
        public JObject visual = new JObject { ["timeOfDay"] = 14.0, ["simulateTime"] = false, ["weatherPresetId"] = "" };
        [JsonIgnore] public bool builtIn;
        public DroneEnvironmentDocument Copy() => new DroneEnvironmentDocument {
            id = id, name = name, environment = (JObject)environment.DeepClone(),
            visual = (JObject)visual.DeepClone(), builtIn = builtIn
        };
    }

    public static class DroneEnvironmentProfiles
    {
        public static string Folder => Path.Combine(Application.persistentDataPath, "DroneLab", "EnvironmentProfiles");
        public static string ExchangeFolder => Path.Combine(Folder, "Exchange");
        public static readonly JObject Defaults = JObject.Parse(@"{
            'schemaVersion':'1.0.0','gravityMps2':9.81,'airDensityMode':'Constant','airDensityKgM3':1.225,
            'temperatureK':288.15,'pressurePa':101325,'altitudeM':0,'windMode':'None',
            'windVelocityWorldMps':[0,0,0],'gustEnabled':false,'gustIntensityMps':0,'gustTimeScaleS':2,
            'turbulenceSeed':48271,'weather':{'precipitation':'None','intensityMmPerHour':0,'model':'VisualOnly'},
            'dryden':{'sigmaUvwMps':[0.4,0.4,0.25],'lengthScaleUvwM':[20,20,10],
            'advectionDirectionWorld':[1,0,0],'advectionSpeedMps':5,'modesPerComponent':32,
            'minDimensionlessWaveNumber':0.02,'maxDimensionlessWaveNumber':20}}");

        public static DroneEnvironmentDocument New() => new DroneEnvironmentDocument {
            id = Guid.NewGuid().ToString("N"), name = "Новый профиль", environment = (JObject)Defaults.DeepClone()
        };
        public static List<DroneEnvironmentDocument> LoadAll(out string warnings)
        {
            var result = new List<DroneEnvironmentDocument>(); var errors = new List<string>();
            foreach (var asset in Resources.LoadAll<TextAsset>("DronePhysics").OrderBy(x => x.name))
            {
                if (!asset.name.StartsWith("environment_", StringComparison.Ordinal)) continue;
                try {
                    var d = New(); d.id = "builtin-" + asset.name; d.name = Title(asset.name);
                    d.environment = JObject.Parse(asset.text); d.builtIn = true; result.Add(d);
                } catch (JsonException ex) { errors.Add(asset.name + ": " + ex.Message); }
            }
            Directory.CreateDirectory(Folder);
            foreach (var file in Directory.GetFiles(Folder, "*.json").OrderBy(x => x))
                try { var d = Read(File.ReadAllText(file)); Validate(d); result.Add(d); }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is JsonException) { errors.Add(Path.GetFileName(file) + ": " + ex.Message); }
            warnings = string.Join("\n", errors);
            return result;
        }
        private static string Title(string name)
        {
            switch (name) {
                case "environment_calm": return "Штиль / день";
                case "environment_wind": return "Постоянный ветер";
                case "environment_gust": return "Порывы ветра";
                case "environment_turbulence": return "Турбулентность";
                case "environment_dryden_frozen": return "Турбулентность Dryden";
                case "environment_atmosphere": return "Стандартная атмосфера";
                case "environment_field": return "Ветер Enviro / внешнее поле";
                case "environment_thermal_rain": return "Дождь";
                case "environment_thermal_snow": return "Снег";
                case "environment_thermal_hail": return "Град";
                case "environment_thermal_warm": return "Тёплый воздух";
                case "environment_final_acceptance": return "Комплексные условия";
                default: return name;
            }
        }
        public static JObject Effective(JObject source)
        {
            var value = (JObject)Defaults.DeepClone();
            value.Merge(source, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            return value;
        }
        public static string PhysicsJson(DroneEnvironmentDocument d) => Effective(d.environment).ToString(Formatting.Indented);
        public static void Validate(DroneEnvironmentDocument d, TextAsset drone = null)
        {
            if (d == null || d.documentVersion != 1 || d.environment == null || d.visual == null)
                throw new ArgumentException("Неподдерживаемый или неполный профиль среды.");
            if (string.IsNullOrWhiteSpace(d.name) || d.name.Length > 100) throw new ArgumentException("Название: от 1 до 100 символов.");
            var ds = Resources.Load<TextAsset>("DronePhysics/drone-profile.schema");
            var es = Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
            var reference = drone ?? Resources.Load<TextAsset>("DronePhysics/quad_test_ctcq");
            if (reference == null || ds == null || es == null) throw new ArgumentException("Не найдены схемы физики или контрольный профиль дрона.");
            var schemaErrors = ContractValidator.Validate(d.environment, JObject.Parse(es.text));
            if (schemaErrors.Count > 0) throw new ArgumentException(string.Join("\n", schemaErrors));
            var result = ProfileLoader.Load(reference.text, PhysicsJson(d), ds.text, es.text);
            if (!result.Success) throw new ArgumentException(string.Join("\n", result.Issues.Where(x => x.Severity == "Error")));
            Number(d.visual, "timeOfDay", 0, 23.999);
            Number(d.visual, "windTurbulence", 0, 10);
            Number(d.visual, "cloudCoverage", 0, 1); Number(d.visual, "cloudDensity", 0, 10);
            Number(d.visual, "wetness", 0, 1); Number(d.visual, "snow", 0, 1);
            Number(d.visual, "fogDistance", 1, 100000); Number(d.visual, "fogBaseHeight", -10000, 10000);
            Number(d.visual, "fogMaxHeight", -10000, 10000);
            if (d.visual["fogBaseHeight"] != null && d.visual["fogMaxHeight"] != null &&
                (double)d.visual["fogMaxHeight"] <= (double)d.visual["fogBaseHeight"])
                throw new ArgumentException("Верхняя граница тумана должна быть выше нижней.");
            if (d.visual["simulateTime"]?.Type != JTokenType.Boolean || d.visual["weatherPresetId"]?.Type != JTokenType.String)
                throw new ArgumentException("Неверные параметры времени или погодного пресета.");
            var allowed = new HashSet<string> { "timeOfDay", "simulateTime", "weatherPresetId", "windTurbulence", "cloudCoverage", "cloudDensity", "wetness", "snow", "fogDistance", "fogBaseHeight", "fogMaxHeight" };
            if (d.visual.Properties().Any(p => !allowed.Contains(p.Name))) throw new ArgumentException("Неизвестный визуальный параметр.");
            if (Effective(d.environment).Descendants().OfType<JValue>().Any(v =>
                (v.Type == JTokenType.Float || v.Type == JTokenType.Integer) && Math.Abs((double)v) > 1e12))
                throw new ArgumentException("Параметр среды превышает допустимый численный диапазон.");
        }
        private static void Number(JObject o, string key, double min, double max)
        {
            if (o[key] == null) return;
            if (o[key].Type != JTokenType.Float && o[key].Type != JTokenType.Integer) throw new ArgumentException("Число требуется: " + key);
            double n = (double)o[key];
            if (double.IsNaN(n) || double.IsInfinity(n) || n < min || n > max) throw new ArgumentException("Недопустимое значение: " + key);
        }
        public static DroneEnvironmentDocument Read(string json)
        {
            if (json.Length > 1024 * 1024) throw new ArgumentException("Файл профиля слишком большой.");
            using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 32, DateParseHandling = DateParseHandling.None }) {
                var value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new ArgumentException("Лишние данные после JSON.");
                // A plain physics JSON is also accepted; it keeps all original fields.
                var d = value["environment"] == null ? New() : value.ToObject<DroneEnvironmentDocument>();
                if (value["environment"] == null) { d.environment = value; d.name = "Импортированный профиль"; }
                d.builtIn = false;
                return d;
            }
        }
        private static string PathFor(string id)
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Неверный идентификатор пользовательского профиля.");
            return Path.Combine(Folder, id + ".json");
        }
        public static DroneEnvironmentDocument Save(DroneEnvironmentDocument draft)
        {
            Validate(draft);
            var saved = draft.Copy();
            if (saved.builtIn || !Guid.TryParseExact(saved.id, "N", out _)) saved.id = Guid.NewGuid().ToString("N");
            saved.builtIn = false;
            Directory.CreateDirectory(Folder);
            var path = PathFor(saved.id); var temp = path + ".tmp";
            File.WriteAllText(temp, JsonConvert.SerializeObject(saved, Formatting.Indented));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
            return saved;
        }
        public static void Delete(DroneEnvironmentDocument d)
        { if (d.builtIn) throw new ArgumentException("Встроенный профиль нельзя удалить."); File.Delete(PathFor(d.id)); }
        public static string Export(DroneEnvironmentDocument d)
        {
            Validate(d); Directory.CreateDirectory(ExchangeFolder);
            string path = Path.Combine(ExchangeFolder, "environment-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(d, Formatting.Indented)); return path;
        }
        public static DroneEnvironmentDocument Import(string path)
        {
            if (new FileInfo(path).Length > 1024 * 1024) throw new ArgumentException("Файл профиля слишком большой.");
            var d = Read(File.ReadAllText(path)); d.id = Guid.NewGuid().ToString("N"); return Save(d);
        }
    }
}
