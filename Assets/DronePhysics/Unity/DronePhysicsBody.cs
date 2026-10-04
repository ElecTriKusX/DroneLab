using System;
using DroneLab.Physics;
using UnityEngine;

namespace DroneLab.Simulation
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(0)]
    public sealed class DronePhysicsBody : MonoBehaviour
    {
        public TextAsset droneProfile;
        public TextAsset environmentProfile;
        public bool drawForces = true;
        public float forceGizmoScale = 0.08f;
        public RuntimeDroneParameters Parameters { get; private set; }
        public Rigidbody Body { get; private set; }
        public bool IsReady => Parameters != null;
        public bool Armed { get; private set; }
        public double[] Omega { get; private set; }
        public double[] ThrustN { get; private set; }
        public double[] ReactionTorqueNm { get; private set; }
        public Vector3 AirVelocity { get; private set; }
        public Vector3 DragForce { get; private set; }
        private double[] commands;

        private void Awake()
        {
            Body=GetComponent<Rigidbody>();
            if (droneProfile == null) droneProfile=Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
            if (environmentProfile == null) environmentProfile=Resources.Load<TextAsset>("DronePhysics/environment_calm");
            Initialize(droneProfile,environmentProfile);
        }
        public bool Initialize(TextAsset drone,TextAsset environment)
        {
            if (Body == null) Body=GetComponent<Rigidbody>();
            Parameters=null; Armed=false;
            var ds=Resources.Load<TextAsset>("DronePhysics/drone-profile.schema");
            var es=Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
            if(drone == null || environment == null || ds == null || es == null)
            { Fail("Missing profile or schema TextAsset."); return false; }
            if((transform.lossyScale-Vector3.one).sqrMagnitude>1e-8f)
            { Fail("Physics root and its ancestors must have unit scale. Scale the visual child instead."); return false; }
            if(Body.isKinematic || Body.constraints != RigidbodyConstraints.None)
            { Fail("Use a dynamic Rigidbody with no frozen axes."); return false; }
            if(GetComponentsInChildren<Rigidbody>().Length != 1)
            { Fail("Only one Rigidbody is allowed in the drone hierarchy."); return false; }
            if(GetComponentsInChildren<Collider>().Length == 0)
            { Fail("Add a BoxCollider or compound primitive colliders to the drone."); return false; }
            var loaded=ProfileLoader.Load(drone.text,environment.text,ds.text,es.text);
            foreach(var issue in loaded.Issues)
                if(issue.Severity == "Error") Debug.LogError(issue.ToString(),this); else Debug.LogWarning(issue.ToString(),this);
            if(!loaded.Success) { Fail("Physics profile rejected. See validation errors."); return false; }
            Parameters=loaded.Parameters;
            Body.mass=(float)Parameters.Mass;
            Body.centerOfMass=ToUnity(Parameters.CenterOfMass);
            Body.inertiaTensorRotation=new Quaternion((float)Parameters.RotationX,(float)Parameters.RotationY,(float)Parameters.RotationZ,(float)Parameters.RotationW);
            Body.inertiaTensor=ToUnity(Parameters.Inertia);
            Body.useGravity=false; // Environment gravity is applied once below; do not modify Physics.gravity globally.
            Body.linearDamping=0; Body.angularDamping=0;
            Body.maxAngularVelocity=50;
            Body.interpolation=RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            int count=Parameters.Rotors.Count;
            commands=new double[count]; Omega=new double[count]; ThrustN=new double[count]; ReactionTorqueNm=new double[count];
            if(Mathf.Abs(Time.fixedDeltaTime-0.01f)>1e-6f) Debug.LogWarning("DroneLab recommends Fixed Timestep = 0.01 s. No global setting was changed.",this);
            enabled=true;
            return true;
        }
        private void Fail(string message) { Debug.LogError("DroneLab: "+message,this); enabled=false; }
        public void SetArmed(bool armed)
        {
            Armed=armed && IsReady;
            if(!Armed && commands != null) Array.Clear(commands,0,commands.Length);
        }
        // Commands are normalized RPM targets, not power or linear thrust.
        // Zero is motor stop; optional idle must be requested explicitly by a flight controller.
        public void SetMotorCommand(int index,double command)
        {
            if(!IsReady) return;
            if(double.IsNaN(command) || double.IsInfinity(command)) command=0;
            commands[index]=PhysicsMath.Clamp(command,0,1);
        }
        public void ResetMotorState()
        {
            SetArmed(false);
            if(Omega == null) return;
            Array.Clear(Omega,0,Omega.Length); Array.Clear(ThrustN,0,ThrustN.Length); Array.Clear(ReactionTorqueNm,0,ReactionTorqueNm.Length);
        }
        private void FixedUpdate()
        {
            if(!IsReady) return;
            Body.AddForce(Vector3.down*(float)(Parameters.Mass*Parameters.Gravity),ForceMode.Force);
            for(int i=0;i<Parameters.Rotors.Count;i++)
            {
                var r=Parameters.Rotors[i];
                double target=Armed && commands[i]>0 ? Math.Max(r.MinOmega,commands[i]*r.MaxOmega) : 0;
                Omega[i]=PhysicsMath.MotorStep(Omega[i],target,target>Omega[i] ? r.TauUp:r.TauDown,Time.fixedDeltaTime);
                ThrustN[i]=PhysicsMath.Thrust(Omega[i],r.KT);
                ReactionTorqueNm[i]=r.ReactionSign*PhysicsMath.Torque(Omega[i],r.KQ);
                Vector3 axis=transform.TransformDirection(ToUnity(r.Axis));
                Body.AddForceAtPosition(axis*(float)ThrustN[i],transform.TransformPoint(ToUnity(r.Position)),ForceMode.Force);
                Body.AddTorque(axis*(float)ReactionTorqueNm[i],ForceMode.Force);
            }
            Vector3 point=transform.TransformPoint(ToUnity(Parameters.DragPoint));
            AirVelocity=Body.GetPointVelocity(point)-ToUnity(Parameters.Wind);
            var local=FromUnity(transform.InverseTransformDirection(AirVelocity));
            DragForce=Parameters.BodyDrag ? transform.TransformDirection(ToUnity(PhysicsMath.AxisDrag(local,Parameters.Density,Parameters.DragCd,Parameters.DragArea))) : Vector3.zero;
            Body.AddForceAtPosition(DragForce,point,ForceMode.Force);
        }
        private void OnDisable() { SetArmed(false); }
        private void OnDrawGizmos()
        {
            if(!drawForces || !IsReady) return;
            Gizmos.color=Color.yellow; Gizmos.DrawSphere(Body.worldCenterOfMass,0.015f);
            var previousMatrix=Gizmos.matrix;
            Gizmos.matrix=transform.localToWorldMatrix;
            Gizmos.color=Color.magenta;
            Gizmos.DrawWireCube(ToUnity(Parameters.CenterOfMass),ToUnity(Parameters.Dimensions));
            Gizmos.matrix=previousMatrix;
            for(int i=0;i<Parameters.Rotors.Count;i++)
            {
                var r=Parameters.Rotors[i]; var pos=transform.TransformPoint(ToUnity(r.Position));
                Gizmos.color=Color.green; Gizmos.DrawLine(pos,pos+transform.TransformDirection(ToUnity(r.Axis))*(float)ThrustN[i]*forceGizmoScale);
                Gizmos.DrawWireSphere(pos,0.012f);
            }
            Gizmos.color=Color.blue; Gizmos.DrawLine(Body.worldCenterOfMass,Body.worldCenterOfMass+ToUnity(Parameters.Wind)*0.15f);
            Gizmos.color=Color.red; var cp=transform.TransformPoint(ToUnity(Parameters.DragPoint)); Gizmos.DrawLine(cp,cp+DragForce*forceGizmoScale);
        }
        public static Vector3 ToUnity(DVector3 v) => new Vector3((float)v.X,(float)v.Y,(float)v.Z);
        public static DVector3 FromUnity(Vector3 v) => new DVector3(v.x,v.y,v.z);
    }
}
