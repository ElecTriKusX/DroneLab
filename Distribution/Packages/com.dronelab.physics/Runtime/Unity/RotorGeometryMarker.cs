using UnityEngine;

namespace DroneLab.Simulation
{
    // Authoring marker only; local +Y is the rotor thrust axis. No runtime forces.
    public sealed class RotorGeometryMarker : MonoBehaviour
    {
        [Tooltip("Authoring only. Stop Play and export Marker Geometry Only to apply position, +Y thrust axis and spin to the selected JSON profile.")]
        public string rotorId;
        public bool clockwise=true;
        private void OnDrawGizmos()
        {
            Gizmos.color=clockwise ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position,0.015f);
            Gizmos.DrawLine(transform.position,transform.position+transform.up*0.12f);
        }
    }
}
