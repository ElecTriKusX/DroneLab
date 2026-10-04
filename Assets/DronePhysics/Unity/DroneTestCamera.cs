using UnityEngine;

namespace DroneLab.Simulation
{
    public sealed class DroneTestCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 headingOffset = new Vector3(0,2,-5);
        public float followSpeed = 6;
        private void LateUpdate()
        {
            if(target == null) return;
            var heading=Quaternion.Euler(0,target.eulerAngles.y,0);
            var desired=target.position+heading*headingOffset;
            transform.position=Vector3.Lerp(transform.position,desired,1-Mathf.Exp(-followSpeed*Time.deltaTime));
            var direction=target.position-transform.position;
            if(direction.sqrMagnitude>1e-6f) transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
        }
    }
}
