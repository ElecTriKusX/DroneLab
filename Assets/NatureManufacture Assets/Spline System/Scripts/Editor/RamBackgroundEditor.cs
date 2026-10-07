using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[InitializeOnLoad]
public class RamBackgroundEditor : MonoBehaviour
{
    static RamBackgroundEditor()
    {
#if UNITY_2019_1_OR_NEWER
        SceneView.duringSceneGui += OnSceneGUI;
#else
        SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        //if (Event.current.type.ToString().Contains("Drag"))
        //    Debug.Log(Event.current.type);

        if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
        {
            if (DragAndDrop.objectReferences.Length < 1)
                return;


            SplineProfile splineProfile = null;
            if (DragAndDrop.objectReferences[0] is SplineProfile)
            {
                splineProfile = (SplineProfile) DragAndDrop.objectReferences[0];


                GameObject go = HandleUtility.PickGameObject(Event.current.mousePosition, false);
                if (go != null)
                {
                    RamSpline ramSpline = go.GetComponent<RamSpline>();


                    if (ramSpline != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy; // show a drag-add icon on the mouse cursor

                        if (Event.current.type == EventType.DragPerform)
                        {
                            Undo.RecordObject(ramSpline, "River changed");

                            RamSplineEditor ramSplineEditor = (RamSplineEditor) Editor.CreateEditor(ramSpline);

                            ramSpline.currentProfile = splineProfile;
                            ramSplineEditor.ResetToProfile();
                            
                            ramSpline.GenerateSpline();
                            EditorUtility.SetDirty(ramSpline);
                            Editor.DestroyImmediate(ramSplineEditor);
                            DragAndDrop.AcceptDrag();
                            Event.current.Use();
                            return;
                        }
                    }
                }

                Event.current.Use();
            }


            LakePolygonProfile lakePolygonProfile = null;
            if (DragAndDrop.objectReferences[0] is LakePolygonProfile)
            {
                lakePolygonProfile = (LakePolygonProfile) DragAndDrop.objectReferences[0];


                //Debug.Log(lakePolygonProfile.name);
                GameObject go = HandleUtility.PickGameObject(Event.current.mousePosition, false);


                if (go != null)
                {
                    LakePolygon lakePolygon = go.GetComponent<LakePolygon>();

                    //Debug.Log(go.name);
                    if (lakePolygon != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy; // show a drag-add icon on the mouse cursor
                        //Debug.Log(Event.current.type);
                        if (Event.current.type == EventType.DragPerform)
                        {
                            //Debug.Log("EventType.DragPerform");
                            Undo.RecordObject(lakePolygon, "Lake changed");
                            LakePolygonEditor lakePolygonEditor = (LakePolygonEditor) Editor.CreateEditor(lakePolygon);

                            lakePolygon.currentProfile = lakePolygonProfile;
                            lakePolygonEditor.ResetToProfile();
                            lakePolygon.GeneratePolygon();
                            EditorUtility.SetDirty(lakePolygon);
                            Editor.DestroyImmediate(lakePolygonEditor);

                            DragAndDrop.AcceptDrag();
                            Event.current.Use();
                            return;
                        }
                    }
                }

                Event.current.Use();
            }
        }
    }
}