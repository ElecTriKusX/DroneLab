using UnityEngine;

namespace DroneLab.Simulation
{
    [DisallowMultipleComponent,RequireComponent(typeof(DronePhysicsBody))]
    public sealed class DroneGeometryAuthoring : MonoBehaviour
    {
        [Tooltip("Only the selected subtree is baked. Use the stationary body, excluding spinning propellers.")]
        public Transform bodyMeshRoot;
        public Transform centerOfMassMarker,dragPointMarker;
        public RotorGeometryMarker[] rotors;
        public AeroSurfaceMarker[] surfaces;
        [Min(0)] public float projectedDragCoefficient=1;
        [Range(16,512)] public int silhouetteResolution=128;
        private void OnDrawGizmos()
        {
            if(centerOfMassMarker!=null) { Gizmos.color=Color.yellow; Gizmos.DrawWireSphere(centerOfMassMarker.position,0.025f); }
            if(dragPointMarker!=null) { Gizmos.color=Color.red; Gizmos.DrawWireSphere(dragPointMarker.position,0.025f); }
        }
    }
}
