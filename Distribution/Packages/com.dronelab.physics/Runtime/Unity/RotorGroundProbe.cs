using System;
using DroneLab.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DroneLab.Simulation
{
    internal sealed class RotorGroundProbe
    {
        private RaycastHit[] hits=new RaycastHit[32];
        private bool warned;
        // Shared reusable buffer; grow only if needed, never select a possibly incomplete hit set.
        public bool Sample(DronePhysicsBody owner,Vector3 origin,Vector3 unitAxis,float radius,int layerMask,out RaycastHit nearest)
        {
            var scene=owner.gameObject.scene.GetPhysicsScene(); nearest=default;
            if(!scene.IsValid() || layerMask==0) return false;
            float range=radius*(float)RotorAerodynamics.ProbeRadiusLimit;
            int count;
            while(true)
            {
                count=scene.Raycast(origin,-unitAxis,hits,range,layerMask,QueryTriggerInteraction.Ignore);
                if(count<hits.Length) break;
                if(hits.Length>=2048)
                {
                    if(!warned) { Debug.LogWarning("DroneLab: ground probe exceeded 2048 hits; ground effect omitted for this probe. Narrow Ground Layers.",owner); warned=true; }
                    return false;
                }
                Array.Resize(ref hits,hits.Length*2);
            }
            float closest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i]; var collider=hit.collider;
                if(collider==null || collider.attachedRigidbody==owner.Body || collider.transform==owner.transform || collider.transform.IsChildOf(owner.transform)) continue;
                if(Vector3.Dot(unitAxis,hit.normal)<=RotorAerodynamics.MinimumNormalAlignment || hit.distance>=closest) continue;
                closest=hit.distance; nearest=hit;
            }
            return !float.IsPositiveInfinity(closest);
        }
    }
}
