using System;
using UnityEngine;

namespace DroneLab.Simulation
{
    [DisallowMultipleComponent,RequireComponent(typeof(DronePhysicsBody)),DefaultExecutionOrder(-50)]
    public sealed class DroneMotorFaultScenario : MonoBehaviour
    {
        public bool automaticFault;
        [Min(0)] public int rotorIndex;
        [Min(0)] public float secondsAfterArm=5;
        [Range(0,1)] public float remainingDrive;
        private DronePhysicsBody body;
        private double armedAt=-1,lastTime=-1;
        private bool injected;
        private void Awake()=>body=GetComponent<DronePhysicsBody>();
        private void FixedUpdate()
        {
            if(body==null || !body.IsReady) return;
            if(!body.Armed || body.SimulationTimeS<lastTime) { armedAt=-1; injected=false; }
            lastTime=body.SimulationTimeS;
            if(!body.Armed) return;
            if(armedAt<0) armedAt=body.SimulationTimeS;
            if(automaticFault && !injected && body.SimulationTimeS-armedAt>=Math.Max(0,secondsAfterArm)) InjectFault();
        }
        [ContextMenu("Inject selected motor fault")]
        public void InjectFault()
        {
            if(!Application.isPlaying) return;
            if(body==null) body=GetComponent<DronePhysicsBody>();
            if(!body.IsReady) return;
            if(rotorIndex<0 || rotorIndex>=body.Parameters.Rotors.Count || float.IsNaN(remainingDrive) || float.IsInfinity(remainingDrive) || remainingDrive<0 || remainingDrive>1)
            { Debug.LogError("DroneLab: invalid motor fault settings.",this); return; }
            body.SetRotorDriveAuthority(rotorIndex,remainingDrive); injected=true;
            Debug.Log($"DroneLab: motor {body.Parameters.Rotors[rotorIndex].Id} drive = {remainingDrive:P0}",this);
        }
        [ContextMenu("Restore all motor drives")]
        public void RestoreAll()
        { body?.RestoreRotorDrive(); automaticFault=false; injected=false; }
    }
}
