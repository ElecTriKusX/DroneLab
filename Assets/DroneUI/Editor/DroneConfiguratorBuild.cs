using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DroneLab.UI.Editor
{
    /// <summary>Runtime models have no material references in scenes; retain their HDRP shaders in players.</summary>
    [InitializeOnLoad]
    public sealed class DroneConfiguratorBuild : IPreprocessBuildWithReport
    {
        private static readonly string[] Paths = {
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-pbrMetallicRoughness.shadergraph",
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-pbrSpecularGlossiness.shadergraph",
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-unlit.shadergraph",
            "Packages/com.unity.cloud.gltfast/Runtime/Shader/HDRP/glTF-pbrMetallicRoughnessStackLit.shadergraph"
        };
        public int callbackOrder => -1000;
        static DroneConfiguratorBuild() { EditorApplication.delayCall += () => IncludeShaders(false); }
        public void OnPreprocessBuild(BuildReport report) => IncludeShaders(true);
        private static void IncludeShaders(bool require)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets.Length == 0) { if (require) throw new BuildFailedException("DroneLab: Graphics Settings недоступны."); return; }
            var settings = new SerializedObject(assets[0]);
            var included = settings.FindProperty("m_AlwaysIncludedShaders");
            var shaders = new List<Shader>();
            foreach (string path in Paths) {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) { if (require) throw new BuildFailedException("DroneLab: glTF shader не импортирован: " + path); return; }
                shaders.Add(shader);
            }
            bool changed = false;
            foreach (var shader in shaders) {
                bool present = false;
                for (int i=0;i<included.arraySize;i++) if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                if (present) continue;
                int index = included.arraySize; included.InsertArrayElementAtIndex(index);
                included.GetArrayElementAtIndex(index).objectReferenceValue = shader; changed = true;
            }
            if (changed) { settings.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); }
        }
    }
}
