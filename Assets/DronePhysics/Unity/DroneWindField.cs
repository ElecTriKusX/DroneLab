using DroneLab.Physics;
using UnityEngine;

namespace DroneLab.Simulation
{
    [DisallowMultipleComponent]
    public sealed class DroneWindField : MonoBehaviour,IWindProvider
    {
        public Vector3 meanWindWorldMps=new Vector3(5,0,0);
        [Tooltip("Change in wind vector per metre of world X/Y/Z from this object's position.")]
        public Vector3 gradientX,gradientY=new Vector3(.1f,0,0),gradientZ;
        [Min(0)] public float maximumDeltaMps=10;
        public DVector3 Sample(DVector3 position,double time)
        {
            EnvironmentMath.Finite(position); EnvironmentMath.Finite(time);
            var origin=DronePhysicsBody.FromUnity(transform.position);
            var x=DronePhysicsBody.FromUnity(gradientX); var y=DronePhysicsBody.FromUnity(gradientY); var z=DronePhysicsBody.FromUnity(gradientZ);
            var mean=DronePhysicsBody.FromUnity(meanWindWorldMps);
            EnvironmentMath.Finite(x); EnvironmentMath.Finite(y); EnvironmentMath.Finite(z); EnvironmentMath.Finite(mean); EnvironmentMath.Finite(maximumDeltaMps);
            if(maximumDeltaMps<0) throw new System.ArgumentOutOfRangeException(nameof(maximumDeltaMps));
            var d=position-origin;
            return mean+EnvironmentMath.Limit(x*d.X+y*d.Y+z*d.Z,maximumDeltaMps);
        }
        private void OnDrawGizmosSelected()
        { Gizmos.color=Color.blue; Gizmos.DrawLine(transform.position,transform.position+meanWindWorldMps); }
    }
}
