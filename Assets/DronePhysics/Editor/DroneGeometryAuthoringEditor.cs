using DroneLab.Simulation;
using UnityEditor;
using UnityEngine;

namespace DroneLab.Editor
{
    [CustomEditor(typeof(DroneGeometryAuthoring))]
    public sealed class DroneGeometryAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Move markers, then export a new profile. Marker edits have no effect until export. +Y is thrust/patch normal. Cd, COM and mass distribution require user data.",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if(GUILayout.Button("Export Box Drag Profile")) { Selection.activeGameObject=((DroneGeometryAuthoring)target).gameObject; DroneGeometryMenu.ExportBox(); }
                if(GUILayout.Button("Export Mesh Silhouette Profile")) { Selection.activeGameObject=((DroneGeometryAuthoring)target).gameObject; DroneGeometryMenu.ExportMesh(); }
                if(GUILayout.Button("Export Surfaces Profile")) { Selection.activeGameObject=((DroneGeometryAuthoring)target).gameObject; DroneGeometryMenu.ExportSurfaces(); }
            }
        }
        private void OnSceneGUI()
        {
            var author=(DroneGeometryAuthoring)target;
            if(author.rotors!=null) foreach(var rotor in author.rotors)
                if(rotor!=null) Handles.Label(rotor.transform.position,rotor.rotorId+" "+(rotor.clockwise ? "CW":"CCW"));
            if(author.centerOfMassMarker!=null) Handles.Label(author.centerOfMassMarker.position,"COM");
            if(author.dragPointMarker!=null) Handles.Label(author.dragPointMarker.position,"Drag point");
        }
    }
}
