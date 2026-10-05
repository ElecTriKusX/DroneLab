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
        [Tooltip("Colliders considered by rotor ground probes. Triggers and the drone hierarchy are ignored.")]
        public LayerMask groundLayers=UnityEngine.Physics.DefaultRaycastLayers;
        public RuntimeDroneParameters Parameters { get; private set; }
        public Rigidbody Body { get; private set; }
        public bool IsReady => Parameters != null;
        public bool Armed { get; private set; }
        public double[] Omega { get; private set; }
        public double[] ThrustN { get; private set; }
        public double[] ReactionTorqueNm { get; private set; }
        public double[] AdvanceRatio { get; private set; }
        public double?[] MeasuredCurrentA { get; private set; }
        public bool[] PerformanceClamped { get; private set; }
        public double[] GroundHeightM { get; private set; }
        public double[] GroundEffectMultiplier { get; private set; }
        public Vector3[] RotorDragForceN { get; private set; }
        public Vector3 RotorDragForce { get; private set; }
        public Vector3 RotorDragTorque { get; private set; }
        public Vector3 AirVelocity { get; private set; }
        public Vector3 DragForce { get; private set; }
        public Vector3 DragTorque { get; private set; }
        public double ProjectedAreaM2 { get; private set; }
        // Explicit stepping for isolated integration tests. Normal scenes use FixedUpdate.
        public bool AutomaticSimulation { get; set; } = true;
        private double[] commands;
        private Vector3[] rotorPoints,rotorAxes,groundPoints;
        private bool[] groundHits;
        private readonly RotorGroundProbe groundProbe=new RotorGroundProbe();

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
            AdvanceRatio=new double[count]; MeasuredCurrentA=new double?[count]; PerformanceClamped=new bool[count];
            GroundHeightM=new double[count]; GroundEffectMultiplier=new double[count]; RotorDragForceN=new Vector3[count];
            rotorPoints=new Vector3[count]; rotorAxes=new Vector3[count]; groundPoints=new Vector3[count]; groundHits=new bool[count];
            ClearRotorEffects();
            if(Mathf.Abs(Time.fixedDeltaTime-0.01f)>1e-6f) Debug.LogWarning("DroneLab recommends Fixed Timestep = 0.01 s. No global setting was changed.",this);
            enabled=true;
            return true;
        }
        private void ClearRotorEffects()
        {
            RotorDragForce=Vector3.zero; RotorDragTorque=Vector3.zero;
            for(int i=0;i<GroundHeightM.Length;i++)
            { GroundHeightM[i]=double.PositiveInfinity; GroundEffectMultiplier[i]=1; RotorDragForceN[i]=Vector3.zero; groundHits[i]=false; }
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
            Array.Clear(AdvanceRatio,0,AdvanceRatio.Length); Array.Clear(MeasuredCurrentA,0,MeasuredCurrentA.Length); Array.Clear(PerformanceClamped,0,PerformanceClamped.Length); ClearRotorEffects();
        }
        private void FixedUpdate()
        { if(AutomaticSimulation) StepPhysics(Time.fixedDeltaTime); }
        public void StepPhysics(float dt)
        {
            if(dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if(!IsReady || !enabled) return;
            ClearRotorEffects();
            try
            {
                for(int i=0;i<Parameters.Rotors.Count;i++)
                {
                    var r=Parameters.Rotors[i];
                    double target=Armed && commands[i]>0 ? Math.Max(r.MinOmega,commands[i]*r.MaxOmega) : 0;
                    Omega[i]=PhysicsMath.MotorStep(Omega[i],target,target>Omega[i] ? r.TauUp:r.TauDown,dt);
                    Vector3 pointWorld=transform.TransformPoint(ToUnity(r.Position));
                    Vector3 axisWorld=transform.TransformDirection(ToUnity(r.Axis));
                    rotorPoints[i]=pointWorld; rotorAxes[i]=axisWorld;
                    var pointAirVelocity=Body.GetPointVelocity(pointWorld)-ToUnity(Parameters.Wind);
                    double axial=Vector3.Dot(pointAirVelocity,axisWorld);
                    var sample=r.Performance.Evaluate(Omega[i],axial);
                    if(Parameters.GroundEffect!=null && groundProbe.Sample(this,pointWorld,axisWorld,(float)(r.Diameter/2),groundLayers.value,out var hit))
                    {
                        groundHits[i]=true; groundPoints[i]=hit.point; GroundHeightM[i]=hit.distance;
                        GroundEffectMultiplier[i]=RotorAerodynamics.GroundMultiplier(Parameters.GroundEffect,r.Diameter/2,hit.distance,Vector3.Dot(axisWorld,hit.normal));
                    }
                    // Thrust-only model; do not invent a Q or current correction, or augment windmilling thrust.
                    ThrustN[i]=RotorAerodynamics.ThrustWithGroundEffect(sample.Thrust,GroundEffectMultiplier[i]);
                    ReactionTorqueNm[i]=r.ReactionSign*sample.Torque;
                    if(Parameters.RotorDrag)
                        RotorDragForceN[i]=ToUnity(RotorAerodynamics.Drag(FromUnity(pointAirVelocity),FromUnity(axisWorld),Omega[i],r.RotorDragCoefficient));
                    AdvanceRatio[i]=sample.AdvanceRatio; MeasuredCurrentA[i]=sample.Current; PerformanceClamped[i]=sample.Clamped;
                }
            }
            catch(ArgumentOutOfRangeException ex)
            {
                Parameters=null; ResetMotorState(); Fail("Propeller range rejected simulation step: "+ex.Message); return;
            }
            Body.AddForce(Vector3.down*(float)(Parameters.Mass*Parameters.Gravity),ForceMode.Force);
            for(int i=0;i<Parameters.Rotors.Count;i++)
            {
                var r=Parameters.Rotors[i];
                Vector3 axis=rotorAxes[i];
                Body.AddForceAtPosition(axis*(float)ThrustN[i]+RotorDragForceN[i],rotorPoints[i],ForceMode.Force);
                RotorDragForce+=RotorDragForceN[i];
                RotorDragTorque+=Vector3.Cross(rotorPoints[i]-Body.worldCenterOfMass,RotorDragForceN[i]);
                Body.AddTorque(axis*(float)ReactionTorqueNm[i],ForceMode.Force);
            }
            Vector3 point=transform.TransformPoint(ToUnity(Parameters.DragPoint));
            AirVelocity=Body.GetPointVelocity(point)-ToUnity(Parameters.Wind);
            var local=FromUnity(transform.InverseTransformDirection(Body.linearVelocity-ToUnity(Parameters.Wind)));
            var angular=FromUnity(transform.InverseTransformDirection(Body.angularVelocity));
            var wrench=BodyAerodynamics.Evaluate(Parameters,local,angular);
            DragForce=transform.TransformDirection(ToUnity(wrench.Force));
            DragTorque=transform.TransformDirection(ToUnity(wrench.Torque));
            ProjectedAreaM2=wrench.ProjectedArea;
            // Equivalent to summing forces at every CP, with moments about COM; no second r x F.
            Body.AddForce(DragForce,ForceMode.Force); Body.AddTorque(DragTorque,ForceMode.Force);
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
                Gizmos.color=Color.cyan; Gizmos.DrawLine(pos,pos+transform.TransformDirection(ToUnity(r.Axis))*0.15f);
                Gizmos.color=new Color(1,0.5f,0); Gizmos.DrawLine(pos,pos+RotorDragForceN[i]*forceGizmoScale);
                if(groundHits[i]) { Gizmos.color=Color.yellow; Gizmos.DrawLine(pos,groundPoints[i]); Gizmos.DrawWireSphere(groundPoints[i],0.01f); }
            }
            Gizmos.color=Color.blue; Gizmos.DrawLine(Body.worldCenterOfMass,Body.worldCenterOfMass+ToUnity(Parameters.Wind)*0.15f);
            Gizmos.color=Color.red; var cp=transform.TransformPoint(ToUnity(Parameters.DragPoint));
            if(Parameters.DragModel!="Surfaces") { Gizmos.DrawLine(cp,cp+DragForce*forceGizmoScale); Gizmos.DrawWireSphere(cp,0.02f); }
            foreach(var surface in Parameters.Surfaces)
            {
                var pos=transform.TransformPoint(ToUnity(surface.Position));
                Gizmos.color=Color.cyan; Gizmos.DrawLine(pos,pos+transform.TransformDirection(ToUnity(surface.Normal))*0.1f);
                Gizmos.DrawWireSphere(pos,0.015f);
                var velocity=FromUnity(transform.InverseTransformDirection(Body.GetPointVelocity(pos)-ToUnity(Parameters.Wind)));
                var force=BodyAerodynamics.SurfaceDrag(velocity,surface.Normal,Parameters.Density,surface.Cd,surface.Area);
                Gizmos.color=Color.red; Gizmos.DrawLine(pos,pos+transform.TransformDirection(ToUnity(force))*forceGizmoScale);
            }
        }
        public static Vector3 ToUnity(DVector3 v) => new Vector3((float)v.X,(float)v.Y,(float)v.Z);
        public static DVector3 FromUnity(Vector3 v) => new DVector3(v.x,v.y,v.z);
    }
}
