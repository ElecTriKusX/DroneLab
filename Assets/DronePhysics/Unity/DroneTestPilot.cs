using System;
using DroneLab.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneLab.Simulation
{
    // Development pilot, separate from forces. Replace ReadKeyboard with joystick input later.
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
        public bool showTelemetry = true;
        public bool Saturated { get; private set; }
        private DronePhysicsBody physicsBody;
        private QuadAllocator allocator;
        private readonly double[] commands=new double[4];
        private float right,forward,yaw,vertical;
        private float targetAltitude;
        private bool previousAltitudeHold;
        private Vector3 startPosition;
        private Quaternion startRotation;

        private void Start()
        {
            physicsBody=GetComponent<DronePhysicsBody>();
            if(!physicsBody.IsReady) { enabled=false; return; }
            try { allocator=new QuadAllocator(physicsBody.Parameters); }
            catch(ArgumentException ex) { Debug.LogError(ex.Message,this); enabled=false; return; }
            startPosition=transform.position; startRotation=transform.rotation;
            targetAltitude=transform.position.y; previousAltitudeHold=altitudeHold;
        }
        private void Update()
        {
            right=forward=yaw=vertical=0;
            var kb=Keyboard.current;
            if(kb == null || !Application.isFocused) return;
            right=(kb.dKey.isPressed?1:0)-(kb.aKey.isPressed?1:0);
            forward=(kb.wKey.isPressed?1:0)-(kb.sKey.isPressed?1:0);
            yaw=(kb.eKey.isPressed?1:0)-(kb.qKey.isPressed?1:0);
            vertical=(kb.spaceKey.isPressed?1:0)-((kb.leftCtrlKey.isPressed||kb.rightCtrlKey.isPressed)?1:0);
            if(kb.fKey.wasPressedThisFrame)
            {
                physicsBody.SetArmed(!physicsBody.Armed);
                targetAltitude=transform.position.y;
            }
            if(kb.zKey.wasPressedThisFrame) autoLevel=!autoLevel;
            if(kb.hKey.wasPressedThisFrame) altitudeHold=!altitudeHold;
            if(kb.backspaceKey.wasPressedThisFrame)
            {
                physicsBody.ResetMotorState();
                physicsBody.Body.position=startPosition; physicsBody.Body.rotation=startRotation;
                physicsBody.Body.linearVelocity=Vector3.zero; physicsBody.Body.angularVelocity=Vector3.zero;
                targetAltitude=startPosition.y;
            }
        }
        private void FixedUpdate()
        {
            if(allocator == null || !physicsBody.IsReady) return;
            var body=physicsBody.Body; var p=physicsBody.Parameters;
            if(altitudeHold != previousAltitudeHold) { targetAltitude=body.position.y; previousAltitudeHold=altitudeHold; }
            if(!physicsBody.Armed) { targetAltitude=body.position.y; return; }
            float upright=Vector3.Dot(transform.up,Vector3.up);
            double collective;
            if(altitudeHold)
            {
                targetAltitude+=vertical*climbSpeedMps*Time.fixedDeltaTime;
                // Bound command integration around current position to avoid unlimited target windup on the ground.
                targetAltitude=Mathf.Clamp(targetAltitude,body.position.y-2,body.position.y+2);
                double accel=PhysicsMath.Clamp(altitudeGain*(targetAltitude-body.position.y)-verticalVelocityGain*body.linearVelocity.y,-4,4);
                collective=upright>0.35f ? p.Mass*(p.Gravity+accel)/upright : 0;
            }
            else collective=vertical>0 ? p.MaxTotalThrust*manualCollectiveFraction : 0;
            // No hidden motor floor when manual throttle is released, even with auto-level enabled.
            if(collective<=0) { for(int i=0;i<4;i++) physicsBody.SetMotorCommand(i,0); Saturated=false; return; }
            Vector3 rate=transform.InverseTransformDirection(body.angularVelocity);
            Vector3 desiredRate;
            if(autoLevel)
            {
                var heading=Quaternion.Euler(0,transform.eulerAngles.y,0);
                var planar=Vector2.ClampMagnitude(new Vector2(right,forward),1);
                float tilt=Mathf.Tan(maxTiltDegrees*Mathf.Deg2Rad);
                Vector3 desiredUp=(Vector3.up+(heading*new Vector3(planar.x,0,planar.y))*tilt).normalized;
                var error=transform.InverseTransformDirection(Vector3.Cross(transform.up,desiredUp));
                desiredRate=Vector3.ClampMagnitude(error*attitudeGain,maxRateDegrees*Mathf.Deg2Rad);
                desiredRate.y=yaw*yawRateDegrees*Mathf.Deg2Rad;
            }
            else desiredRate=new Vector3(forward*maxRateDegrees,yaw*yawRateDegrees,-right*maxRateDegrees)*Mathf.Deg2Rad;
            Vector3 acceleration=(desiredRate-rate)*rateGain;
            // Multiply by full principal-axis inertia; do not assume tensor axes match body axes.
            Quaternion axes=body.inertiaTensorRotation;
            Vector3 MultiplyInertia(Vector3 v) => axes*Vector3.Scale(body.inertiaTensor,Quaternion.Inverse(axes)*v);
            Vector3 torque=MultiplyInertia(acceleration)+Vector3.Cross(rate,MultiplyInertia(rate));
            Saturated=allocator.Allocate(collective,DronePhysicsBody.FromUnity(torque),commands);
            for(int i=0;i<4;i++) physicsBody.SetMotorCommand(i,commands[i]);
        }
        private void OnApplicationFocus(bool focus) { if(!focus && physicsBody != null) physicsBody.SetArmed(false); }
        private void OnDisable() { if(physicsBody != null) physicsBody.SetArmed(false); }
        private void OnGUI()
        {
            if(!showTelemetry || physicsBody == null || !physicsBody.IsReady) return;
            var p=physicsBody.Parameters;
            GUILayout.BeginArea(new Rect(12,12,430,380),GUI.skin.box);
            GUILayout.Label($"DroneLab | {(physicsBody.Armed?"ARMED":"DISARMED")} | {(autoLevel?"ANGLE":"RATE")} | Alt hold: {altitudeHold}");
            GUILayout.Label("F arm | WASD tilt | Q/E yaw | Space/Ctrl lift\nZ level | H altitude hold | Backspace reset");
            GUILayout.Label($"Mass {p.Mass:F2} kg | T/W {p.ThrustToWeight:F2} | Saturation {Saturated}");
            GUILayout.Label($"Height {transform.position.y:F2} m | Velocity {physicsBody.Body.linearVelocity}\nAir velocity {physicsBody.AirVelocity} | Drag {physicsBody.DragForce}");
            for(int i=0;i<p.Rotors.Count;i++) GUILayout.Label($"{p.Rotors[i].Id}: {PhysicsMath.OmegaToRpm(physicsBody.Omega[i]):F0} RPM | {physicsBody.ThrustN[i]:F2} N | {physicsBody.ReactionTorqueNm[i]:F3} Nm");
            GUILayout.EndArea();
        }
    }
}
