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
        [Tooltip("For CustomField only: component implementing DroneLab.Physics.IWindProvider.")]
        public MonoBehaviour customWindProvider;
        public IWindProvider CustomWindProvider { get; set; }
        [Tooltip("Optional live atmosphere. Requires IAirProvider and CtCq/PerformanceMap rotors; assign before initialization.")]
        public MonoBehaviour customAirProvider;
        public IAirProvider CustomAirProvider { get; set; }
        public AirSample Air { get; private set; }
        public bool UsesLiveAir { get; private set; }
        public WeatherSample Weather { get; private set; }
        public double SimulationTimeS { get; private set; }
        public Vector3 WindVelocityWorld { get; private set; }
        public Vector3[] RotorWindVelocityWorld { get; private set; }
        public RotorFlightEnvelopeSample[] RotorEnvelopeSamples { get; private set; }
        public double? WindSamplingRatio { get; private set; }
        public bool WindUnderResolved=>WindSamplingRatio>.5;
        public bool drawForces = true;
        public float forceGizmoScale = 0.08f;
        [Tooltip("Colliders considered by rotor ground probes. Triggers and the drone hierarchy are ignored.")]
        public LayerMask groundLayers=UnityEngine.Physics.DefaultRaycastLayers;
        public RuntimeDroneParameters Parameters { get; private set; }
        public Rigidbody Body { get; private set; }
        public bool IsReady => Parameters != null;
        public bool Armed { get; private set; }
        public double[] Omega { get; private set; }
        public PowerSystem Power { get; private set; }
        public RotorDriveState Drive { get; private set; }
        public string ActiveDroneJson { get; private set; }
        public string ActiveEnvironmentJson { get; private set; }
        public double EnvironmentReferenceWorldY=>referenceWorldY;
        public event Action<DronePhysicsBody,float> StepPrepared;
        public double GetMotorCommand(int index)=>commands[index];
        public void SetRotorDriveAuthority(int index,double fraction)
        { if(!IsReady) throw new InvalidOperationException("Initialize physics first."); Drive.Set(index,fraction); }
        public void RestoreRotorDrive()=>Drive?.Reset();
        public RotorTelemetry GetRotorTelemetry(int index)
        {
            var r=Parameters.Rotors[index]; double h=GroundHeightM[index];
            return new RotorTelemetry(commands[index],Drive.Get(index),PhysicsMath.OmegaToRpm(Omega[index]),ThrustN[index],ReactionTorqueNm[index],AdvanceRatio[index],
                MeasuredCurrentA[index],Power==null ? (double?)null : Power.RotorCurrentA[index],double.IsInfinity(h) ? (double?)null : h,GroundEffectMultiplier[index],
                PhysicsMath.InducedHoverVelocity(ThrustN[index],Air.Density,r.Diameter),FromUnity(RotorWindVelocityWorld[index]),FromUnity(RotorDragForceN[index]),PerformanceClamped[index],
                RotorThrustCorrectionN[index],FromUnity(RotorFlappingMomentNm[index]),RotorFlowClamped[index],PropellerTorqueNm[index],
                Parameters.InertialRotors ? Power.RotorAccelerationTorqueNm[index] : (double?)null,
                Parameters.InertialRotors ? Power.RotorSpinEnergyJ[index] : (double?)null,
                Parameters.Battery?.Mode=="Electrical" ? Power.MotorCurrentA[index] : (double?)null,
                Power==null ? (double?)null : Power.RotorMotorLossW[index],Power==null ? (double?)null : Power.RotorEscLossW[index],
                PhysicsMath.OmegaToRpm(Parameters.InertialRotors ? gyroOmega[index] : Omega[index]),
                Power?.Thermal?.Motor(index).TemperatureK,Power?.Thermal?.Esc(index).TemperatureK,
                Power?.Thermal?.Motor(index).Authority,Power?.Thermal?.Esc(index).Authority,
                Power?.Thermal==null ? (double?)null : Power.MotorResistanceOhm(index),RotorEnvelopeSamples[index]);
        }
        public double[] ThrustN { get; private set; }
        public double[] ReactionTorqueNm { get; private set; }
        public double[] PropellerTorqueNm { get; private set; }
        public double[] AdvanceRatio { get; private set; }
        public double?[] MeasuredCurrentA { get; private set; }
        public bool[] PerformanceClamped { get; private set; }
        public double[] GroundHeightM { get; private set; }
        public double[] GroundEffectMultiplier { get; private set; }
        public Vector3[] RotorDragForceN { get; private set; }
        public Vector3 RotorDragForce { get; private set; }
        public Vector3 RotorDragTorque { get; private set; }
        public double[] RotorThrustCorrectionN { get; private set; }
        public Vector3[] RotorFlappingMomentNm { get; private set; }
        public bool[] RotorFlowClamped { get; private set; }
        public Vector3 RotorFlappingMoment { get; private set; }
        public Vector3 RotorGyroscopicMoment { get; private set; }
        public Vector3 AirVelocity { get; private set; }
        public Vector3 DragForce { get; private set; }
        public Vector3 DragTorque { get; private set; }
        public double ProjectedAreaM2 { get; private set; }
        // Explicit stepping for isolated integration tests. Normal scenes use FixedUpdate.
        public bool AutomaticSimulation { get; set; } = true;
        private IWindProvider activeWind;
        private IAirProvider activeAir;
        private IWeatherProvider activeWeather;
        private double referenceWorldY;
        private double[] commands,candidateOmega,axialVelocities,gyroOmega,rotorAirSpeeds;
        private Vector3[] pointAirVelocities,bodyPointForces;
        private Vector3[] rotorPoints,rotorAxes,groundPoints;
        private bool[] groundHits;
        private readonly RotorGroundProbe groundProbe=new RotorGroundProbe();

        // Optional application-level preparation. Runs before the first Initialize, never in flight.
        public static event Action<DronePhysicsBody> PreparingSceneBody;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPreparationHooks() => PreparingSceneBody = null;

        private void Awake()
        {
            Body=GetComponent<Rigidbody>();
            if (droneProfile == null) droneProfile=Resources.Load<TextAsset>("DronePhysics/quad_test_basic");
            if (environmentProfile == null) environmentProfile=Resources.Load<TextAsset>("DronePhysics/environment_calm");
            try { PreparingSceneBody?.Invoke(this); }
            catch (Exception ex) { Fail("Scene preparation rejected: "+ex.Message); return; }
            Initialize(droneProfile,environmentProfile);
        }
        public bool Initialize(TextAsset drone,TextAsset environment)
        {
            if (Body == null) Body=GetComponent<Rigidbody>();
            Parameters=null; Power=null; Drive=null; Armed=false; ActiveDroneJson=ActiveEnvironmentJson=null;
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
            ActiveDroneJson=drone.text; ActiveEnvironmentJson=environment.text;
            activeWind=Parameters.Environment;
            if(Parameters.Environment.WindEnabled && Parameters.Environment.WindMode=="CustomField")
            {
                activeWind=CustomWindProvider ?? customWindProvider as IWindProvider;
                if(activeWind==null) { Parameters=null; Fail("CustomField requires a component implementing IWindProvider."); return false; }
            }
            activeAir=CustomAirProvider ?? customAirProvider as IAirProvider;
            if(customAirProvider!=null && activeAir==null)
            { Parameters=null; Fail("Custom air component must implement IAirProvider."); return false; }
            if(activeAir!=null)
            {
                try { LiveAirValidation.RequireCompatible(Parameters); }
                catch(ArgumentException ex) { Parameters=null; Fail(ex.Message); return false; }
            }
            activeWeather=activeAir as IWeatherProvider ?? activeWind as IWeatherProvider;
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
            Drive=new RotorDriveState(count);
            RotorWindVelocityWorld=new Vector3[count];
            bodyPointForces=new Vector3[Parameters.DragModel=="Surfaces" ? Parameters.Surfaces.Count : 1];
            referenceWorldY=Body.worldCenterOfMass.y; Air=Parameters.Environment.SampleAir(0); SimulationTimeS=0; WindVelocityWorld=Vector3.zero;
            UsesLiveAir=false; Weather=ProfileWeather();
            commands=new double[count]; candidateOmega=new double[count]; pointAirVelocities=new Vector3[count]; Omega=new double[count]; ThrustN=new double[count]; ReactionTorqueNm=new double[count];
            axialVelocities=new double[count]; gyroOmega=new double[count]; rotorAirSpeeds=new double[count];
            RotorEnvelopeSamples=new RotorFlightEnvelopeSample[count];
            PropellerTorqueNm=new double[count];
            AdvanceRatio=new double[count]; MeasuredCurrentA=new double?[count]; PerformanceClamped=new bool[count];
            GroundHeightM=new double[count]; GroundEffectMultiplier=new double[count]; RotorDragForceN=new Vector3[count];
            RotorThrustCorrectionN=new double[count]; RotorFlappingMomentNm=new Vector3[count]; RotorFlowClamped=new bool[count];
            rotorPoints=new Vector3[count]; rotorAxes=new Vector3[count]; groundPoints=new Vector3[count]; groundHits=new bool[count];
            Power=Parameters.Battery==null ? null : new PowerSystem(Parameters);
            ClearRotorEffects();
            if(Mathf.Abs(Time.fixedDeltaTime-0.01f)>1e-6f) Debug.LogWarning("DroneLab recommends Fixed Timestep = 0.01 s. No global setting was changed.",this);
            enabled=true;
            return true;
        }
        private void ClearRotorEffects()
        {
            RotorDragForce=Vector3.zero; RotorDragTorque=Vector3.zero; RotorFlappingMoment=Vector3.zero;
            RotorGyroscopicMoment=Vector3.zero;
            if(RotorThrustCorrectionN!=null) Array.Clear(RotorThrustCorrectionN,0,RotorThrustCorrectionN.Length);
            if(RotorFlappingMomentNm!=null) Array.Clear(RotorFlappingMomentNm,0,RotorFlappingMomentNm.Length);
            if(RotorFlowClamped!=null) Array.Clear(RotorFlowClamped,0,RotorFlowClamped.Length);
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
            SetArmed(false); Power?.Reset(); SimulationTimeS=0; WindVelocityWorld=Vector3.zero;
            Drive?.Reset();
            DragForce=DragTorque=AirVelocity=Vector3.zero; ProjectedAreaM2=0;
            if(bodyPointForces!=null) Array.Clear(bodyPointForces,0,bodyPointForces.Length);
            if(Parameters!=null) { Air=Parameters.Environment.SampleAir(0); UsesLiveAir=false; Weather=ProfileWeather(); }
            if(RotorWindVelocityWorld!=null) Array.Clear(RotorWindVelocityWorld,0,RotorWindVelocityWorld.Length);
            if(Omega == null) return;
            WindSamplingRatio=null; Array.Clear(RotorEnvelopeSamples,0,RotorEnvelopeSamples.Length);
            Array.Clear(Omega,0,Omega.Length); Array.Clear(ThrustN,0,ThrustN.Length); Array.Clear(ReactionTorqueNm,0,ReactionTorqueNm.Length);
            Array.Clear(PropellerTorqueNm,0,PropellerTorqueNm.Length);
            Array.Clear(gyroOmega,0,gyroOmega.Length);
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
                AirSample liveAir=default;
                UsesLiveAir=activeAir!=null && activeAir.TrySampleAir(FromUnity(Body.worldCenterOfMass),SimulationTimeS,out liveAir);
                if(UsesLiveAir) { LiveAirValidation.Validate(liveAir); Air=liveAir; }
                else Air=Parameters.Environment.SampleAir(Body.worldCenterOfMass.y-referenceWorldY);
                Weather=activeWeather!=null && activeWeather.TrySampleWeather(out var weather) ? weather : ProfileWeather();
                Weather=new WeatherSample(Weather.Precipitation,Weather.IntensityMmPerHour);
                WindVelocityWorld=WindAt(Body.worldCenterOfMass);
                WindSamplingRatio=Parameters.Environment.WindSamplingRatio(FromUnity(Body.linearVelocity),dt);
                for(int i=0;i<Parameters.Rotors.Count;i++)
                {
                    var r=Parameters.Rotors[i];
                    double target=Armed && commands[i]>0 ? Math.Max(r.MinOmega,commands[i]*r.MaxOmega) : 0;
                    target=Drive.Target(i,target);
                    if(Power!=null && !Power.IsThermalDriveAvailable(i)) target=0;
                    candidateOmega[i]=PhysicsMath.MotorStep(Omega[i],target,target>Omega[i] ? r.TauUp:r.TauDown,dt);
                    Vector3 pointWorld=transform.TransformPoint(ToUnity(r.Position));
                    Vector3 axisWorld=transform.TransformDirection(ToUnity(r.Axis));
                    rotorPoints[i]=pointWorld; rotorAxes[i]=axisWorld;
                    RotorWindVelocityWorld[i]=WindAt(pointWorld);
                    var pointVelocity=Body.GetPointVelocity(pointWorld);
                    var pointAirVelocity=pointVelocity-RotorWindVelocityWorld[i];
                    pointAirVelocities[i]=pointAirVelocity;
                    axialVelocities[i]=Vector3.Dot(pointAirVelocity,axisWorld);
                    if(WindSamplingRatio.HasValue)
                        WindSamplingRatio=Math.Max(WindSamplingRatio.Value,Parameters.Environment.WindSamplingRatio(FromUnity(pointVelocity),dt).Value);
                }
                if(Power!=null)
                {
                    if(Power.Thermal!=null) for(int i=0;i<rotorAirSpeeds.Length;i++) rotorAirSpeeds[i]=pointAirVelocities[i].magnitude;
                    Power.Resolve(candidateOmega,candidateOmega,dt,Armed,Air.Density,Drive,axialVelocities,Omega,
                        Air.TemperatureK,rotorAirSpeeds,(Body.linearVelocity-WindVelocityWorld).magnitude);
                }
                for(int i=0;i<gyroOmega.Length;i++) gyroOmega[i]=(Omega[i]+candidateOmega[i])/2;
                RotorGyroscopicMoment=transform.TransformDirection(ToUnity(RotorDynamics.GyroscopicMoment(Parameters,gyroOmega,
                    FromUnity(transform.InverseTransformDirection(Body.angularVelocity)))));
                for(int i=0;i<Parameters.Rotors.Count;i++)
                {
                    var r=Parameters.Rotors[i]; var pointWorld=rotorPoints[i]; var axisWorld=rotorAxes[i];
                    var pointAirVelocity=pointAirVelocities[i];
                    double axial=Vector3.Dot(pointAirVelocity,axisWorld);
                    double evaluationOmega=Parameters.InertialRotors ? gyroOmega[i] : candidateOmega[i];
                    var sample=r.Performance.Evaluate(evaluationOmega,axial,Air.Density);
                    RotorEnvelopeSamples[i]=RotorFlightEnvelope.Evaluate(r,evaluationOmega,sample.Thrust,FromUnity(pointAirVelocity),FromUnity(axisWorld),Air.Density);
                    var flow=RotorFlow.Evaluate(r,evaluationOmega,sample.Thrust,FromUnity(pointAirVelocity),FromUnity(axisWorld),Air.Density);
                    RotorThrustCorrectionN[i]=flow.ThrustCorrection;
                    RotorFlappingMomentNm[i]=ToUnity(flow.FlappingMoment); RotorFlowClamped[i]=flow.Clamped;
                    if(Parameters.GroundEffect!=null && groundProbe.Sample(this,pointWorld,axisWorld,(float)(r.Diameter/2),groundLayers.value,out var hit))
                    {
                        groundHits[i]=true; groundPoints[i]=hit.point; GroundHeightM[i]=hit.distance;
                        GroundEffectMultiplier[i]=RotorAerodynamics.GroundMultiplier(Parameters.GroundEffect,r.Diameter/2,hit.distance,Vector3.Dot(axisWorld,hit.normal));
                    }
                    // Thrust-only model; do not invent a Q or current correction, or augment windmilling thrust.
                    ThrustN[i]=RotorAerodynamics.ThrustWithGroundEffect(sample.Thrust+flow.ThrustCorrection,GroundEffectMultiplier[i]);
                    ReactionTorqueNm[i]=r.ReactionSign*(Parameters.InertialRotors ? Power.RotorTorqueNm[i] : sample.Torque);
                    PropellerTorqueNm[i]=Parameters.InertialRotors ? Power.RotorPropellerTorqueNm[i] : sample.Torque;
                    if(Parameters.RotorDrag)
                        RotorDragForceN[i]=ToUnity(RotorAerodynamics.Drag(FromUnity(pointAirVelocity),FromUnity(axisWorld),evaluationOmega,r.RotorDragCoefficient));
                    AdvanceRatio[i]=sample.AdvanceRatio; MeasuredCurrentA[i]=sample.Current; PerformanceClamped[i]=sample.Clamped;
                }
                // Sample all body points before applying any forces or spending charge.
                DVector3 localForce=default,localTorque=default; double area=0;
                AirVelocity=Body.linearVelocity-WindVelocityWorld;
                int points=Parameters.DragModel=="Surfaces" ? Parameters.Surfaces.Count : 1;
                if(Parameters.BodyDrag) for(int i=0;i<points;i++)
                {
                    var localPoint=Parameters.DragModel=="Surfaces" ? Parameters.Surfaces[i].Position : Parameters.DragPoint;
                    var worldPoint=transform.TransformPoint(ToUnity(localPoint));
                    var pointVelocity=Body.GetPointVelocity(worldPoint);
                    var flow=pointVelocity-WindAt(worldPoint);
                    if(WindSamplingRatio.HasValue)
                        WindSamplingRatio=Math.Max(WindSamplingRatio.Value,Parameters.Environment.WindSamplingRatio(FromUnity(pointVelocity),dt).Value);
                    var wrench=BodyAerodynamics.EvaluatePoint(Parameters,FromUnity(transform.InverseTransformDirection(flow)),Air.Density,i);
                    bodyPointForces[i]=transform.TransformDirection(ToUnity(wrench.Force));
                    localForce+=wrench.Force; localTorque+=wrench.Torque; area=wrench.ProjectedArea;
                    if(Parameters.DragModel!="Surfaces") AirVelocity=flow;
                }
                DragForce=transform.TransformDirection(ToUnity(localForce)); DragTorque=transform.TransformDirection(ToUnity(localTorque)); ProjectedAreaM2=area;
            }
            catch(Exception ex) when(ex is ArgumentException || ex is InvalidOperationException)
            {
                Parameters=null; ResetMotorState(); Fail("Physics/power query rejected simulation step: "+ex.Message); return;
            }
            Array.Copy(candidateOmega,Omega,Omega.Length); Power?.Commit(dt); SimulationTimeS+=dt;
            Body.AddForce(Vector3.down*(float)(Parameters.Mass*Parameters.Gravity),ForceMode.Force);
            for(int i=0;i<Parameters.Rotors.Count;i++)
            {
                var r=Parameters.Rotors[i];
                Vector3 axis=rotorAxes[i];
                Body.AddForceAtPosition(axis*(float)ThrustN[i]+RotorDragForceN[i],rotorPoints[i],ForceMode.Force);
                RotorDragForce+=RotorDragForceN[i];
                RotorDragTorque+=Vector3.Cross(rotorPoints[i]-Body.worldCenterOfMass,RotorDragForceN[i]);
                Body.AddTorque(axis*(float)ReactionTorqueNm[i],ForceMode.Force);
                // Hub flapping moment is separate from AddForceAtPosition's r x F.
                Body.AddTorque(RotorFlappingMomentNm[i],ForceMode.Force); RotorFlappingMoment+=RotorFlappingMomentNm[i];
            }
            // Equivalent CP wrench about COM, with no second r x F.
            Body.AddForce(DragForce,ForceMode.Force); Body.AddTorque(DragTorque,ForceMode.Force);
            Body.AddTorque(RotorGyroscopicMoment,ForceMode.Force);
            StepPrepared?.Invoke(this,dt);
        }
        private Vector3 WindAt(Vector3 point)
        {
            var wind=activeWind.Sample(FromUnity(point),SimulationTimeS); EnvironmentMath.Finite(wind);
            if(Math.Abs(wind.X)>1e6 || Math.Abs(wind.Y)>1e6 || Math.Abs(wind.Z)>1e6)
                throw new ArgumentOutOfRangeException(nameof(wind),"Wind provider exceeds runtime safety bound 1e6 m/s.");
            return ToUnity(wind);
        }
        private WeatherSample ProfileWeather()=>new WeatherSample(Parameters.Environment.Precipitation,Parameters.Environment.PrecipitationIntensityMmPerHour);
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
            Gizmos.color=Color.blue; Gizmos.DrawLine(Body.worldCenterOfMass,Body.worldCenterOfMass+WindVelocityWorld*0.15f);
            Gizmos.color=Color.red; var cp=transform.TransformPoint(ToUnity(Parameters.DragPoint));
            if(Parameters.DragModel!="Surfaces") { Gizmos.DrawLine(cp,cp+DragForce*forceGizmoScale); Gizmos.DrawWireSphere(cp,0.02f); }
            for(int i=0;i<Parameters.Surfaces.Count;i++)
            {
                var surface=Parameters.Surfaces[i];
                var pos=transform.TransformPoint(ToUnity(surface.Position));
                Gizmos.color=Color.cyan; Gizmos.DrawLine(pos,pos+transform.TransformDirection(ToUnity(surface.Normal))*0.1f);
                Gizmos.DrawWireSphere(pos,0.015f);
                Gizmos.color=Color.red; Gizmos.DrawLine(pos,pos+bodyPointForces[i]*forceGizmoScale);
            }
        }
        public static Vector3 ToUnity(DVector3 v) => new Vector3((float)v.X,(float)v.Y,(float)v.Z);
        public static DVector3 FromUnity(Vector3 v) => new DVector3(v.x,v.y,v.z);
    }
}
