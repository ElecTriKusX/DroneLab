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
    /// <summary>The physical JSON is authoritative. Visual authoring never round-trips through a partial DTO.</summary>
    [Serializable]
    public sealed class DroneProfileDocument
    {
        public string version = "1.0.0";
        public string id = Guid.NewGuid().ToString("N");
        public bool draft;
        // Catalog information belongs to the document envelope, not the physical schema.
        public string category = "Без категории";
        public JObject profile;
        public JObject visual = new JObject {
            ["modelFile"] = "", ["scale"] = 1.0,
            ["rotationEulerDeg"] = new JArray(0.0, 0.0, 0.0),
            ["centerModel"] = true, ["rotorNodes"] = new JObject(),
            ["previewOrthographic"] = false,
            ["previewYaw"] = 35.0, ["previewPitch"] = 25.0,
            ["previewDistance"] = 1.0, ["previewTarget"] = new JArray(0.0,0.0,0.0)
        };
        // Absolute paths belong only to the working copy, never to the physics contract.
        public string sourceModel;
        public Dictionary<string,string> unfinishedInputs = new Dictionary<string,string>();
        public string Name => (string)profile?["metadata"]?["name"] ?? "Без названия";
        [JsonIgnore] public string Category => string.IsNullOrWhiteSpace(category) ? "Без категории" : category.Trim();
        public DroneProfileDocument Copy() => JsonConvert.DeserializeObject<DroneProfileDocument>(JsonConvert.SerializeObject(this));
        public string Snapshot() => JsonConvert.SerializeObject(this, Formatting.None);
    }

    public static class DroneProfileLibrary
    {
        public static string Root => Path.Combine(Application.persistentDataPath, "DroneLibrary");
        public static string Folder(string id) {
            if (!Guid.TryParseExact(id, "N", out _) && !(id != null && id.StartsWith("builtin-",StringComparison.Ordinal) &&
                id.Substring(8).All(c => c < 128 && (char.IsLetterOrDigit(c) || c == '_'))))
                throw new ArgumentException("Некорректный идентификатор дрона.");
            return Path.Combine(Root, id);
        }
        public static DroneProfileDocument Create()
        {
            var result = new DroneProfileDocument { draft = true, profile = JObject.Parse(Resource("quad_test_basic")) };
            result.profile["metadata"] = new JObject { ["name"] = "Новый дрон", ["description"] = "Стартовый оценочный профиль. Уточните характеристики по данным своего аппарата." };
            return result;
        }
        public static string Resource(string name) => Resources.Load<TextAsset>("DronePhysics/" + name)?.text ?? throw new InvalidOperationException("Не найден профиль или схема физики: " + name);
        public static List<DroneProfileDocument> LoadAll(out string warnings)
        {
            var list = new List<DroneProfileDocument>(); var problems = new List<string>();
            var hidden = HiddenBuiltins();
            var names = new[] { "quad_test_basic", "reference_crazyflie20", "reference_crazyflie_brushless", "reference_hummingbird", "quad_test_thermal" };
            var titles = new[] { "Учебный квадрокоптер", "Crazyflie 2.0", "Crazyflie Brushless", "Hummingbird", "Инженерный: питание и нагрев" };
            for (int i=0;i<names.Length;i++) {
                if (hidden.Contains("builtin-" + names[i])) continue;
                var item = new DroneProfileDocument { id = "builtin-" + names[i], category = i == 0 ? "Учебные" : "Исследовательские", profile = JObject.Parse(Resource(names[i])) };
                item.profile["metadata"]["name"] = titles[i]; list.Add(item);
            }
            Directory.CreateDirectory(Root);
            foreach (string directory in Directory.GetDirectories(Root)) {
                try {
                    string document = Path.Combine(directory,"document.json"); if (!File.Exists(document)) continue;
                    var item = JsonConvert.DeserializeObject<DroneProfileDocument>(File.ReadAllText(document));
                    if (item?.profile == null || Path.GetFullPath(directory) != Path.GetFullPath(Folder(item.id))) throw new ArgumentException("Повреждённый профиль");
                    if (hidden.Contains(item.id)) continue;
                    PrepareForEditing(item);
                    var previous = list.FindIndex(x=>x.id == item.id); if (previous >= 0) list[previous] = item; else list.Add(item);
                } catch (Exception ex) { problems.Add(Path.GetFileName(directory) + ": " + ex.Message); }
            }
            warnings = string.Join("\n",problems); return list;
        }
        private static HashSet<string> HiddenBuiltins()
        {
            string path = Path.Combine(Root, "hidden-builtins.json");
            return File.Exists(path) ? new HashSet<string>(JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path)) ?? new List<string>()) : new HashSet<string>();
        }
        public static void Delete(DroneProfileDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            string folder = Folder(document.id);
            Directory.CreateDirectory(Root);
            string trash = Path.Combine(Root, ".trash", document.id + "-" + Guid.NewGuid().ToString("N"));
            bool moved = Directory.Exists(folder);
            if (moved) { Directory.CreateDirectory(Path.GetDirectoryName(trash)); Directory.Move(folder, trash); }
            try {
                if (document.id.StartsWith("builtin-", StringComparison.Ordinal)) {
                    var hidden = HiddenBuiltins(); hidden.Add(document.id);
                    Write(Root, "hidden-builtins.json", JsonConvert.SerializeObject(hidden, Formatting.Indented));
                }
            } catch {
                if (moved && Directory.Exists(trash)) Directory.Move(trash, folder);
                throw;
            }
            // Removing from the catalog is committed; an unused recovery directory may be cleaned later.
            if (moved) try { Directory.Delete(trash, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        public static ProfileLoadResult Validate(DroneProfileDocument document, string environmentJson = null)
        {
            var environment = JObject.Parse(environmentJson ?? Resource("environment_calm"));
            // Calibration preview: measured curves must use their reference density, not today's weather.
            if (environmentJson == null) {
                environment["temperatureK"] = 293.15;
                var measured = ((JArray)document.profile["rotors"]).OfType<JObject>().FirstOrDefault(r =>
                    (string)r["performance"]?["model"] == "OmegaSquared" || (string)r["performance"]?["model"] == "RpmTable");
                if (measured?["performance"]?["referenceAirDensityKgM3"] != null)
                    environment["airDensityKgM3"] = measured["performance"]["referenceAirDensityKgM3"].DeepClone();
            }
            return ProfileLoader.Load(document.profile.ToString(),environment.ToString(), Resource("drone-profile.schema"),Resource("environment-profile.schema"));
        }
        public static void Save(DroneProfileDocument document, bool asDraft)
        {
            if (!asDraft) {
                if(document.unfinishedInputs.Count>0)throw new ArgumentException("Профиль содержит незавершённые значения. Исправьте их или сохраните черновик.");
                var result = Validate(document);
                if (!result.Success) throw new ArgumentException(string.Join("\n", result.Issues.Where(x=>x.Severity == "Error").Select(x=>DroneValidationText.Path(x.Path) + ": " + DroneValidationText.Message(x.Message))));
            }
            Directory.CreateDirectory(Root);
            string folder=Folder(document.id),staging=folder+".tmp-"+Guid.NewGuid().ToString("N"),backup=folder+".previous-"+Guid.NewGuid().ToString("N");
            var saved=document.Copy();saved.draft=asDraft;
            saved.category=saved.Category;
            try {
                Directory.CreateDirectory(staging);
                if(Directory.Exists(folder))CopyDirectory(folder,staging);
                if(!string.IsNullOrWhiteSpace(saved.sourceModel)) {
                    string model=DroneModelFiles.Copy(saved.sourceModel,Path.Combine(staging,"model"));
                    saved.visual["modelFile"]="model/"+Path.GetFileName(model);saved.sourceModel=null;
                }
                Write(staging,"document.json",JsonConvert.SerializeObject(saved,Formatting.Indented));
                Write(staging,asDraft ? "draft.json" : "profile.json",saved.profile.ToString());
                File.Delete(Path.Combine(staging,asDraft ? "profile.json" : "draft.json"));
                Write(staging,"asset-bindings.json",saved.visual.ToString());
                Write(staging,"package-manifest.json",new JObject {
                    ["version"]="1.0.0",["profileId"]=saved.id,["profileFile"]=asDraft ? "draft.json" : "profile.json",
                    ["modelFile"]=saved.visual["modelFile"],["assetBindingsFile"]="asset-bindings.json",["previewFile"]="preview.png"
                }.ToString());
                // Commit the complete package on the same filesystem; recover the old package on rename failure.
                if(Directory.Exists(folder))Directory.Move(folder,backup);
                try {Directory.Move(staging,folder);}
                catch {if(Directory.Exists(backup))Directory.Move(backup,folder);throw;}
                document.visual=saved.visual;document.sourceModel=saved.sourceModel;document.draft=asDraft;document.category=saved.category;
            } finally {
                if(Directory.Exists(staging))Directory.Delete(staging,true);
                if(Directory.Exists(backup) && Directory.Exists(folder))try{Directory.Delete(backup,true);}catch(IOException){ /* Recovery copy is harmless. */ }
            }
        }
        private static void Write(string folder,string file,string text)=>DroneLab.Configurator.DroneConfiguratorStorage.AtomicWrite(Path.Combine(folder,file),text);
        private static void CopyDirectory(string source,string destination)
        {
            foreach(string directory in Directory.GetDirectories(source)) {string target=Path.Combine(destination,Path.GetFileName(directory));Directory.CreateDirectory(target);CopyDirectory(directory,target);}
            foreach(string file in Directory.GetFiles(source))File.Copy(file,Path.Combine(destination,Path.GetFileName(file)),true);
        }

        public static string ModelPath(DroneProfileDocument document)
        {
            if (!string.IsNullOrEmpty(document.sourceModel)) return document.sourceModel;
            var model = (string)document.visual["modelFile"]; if (string.IsNullOrEmpty(model)) return null;
            return DroneModelFiles.Contained(Folder(document.id),model);
        }
        public static DroneProfileDocument Import(string path)
        {
            var token = JObject.Parse(File.ReadAllText(path));
            // Package document, strict profile, or the teammate's legacy draft.
            DroneProfileDocument item;
            if (token["profile"] is JObject) item = token.ToObject<DroneProfileDocument>();
            else if (token["draftVersion"] != null) {
                var legacy = token.ToObject<DroneLab.Configurator.DroneConfiguratorDraft>();
                item = new DroneProfileDocument { profile = JObject.Parse(DroneLab.Configurator.DroneConfiguratorProfileAdapter.BuildStrictProfileJson(legacy)), sourceModel = legacy.sourceModelPath };
                item.visual["scale"] = legacy.modelScaleMetersPerUnit;
            } else item = new DroneProfileDocument { profile = token };
            if (item.profile == null || item.profile["rotors"] is not JArray) throw new ArgumentException("Файл не содержит профиль дрона.");
            PrepareForEditing(item);
            item.id = Guid.NewGuid().ToString("N"); item.draft = true;
            var model = (string)item.visual["modelFile"];
            if (!string.IsNullOrEmpty(model)) item.sourceModel = DroneModelFiles.Contained(Path.GetDirectoryName(Path.GetFullPath(path)),model);
            return item;
        }
        public static void PrepareForEditing(DroneProfileDocument item)
        {
            if(item?.profile==null)throw new ArgumentException("Файл не содержит профиль дрона.");
            CheckShape(item.profile,DroneParameterSchema.ObjectRule("DroneProfile"),"profile");
            foreach(string key in new[]{"metadata","coordinateSystem","physicsConfiguration","massProperties","bodyAerodynamics","powerSystem"})
                if(item.profile[key] is not JObject)throw new ArgumentException("В профиле отсутствует блок «"+DroneParameterSchema.Name(key)+"».");
            if(item.profile["rotors"] is not JArray rotors || rotors.Count==0)throw new ArgumentException("В профиле должен быть хотя бы один ротор.");
            foreach(var rotor in rotors)foreach(string key in new[]{"geometry","motor","propeller","performance"})
                if(rotor[key] is not JObject)throw new ArgumentException("У ротора отсутствует блок «"+DroneParameterSchema.Name(key)+"».");
            if(item.profile["powerSystem"]["battery"] is not JObject)throw new ArgumentException("В профиле отсутствует блок батареи.");
            var visual=new DroneProfileDocument().visual;
            if(item.visual!=null)visual.Merge(item.visual,new JsonMergeSettings{MergeArrayHandling=MergeArrayHandling.Replace,MergeNullValueHandling=MergeNullValueHandling.Ignore});
            foreach(string key in new[]{"rotationEulerDeg","previewTarget"})
                if(visual[key] is not JArray vector || vector.Count!=3 || vector.Any(v=>!FiniteNumber(v)))throw new ArgumentException("Некорректный визуальный вектор: "+key);
            foreach(string key in new[]{"scale","previewYaw","previewPitch","previewDistance"})
                if(!FiniteNumber(visual[key]))throw new ArgumentException("Некорректная настройка 3D-вида: "+key);
            if((double)visual["scale"]<=0 || (double)visual["previewDistance"]<=0 || visual["rotorNodes"] is not JObject || visual["modelFile"].Type!=JTokenType.String || visual["centerModel"].Type!=JTokenType.Boolean || visual["previewOrthographic"].Type!=JTokenType.Boolean)
                throw new ArgumentException("Некорректные настройки визуальной модели.");
            item.visual=visual;item.unfinishedInputs??=new Dictionary<string,string>();
            item.category=item.Category;
        }
        private static bool FiniteNumber(JToken value)=>value!=null && (value.Type==JTokenType.Float || value.Type==JTokenType.Integer) && !double.IsNaN((double)value) && !double.IsInfinity((double)value) && Math.Abs((double)value)<=1e12;
        private static void CheckShape(JToken value,JToken unresolved,string path)
        {
            var rule=DroneParameterSchema.Resolve(unresolved);string type=(string)rule["type"];
            bool matches=type=="object"?value is JObject:type=="array"?value is JArray:type=="number" || type=="integer"?FiniteNumber(value):type=="boolean"?value.Type==JTokenType.Boolean:value.Type==JTokenType.String;
            if(!matches)throw new ArgumentException("Некорректный тип значения: "+path);
            if(value is JObject obj)foreach(var prop in obj.Properties())if(rule["properties"]?[prop.Name]!=null)CheckShape(prop.Value,rule["properties"][prop.Name],path+"."+prop.Name);
            if(value is JArray array){if(rule["maxItems"]!=null && array.Count!=(int)rule["maxItems"])throw new ArgumentException("Некорректное число компонентов: "+path);for(int i=0;i<array.Count;i++)CheckShape(array[i],rule["items"],path+"["+i+"]");}
        }
    }

    /// <summary>Copies glTF's relative resource tree. Never renames .gltf to .glb or drops textures.</summary>
    public static class DroneModelFiles
    {
        public static string Contained(string folder,string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || Uri.TryCreate(relative,UriKind.Absolute,out _))
                throw new ArgumentException("Ресурс модели должен иметь относительный путь.");
            string root = Path.GetFullPath(folder) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(folder,Uri.UnescapeDataString(relative).Replace('\\','/').Replace('/',Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root,StringComparison.Ordinal)) throw new ArgumentException("Ресурс модели выходит за пределы её папки.");
            return path;
        }
        public static string Copy(string source,string destination)
        {
            var files=Collect(source);
            // Validate every dependency before touching the destination; keep the original URI tree.
            Directory.CreateDirectory(destination);
            foreach (var file in files) {
                string target = Contained(destination,file.relative); Directory.CreateDirectory(Path.GetDirectoryName(target));
                if (Path.GetFullPath(file.source) != target) File.Copy(file.source,target,true);
            }
            return Contained(destination,Path.GetFileName(source));
        }
        public static void ValidateLocalModel(string source)=>Collect(source);
        private static List<(string source,string relative)> Collect(string source)
        {
            source = Path.GetFullPath(source); string extension = Path.GetExtension(source).ToLowerInvariant();
            if (extension != ".glb" && extension != ".gltf") throw new ArgumentException("Выберите GLB или glTF 2.0.");
            if (!File.Exists(source)) throw new FileNotFoundException("Файл модели не найден.",source);
            var files = new List<(string source,string relative)> { (source,Path.GetFileName(source)) };
            {
                var gltf = ReadModelJson(source);
                foreach (string collection in new[] { "buffers", "images" })
                    foreach (var token in gltf[collection] as JArray ?? new JArray()) {
                        var uri = (string)token["uri"]; if (string.IsNullOrEmpty(uri) || uri.StartsWith("data:",StringComparison.Ordinal)) continue;
                        string resource = Contained(Path.GetDirectoryName(source),uri);
                        if (!File.Exists(resource)) throw new FileNotFoundException("Не найден буфер или текстура: " + uri,resource);
                        files.Add((resource,Uri.UnescapeDataString(uri)));
                    }
            }
            return files;
        }
        public static JObject ReadModelJson(string source)
        {
            if (Path.GetExtension(source).Equals(".gltf",StringComparison.OrdinalIgnoreCase)) return JObject.Parse(File.ReadAllText(source));
            using var reader=new BinaryReader(File.OpenRead(source));
            if(reader.BaseStream.Length<20 || reader.ReadUInt32()!=0x46546C67 || reader.ReadUInt32()!=2)
                throw new ArgumentException("Ожидается GLB версии 2.");
            uint length=reader.ReadUInt32();if(length!=reader.BaseStream.Length)throw new ArgumentException("Повреждённая длина GLB.");
            uint jsonLength=reader.ReadUInt32();if(reader.ReadUInt32()!=0x4E4F534A || jsonLength>32*1024*1024 || jsonLength>length-20)
                throw new ArgumentException("Повреждённый JSON-блок GLB.");
            return JObject.Parse(System.Text.Encoding.UTF8.GetString(reader.ReadBytes((int)jsonLength)).TrimEnd('\0',' '));
        }
    }
}
