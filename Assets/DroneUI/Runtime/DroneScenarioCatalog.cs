using System;
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
        public static DroneScenarioCatalog Load() => Resources.Load<DroneScenarioCatalog>("DroneLab/ScenarioCatalog");
        public DroneWeatherEntry Weather(string id) => weatherPresets.Find(x => x != null && x.id == id);
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
