using System;
using DroneLab.Physics;
using UnityEngine;

namespace DroneLab.Simulation
{
    // Scene-compatible flight controller adapter; physics forces remain in DronePhysicsBody.
    [DefaultExecutionOrder(-100), RequireComponent(typeof(DronePhysicsBody))]
    public sealed class DroneTestPilot : MonoBehaviour
    {
        public bool autoLevel = true;
        public bool altitudeHold;
        [Range(0.1f,0.95f)] public float manualCollectiveFraction = 0.38f;
        [Range(1,45)] public float maxTiltDegrees = 20;
        public float maxRateDegrees = 100;
        public float yawRateDegrees = 70;
        public float attitudeGain = 5;
        public float rateGain = 12;
        public float altitudeGain = 3;
        public float verticalVelocityGain = 3;
        public float climbSpeedMps = 1.5f;
        [Header("PID: legacy Gain fields above are proportional gains")]
        public PidTerms ratePid = new PidTerms(2,0.15,3);
        public PidTerms attitudePid = new PidTerms(0.1,0.1,0.3);
        public PidTerms altitudePid = new PidTerms(0.1,0,0.5);
        public PidTerms verticalVelocityPid = new PidTerms(0.8,0.1,2);
        [Min(0)] public float commandSmoothingSeconds = 0.12f;
        [Min(0)] public float modeTransitionSeconds = 0.3f;
        public PilotDevice inputDevice = PilotDevice.Keyboard;
        public bool showTelemetry = true;
        [Tooltip("Legacy name: enables the selected keyboard/gamepad input. Disable for scripted input.")]
        public bool readKeyboard = true;
        public bool Saturated { get; private set; }
        public Vector3 DesiredAngularRateLocal { get; private set; }
        public Vector3 RequestedTorqueLocal { get; private set; }
        private DronePhysicsBody physicsBody;
        private QuadAllocator allocator;
        private readonly double[] commands=new double[4];
        private FlightInput input;
        private readonly FlightController controller=new FlightController();
        private Vector3 smoothedRate;
        private double lastCollective, transitionCollective;
        private float transitionRemaining;
        private bool previousAutoLevel, previousArmed;
        private PilotDevice previousDevice;
        private int previousDeviceId;
        public bool AutomaticControl { get; set; } = true;
        public bool DisarmOnFocusLoss { get; set; } = true;
        private float targetAltitude;
        private bool previousAltitudeHold;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Start()
        { InitializeController(); }
        public void InitializeController()
        {
            if(allocator!=null) return;
            physicsBody=GetComponent<DronePhysicsBody>();
            if(!physicsBody.IsReady) { enabled=false; return; }
            try { allocator=new QuadAllocator(physicsBody.Parameters); }
            catch(ArgumentException ex) { Debug.LogError(ex.Message,this); enabled=false; return; }
            startPosition=transform.position; startRotation=transform.rotation;
            targetAltitude=transform.position.y; previousAltitudeHold=altitudeHold;
            previousAutoLevel=autoLevel; previousDevice=inputDevice;
        }
        private void Update()
        {
            if(!AutomaticControl || !readKeyboard) return;
            if(!Application.isFocused || physicsBody == null) { input=default; return; }
            if(previousDevice!=inputDevice)
            {
                physicsBody.SetArmed(false); ResetControl(); previousDevice=inputDevice; previousDeviceId=0;
            }
            var frame=DronePilotInput.Read(inputDevice,manualCollectiveFraction);
            input=frame.Command;
            if(!frame.Available) { physicsBody.SetArmed(false); return; }
            if(previousDeviceId!=0 && previousDeviceId!=frame.DeviceId) { physicsBody.SetArmed(false); ResetControl(); }
            previousDeviceId=frame.DeviceId;
            if(frame.Reset) { ResetPose(); return; }
            if(frame.Arm) { physicsBody.SetArmed(!physicsBody.Armed); ResetControl(); previousArmed=false; }
            if(frame.Mode) autoLevel=!autoLevel;
            if(frame.Altitude) altitudeHold=!altitudeHold;
        }
        // Keeps the stage-1 test API and throttle semantics intact.
        public void SetTestInput(float rollInput,float pitchInput,float yawInput,float climbInput)
            => input=new FlightInput(rollInput,pitchInput,yawInput,climbInput,climbInput>0 ? manualCollectiveFraction : 0);
        public void SetFlightInput(FlightInput command) => input=command;
        private void ResetControl()
        {
            controller.Reset(); smoothedRate=Vector3.zero;
            lastCollective=transitionCollective=0; transitionRemaining=0;
            Saturated=false; DesiredAngularRateLocal=RequestedTorqueLocal=Vector3.zero;
        }
        private void ResetPose()
        {
            physicsBody.ResetMotorState(); ResetControl(); input=default;
            physicsBody.Body.position=startPosition; physicsBody.Body.rotation=startRotation;
            physicsBody.Body.linearVelocity=Vector3.zero; physicsBody.Body.angularVelocity=Vector3.zero;
            targetAltitude=startPosition.y;
        }
        private void FixedUpdate()
        { if(AutomaticControl) StepControl(Time.fixedDeltaTime); }
        public void StepControl(float dt)
        {
            if(dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if(allocator == null || !physicsBody.IsReady) return;
            var body=physicsBody.Body; var p=physicsBody.Parameters;
            if(!physicsBody.Armed)
            {
                targetAltitude=body.position.y; ResetControl(); previousArmed=false;
                previousAltitudeHold=altitudeHold; previousAutoLevel=autoLevel; return;
            }
            if(!previousArmed) { ResetControl(); targetAltitude=body.position.y; previousArmed=true; }
            bool integrate=!Saturated;
            if(altitudeHold!=previousAltitudeHold || autoLevel!=previousAutoLevel)
            {
                controller.Reset(); integrate=false;
                if(altitudeHold!=previousAltitudeHold) targetAltitude=body.position.y;
                transitionCollective=lastCollective; transitionRemaining=modeTransitionSeconds;
                previousAltitudeHold=altitudeHold; previousAutoLevel=autoLevel;
            }
            float upright=Vector3.Dot(transform.up,Vector3.up);
            double collective;
            if(altitudeHold)
            {
                targetAltitude+=(float)input.Climb*climbSpeedMps*dt;
                targetAltitude=Mathf.Clamp(targetAltitude,body.position.y-2,body.position.y+2);
                double accel=controller.ClimbAcceleration(targetAltitude-body.position.y,body.linearVelocity.y,dt,
                    altitudeGain,altitudePid,verticalVelocityGain,verticalVelocityPid,
                    Math.Max(2,climbSpeedMps),4,integrate && upright>0.35f);
                collective=upright>0.35f ? p.Mass*(p.Gravity+accel)/upright : 0;
            }
            else collective=p.MaxTotalThrust*input.Throttle;
            // A manual zero throttle and unsafe upside-down altitude hold override transition smoothing.
            if(collective<=0)
            {
                for(int i=0;i<4;i++) physicsBody.SetMotorCommand(i,0);
                ResetControl(); targetAltitude=body.position.y; return;
            }
            if(transitionRemaining>0 && modeTransitionSeconds>0)
            {
                transitionRemaining=Mathf.Max(0,transitionRemaining-dt);
                float blend=1-transitionRemaining/modeTransitionSeconds;
                blend=Mathf.Clamp01(blend); blend=blend*blend*(3-2*blend);
                collective=transitionCollective+(collective-transitionCollective)*blend;
            }
            lastCollective=collective;
            Vector3 rate=transform.InverseTransformDirection(body.angularVelocity);
            Vector3 desiredRate;
            if(autoLevel)
            {
                var heading=Quaternion.Euler(0,transform.eulerAngles.y,0);
                var planar=Vector2.ClampMagnitude(new Vector2((float)input.Roll,(float)input.Pitch),1);
                float tilt=Mathf.Tan(maxTiltDegrees*Mathf.Deg2Rad);
                Vector3 desiredUp=(Vector3.up+(heading*new Vector3(planar.x,0,planar.y))*tilt).normalized;
                var error=transform.InverseTransformDirection(Vector3.Cross(transform.up,desiredUp));
                desiredRate=DronePhysicsBody.ToUnity(controller.AttitudeRate(DronePhysicsBody.FromUnity(error),
                    DronePhysicsBody.FromUnity(rate),dt,attitudeGain,attitudePid,maxRateDegrees*Mathf.Deg2Rad,integrate));
                desiredRate=Vector3.ClampMagnitude(desiredRate,maxRateDegrees*Mathf.Deg2Rad);
                desiredRate.y=(float)input.Yaw*yawRateDegrees*Mathf.Deg2Rad;
            }
            else desiredRate=new Vector3((float)input.Pitch*maxRateDegrees,(float)input.Yaw*yawRateDegrees,-(float)input.Roll*maxRateDegrees)*Mathf.Deg2Rad;
            smoothedRate=new Vector3((float)PilotMath.SmoothCommand(smoothedRate.x,desiredRate.x,commandSmoothingSeconds,dt),
                (float)PilotMath.SmoothCommand(smoothedRate.y,desiredRate.y,commandSmoothingSeconds,dt),
                (float)PilotMath.SmoothCommand(smoothedRate.z,desiredRate.z,commandSmoothingSeconds,dt));
            desiredRate=smoothedRate;
            DesiredAngularRateLocal=desiredRate;
            Vector3 acceleration=DronePhysicsBody.ToUnity(controller.RateAcceleration(
                DronePhysicsBody.FromUnity(desiredRate),DronePhysicsBody.FromUnity(rate),dt,rateGain,ratePid,40,integrate));
            // Multiply by full principal-axis inertia; do not assume tensor axes match body axes.
            Quaternion axes=body.inertiaTensorRotation;
            Vector3 MultiplyInertia(Vector3 v) => axes*Vector3.Scale(body.inertiaTensor,Quaternion.Inverse(axes)*v);
            Vector3 torque=MultiplyInertia(acceleration)+Vector3.Cross(rate,MultiplyInertia(rate));
            RequestedTorqueLocal=torque;
            Saturated=allocator.Allocate(collective,DronePhysicsBody.FromUnity(torque),commands);
            for(int i=0;i<4;i++) physicsBody.SetMotorCommand(i,commands[i]);
        }
        private void OnApplicationFocus(bool focus) { if(DisarmOnFocusLoss && !focus && physicsBody != null) { physicsBody.SetArmed(false); input=default; ResetControl(); } }
        private void OnDisable() { if(physicsBody != null) { physicsBody.SetArmed(false); ResetControl(); } }
        private void OnGUI()
        {
            if(!showTelemetry || physicsBody == null || !physicsBody.IsReady) return;
            var p=physicsBody.Parameters;
            Vector3 velocity=physicsBody.Body.linearVelocity;
            Vector3 rate=transform.InverseTransformDirection(physicsBody.Body.angularVelocity)*Mathf.Rad2Deg;
            double minRpm=double.MaxValue,maxRpm=0;
            foreach(double omega in physicsBody.Omega)
            { double rpm=PhysicsMath.OmegaToRpm(omega); minRpm=Math.Min(minRpm,rpm); maxRpm=Math.Max(maxRpm,rpm); }
            float tilt=Mathf.Acos(Mathf.Clamp(Vector3.Dot(transform.up,Vector3.up),-1,1))*Mathf.Rad2Deg;
            GUILayout.BeginArea(new Rect(12,12,720,620),GUI.skin.box);
            GUILayout.Label($"DroneLab | {(physicsBody.Armed?"ARMED":"DISARMED")} | {(autoLevel?"ANGLE":"ACRO")} | Alt hold: {altitudeHold}");
            GUILayout.Label($"Input {inputDevice} | throttle {input.Throttle:P0} | torque authority {allocator?.TorqueScale ?? 0:P0}");
            GUILayout.Label("F arm | WASD tilt | Q/E yaw | Space/Ctrl lift\nZ Angle/Acro | H altitude hold | Backspace reset");
            if(inputDevice==PilotDevice.Gamepad) GUILayout.Label("Start arm | Right stick tilt | Left X yaw / Y climb | RT throttle\nX/Square mode | A/Cross altitude | Y/Triangle reset");
            GUILayout.Label($"Mass {p.Mass:F2} kg | T/W {p.ThrustToWeight:F2} | Saturation {Saturated}");
            GUILayout.Label($"Speed {velocity.magnitude:F2} m/s = {velocity.magnitude*3.6f:F1} km/h | Horizontal {new Vector2(velocity.x,velocity.z).magnitude:F2} m/s");
            GUILayout.Label($"Vertical {velocity.y:F2} m/s | World Y {transform.position.y:F2} m | From reset {Vector3.Distance(startPosition,transform.position):F2} m");
            GUILayout.Label($"Tilt {tilt:F1} deg | Yaw {rate.y:F1} / target {DesiredAngularRateLocal.y*Mathf.Rad2Deg:F1} deg/s");
            GUILayout.Label($"RPM spread {maxRpm-minRpm:F2} | Torque local [{RequestedTorqueLocal.x:F4}, {RequestedTorqueLocal.y:F4}, {RequestedTorqueLocal.z:F4}] Nm");
            GUILayout.Label($"Profile dimensions {p.Dimensions.X:F2} x {p.Dimensions.Y:F2} x {p.Dimensions.Z:F2} m | Air velocity {physicsBody.AirVelocity}");
            GUILayout.Label($"Body {p.DragModel} | silhouette {physicsBody.ProjectedAreaM2:F4} m² | drag {physicsBody.DragForce.magnitude:F3} N | aero torque {physicsBody.DragTorque.magnitude:F4} Nm");
            if(p.RotorDrag || p.GroundEffect!=null)
                GUILayout.Label($"Rotor drag {physicsBody.RotorDragForce.magnitude:F3} N | rotor torque {physicsBody.RotorDragTorque.magnitude:F4} Nm | ground effect {(p.GroundEffect!=null ? "ON" : "OFF")}");
            if(p.Rotors[0].Performance.Model=="PerformanceMap") GUILayout.Label("Map: physics uses rotor-point axial flow; test pilot allocation uses static J=0 reference.");
            for(int i=0;i<p.Rotors.Count;i++)
            {
                GUILayout.Label($"{p.Rotors[i].Id}: {PhysicsMath.OmegaToRpm(physicsBody.Omega[i]):F1} RPM | {physicsBody.ThrustN[i]:F2} N | {physicsBody.ReactionTorqueNm[i]:F4} Nm | I {physicsBody.MeasuredCurrentA[i]?.ToString("F2") ?? "—"} A | J {physicsBody.AdvanceRatio[i]:F2} | {(physicsBody.PerformanceClamped[i] ? "CLAMP" : "in range")}");
                if(p.GroundEffect!=null) GUILayout.Label($"  {p.Rotors[i].Id} ground h {(double.IsPositiveInfinity(physicsBody.GroundHeightM[i]) ? "—" : physicsBody.GroundHeightM[i].ToString("F3"))} m | GE x{physicsBody.GroundEffectMultiplier[i]:F3}");
            }
            GUILayout.EndArea();
        }
    }
}
