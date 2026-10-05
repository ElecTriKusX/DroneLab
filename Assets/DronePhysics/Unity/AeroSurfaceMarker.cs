using UnityEngine;

namespace DroneLab.Simulation
{
    // A two-sided pressure patch; +Y is the normal. Area is explicitly in square meters.
    public sealed class AeroSurfaceMarker : MonoBehaviour
    {
        public string surfaceId="surface";
        [Min(0.000001f)] public float areaM2=0.04f;
        [Min(0)] public float dragCoefficient=1;
        private void OnDrawGizmos()
        {
            Gizmos.color=Color.green;
            var old=Gizmos.matrix; Gizmos.matrix=Matrix4x4.TRS(transform.position,transform.rotation,Vector3.one);
            float side=Mathf.Sqrt(Mathf.Max(0,areaM2));
            Gizmos.DrawWireCube(Vector3.zero,new Vector3(side,0,side)); Gizmos.matrix=old;
            Gizmos.DrawLine(transform.position,transform.position+transform.up*0.12f);
        }
    }
}
