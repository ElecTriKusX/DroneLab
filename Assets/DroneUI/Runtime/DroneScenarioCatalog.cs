using System;
using System.Linq;
using System.Collections.Generic;
using Enviro;
using UnityEngine;

namespace DroneLab.UI
{
    [Serializable]
    public sealed class DroneMapEntry
    {
        public string id;
        public string title;
        public string terrainType;
        [Tooltip("Dimensions of the usable area in metres. Zero means not specified.")]
        public Vector2 sizeM;
        public Texture2D screenshot;
        [Tooltip("Full build scene path, for example Assets/Scenes/Forest.unity.")]
        public string scenePath;
    }

    [Serializable]
    public sealed class DroneWeatherEntry
    {
        public string id;
        public string title;
        public EnviroWeatherType preset;
        public Texture2D screenshot;
    }

    [CreateAssetMenu(menuName = "DroneLab/UI/Scenario Catalog")]
    public sealed class DroneScenarioCatalog : ScriptableObject
    {
        public List<DroneMapEntry> maps = new List<DroneMapEntry>();
        public List<DroneWeatherEntry> weatherPresets = new List<DroneWeatherEntry>();
        [Header("Названия эффектов Enviro (необязательно; иначе распознаются Rain / Snow / Hail)")]
        public string rainEffectName, snowEffectName, hailEffectName;
        public static DroneScenarioCatalog Load() => Resources.Load<DroneScenarioCatalog>("DroneLab/ScenarioCatalog");
        public DroneWeatherEntry Weather(string id) => weatherPresets.Find(x => x != null && x.id == id);
        public static bool IsPrecipitation(string name)
        {
            string key = (name ?? "").ToLowerInvariant();
            return key.Contains("rain") || key.Contains("snow") || key.Contains("hail") || key.Contains("дожд") || key.Contains("снег") || key.Contains("град");
        }
        public static string WeatherTitle(string name)
        {
            string key = (name ?? "").ToLowerInvariant().Replace(" ", "");
            if (key.Contains("clear") || key.Contains("sunny")) return "Ясно";
            if (key.Contains("fog")) return "Туман";
            if (key.Contains("storm") || key.Contains("thunder")) return "Грозовые облака";
            if (key.Contains("overcast")) return "Пасмурно";
            if (key.Contains("cloud")) {
                if (key.Contains("1")) return "Небольшая облачность";
                if (key.Contains("2")) return "Переменная облачность";
                if (key.Contains("4")) return "Пасмурно";
                return "Облачно";
            }
            if ((name ?? "").Any(c => c >= 'А' && c <= 'я')) return name;
            return "Небо и облака";
        }
        public List<DroneWeatherEntry> WeatherOptions() => weatherPresets.Where(w => w?.preset != null && !IsPrecipitation(w.preset.name))
            .GroupBy(w => WeatherTitle(w.title)).Select(g => g.First()).ToList();
        public static List<string> DisplayChoices(IEnumerable<string> names)
        {
            var used = new HashSet<string>(StringComparer.Ordinal); var result = new List<string>();
            foreach (var source in names) {
                string title = string.IsNullOrWhiteSpace(source) ? "Без названия" : source;
                string unique = title; int suffix = 2;
                while (!used.Add(unique)) unique = title + " (" + suffix++ + ")";
                result.Add(unique);
            }
            return result;
        }
    }
}
