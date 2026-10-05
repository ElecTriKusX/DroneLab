using UnityEngine;

namespace DroneLab.Simulation
{
    public sealed class ScaleReferenceLabels : MonoBehaviour
    {
        public Camera viewCamera;
        public Vector3[] labelPositionsLocal;
        public string[] labels;
        private void OnGUI()
        {
            var camera=viewCamera != null ? viewCamera : Camera.main;
            if(camera == null || labels == null || labelPositionsLocal == null) return;
            for(int i=0;i<Mathf.Min(labels.Length,labelPositionsLocal.Length);i++)
            {
                var screen=camera.WorldToScreenPoint(transform.TransformPoint(labelPositionsLocal[i]));
                if(screen.z<=camera.nearClipPlane || screen.x<0 || screen.x>Screen.width || screen.y<0 || screen.y>Screen.height) continue;
                GUI.Box(new Rect(screen.x-65,Screen.height-screen.y-12,130,24),labels[i]);
            }
        }
    }
}
