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
            documentVersion = documentVersion, id = id, name = name, environment = (JObject)environment.DeepClone(),
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
            var store = new DroneEnvironmentProfileStore(Folder); HashSet<string> removed;
            try { removed = store.Removed(); }
            catch (Exception ex) when (ex is IOException || ex is JsonException) { removed = new HashSet<string>(); errors.Add("Не удалось прочитать список удалённых профилей: " + ex.Message); }
            foreach (var asset in Resources.LoadAll<TextAsset>("DroneLab/EnvironmentPresets").OrderBy(x => x.name))
                try {
                    var d = Read(asset.text); Validate(d);
                    if (!DroneEnvironmentProfileStore.ValidId(d.id) || !d.id.StartsWith("builtin-environment_", StringComparison.Ordinal))
                        throw new ArgumentException("Неверный идентификатор стартового профиля.");
                    if (result.Any(p => p.id == d.id)) throw new ArgumentException("Повторяющийся идентификатор стартового профиля.");
                    d.builtIn = true;
                    if (!removed.Contains(d.id)) result.Add(d);
                } catch (Exception ex) when (ex is JsonException || ex is ArgumentException) { errors.Add(asset.name + ": " + ex.Message); }
            Directory.CreateDirectory(Folder);
            foreach (var file in store.Files())
                try { var d = Read(File.ReadAllText(file)); Validate(d);
                    if (!DroneEnvironmentProfileStore.ValidId(d.id)) throw new ArgumentException("Неверный идентификатор профиля.");
                    result.RemoveAll(p => p.id == d.id); if (!removed.Contains(d.id)) result.Add(d); }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is JsonException) { errors.Add(Path.GetFileName(file) + ": " + ex.Message); }
            warnings = string.Join("\n", errors);
            return result;
        }
        public static JObject Effective(JObject source)
        {
            var value = (JObject)Defaults.DeepClone();
            value.Merge(source, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
            return value;
        }
        public static string PhysicsJson(DroneEnvironmentDocument d) => Effective(d.environment).ToString(Formatting.Indented);
        public static void Validate(DroneEnvironmentDocument d, TextAsset drone = null, DroneScenarioCatalog catalog = null)
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
            var fieldErrors = DroneEnvironmentFields.Errors(d,catalog != null ? catalog : DroneScenarioCatalog.Load());
            if (fieldErrors.Count > 0) throw new ArgumentException(string.Join("\n", fieldErrors.Select(x => x.Key + ": " + x.Value)));
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
        public static DroneEnvironmentDocument Save(DroneEnvironmentDocument draft, DroneScenarioCatalog catalog = null)
        {
            Validate(draft,null,catalog); var saved = draft.Copy();
            if (!DroneEnvironmentProfileStore.ValidId(saved.id)) saved.id = Guid.NewGuid().ToString("N");
            saved.builtIn = false; new DroneEnvironmentProfileStore(Folder).Save(saved); return saved;
        }
        public static void Delete(DroneEnvironmentDocument document) => new DroneEnvironmentProfileStore(Folder).Delete(document);
        public static string Export(DroneEnvironmentDocument document, string destination = null, DroneScenarioCatalog catalog = null)
        {
            Validate(document,null,catalog); Directory.CreateDirectory(ExchangeFolder);
            string path = destination ?? Path.Combine(ExchangeFolder, "environment-" + Guid.NewGuid().ToString("N") + ".json");
            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) path += ".json";
            DroneEnvironmentProfileStore.Atomic(path, JsonConvert.SerializeObject(document, Formatting.Indented)); return path;
        }
        public static DroneEnvironmentDocument Import(string path)
        {
            if (new FileInfo(path).Length > 1024 * 1024) throw new ArgumentException("Файл профиля слишком большой.");
            var d = Read(File.ReadAllText(path)); d.id = Guid.NewGuid().ToString("N"); return Save(d);
        }
    }
}
