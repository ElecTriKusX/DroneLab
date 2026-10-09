using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace DroneLab.UI
{
    public enum DroneFlightCameraMode { Chase, Fpv, Free }

    /// <summary>One HDRP observer camera: chase, FPV and route free view. Never owns input axes or physical forces.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class DroneFlightCamera : MonoBehaviour
    {
        public DroneFlightCameraMode Mode { get; private set; }
        public bool Paused { get; set; }
        public bool PointerOverUI { get; set; }
        public Camera Camera => flightCamera;
        public DroneSensorRig SensorRig { get; set; }
        public float OrbitSensitivity { get; set; } = .18f;
        public float ZoomSensitivity { get; set; } = .22f;
        private Transform target;
        private Camera flightCamera;
        private Vector3 dimensions;
        private float distance, baseDistance, orbitYaw, orbitPitch = 22;
        private float originalFov, originalNear;
        private float freeYaw, freePitch=55, freeSpeed=12;
        private bool configured, snap;
        private bool capturingOrbit, previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private HDAdditionalCameraData hdCamera;
        private FrameSettings savedFrames;
        private FrameSettingsOverrideMask savedMask;
        private bool savedCustomSettings;
        private readonly RaycastHit[] obstacles = new RaycastHit[32];

        public void Configure(Transform selectedTarget, Vector3 size)
        {
            target = selectedTarget; dimensions = size;
            flightCamera = GetComponent<Camera>();
            if (!configured) { originalFov = flightCamera.fieldOfView; originalNear = flightCamera.nearClipPlane; }
            configured = true;
            // Reuse the scene camera: HDRP exposure, volumes and weather bindings stay intact.
            var legacy = GetComponent<DroneTestCamera>(); if (legacy != null) legacy.enabled = false;
            baseDistance = Mathf.Max(3.6f, Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * 4);
            distance = baseDistance; flightCamera.nearClipPlane = .02f;
            OrbitSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("DroneLab.CameraOrbit", .18f), .05f, .6f);
            ZoomSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("DroneLab.CameraZoom", .22f), .08f, .55f);
            hdCamera = GetComponent<HDAdditionalCameraData>();
            if (hdCamera != null) {
                savedFrames = hdCamera.renderingPathCustomFrameSettings; savedMask = hdCamera.renderingPathCustomFrameSettingsOverrideMask;
                savedCustomSettings = hdCamera.customRenderingSettings;
                MakeSharp(hdCamera);
            }
            SetMode(DroneFlightCameraMode.Chase); SnapToTarget();
        }
        public void ToggleMode() => SetMode(Mode == DroneFlightCameraMode.Chase ? DroneFlightCameraMode.Fpv : DroneFlightCameraMode.Chase);
        public void SetMode(DroneFlightCameraMode mode)
        {
            ReleaseOrbit();
            bool enteringFree=mode==DroneFlightCameraMode.Free && Mode!=mode;
            Mode = mode; snap = true;
            if(enteringFree && target!=null) {
                transform.position=target.position+new Vector3(0,18,-12);
                freeYaw=0; freePitch=55; transform.rotation=Quaternion.Euler(freePitch,freeYaw,0);
            }
            if (flightCamera != null) flightCamera.fieldOfView = mode == DroneFlightCameraMode.Fpv ? 90 : originalFov;
        }
        public static void MakeSharp(HDAdditionalCameraData data)
        {
            data.customRenderingSettings = true;
            foreach (var field in new[] { FrameSettingsField.DepthOfField, FrameSettingsField.MotionBlur, FrameSettingsField.ChromaticAberration }) {
                data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
                data.renderingPathCustomFrameSettings.SetEnabled(field, false);
            }
        }
        public void ResetOrbit() { orbitYaw = 0; orbitPitch = 22; distance = baseDistance; snap = true; }
        public void SnapToTarget() { snap = true; PlaceCamera(0); }
        private void LateUpdate()
        {
            if (!configured || target == null) return;
            var mouse = Mouse.current;
            if(Mode==DroneFlightCameraMode.Free) {
                if(!Paused && Application.isFocused && !PointerOverUI) {
                    if(mouse?.rightButton.isPressed==true) { var delta=mouse.delta.ReadValue(); freeYaw+=delta.x*OrbitSensitivity; freePitch=Mathf.Clamp(freePitch-delta.y*OrbitSensitivity,-85,85); }
                    if(mouse!=null) freeSpeed=DroneFlightMath.ZoomDistance(freeSpeed,-mouse.scroll.ReadValue().y,.15f,2,60);
                    var k=Keyboard.current; if(k!=null) {
                        float x=(k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0), z=(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0), y=(k.eKey.isPressed?1:0)-(k.qKey.isPressed?1:0);
                        var direction=Quaternion.Euler(0,freeYaw,0)*Vector3.ClampMagnitude(new Vector3(x,0,z),1)+Vector3.up*y;
                        transform.position+=direction*freeSpeed*Time.deltaTime;
                    }
                    transform.rotation=Quaternion.Euler(freePitch,freeYaw,0);
                }
                return;
            }
            bool canOrbit = !Paused && Application.isFocused && !PointerOverUI && Mode == DroneFlightCameraMode.Chase && mouse != null;
            if (capturingOrbit && (!canOrbit || !mouse.rightButton.isPressed)) ReleaseOrbit();
            if (canOrbit)
            {
                if (mouse.rightButton.isPressed)
                {
                    if (!capturingOrbit) {
                        previousCursorLock = UnityEngine.Cursor.lockState; previousCursorVisible = UnityEngine.Cursor.visible;
                        UnityEngine.Cursor.lockState = CursorLockMode.Locked; UnityEngine.Cursor.visible = false; capturingOrbit = true;
                    }
                    var delta = mouse.delta.ReadValue();
                    orbitYaw += delta.x * OrbitSensitivity; orbitPitch = Mathf.Clamp(orbitPitch - delta.y * OrbitSensitivity, -15, 80);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .01f)
                    // Input System 1.20 may report wheel ticks as +/-1 rather than +/-120.
                    distance = DroneFlightMath.ZoomDistance(distance, scroll, ZoomSensitivity, baseDistance * .45f, baseDistance * 8);
                if (mouse.middleButton.wasPressedThisFrame) ResetOrbit();
            }
            // Physics remains interpolated; camera follows the rendered transform, not the raw Rigidbody pose.
            PlaceCamera(Paused ? 0 : Time.deltaTime);
        }
        private void PlaceCamera(float dt)
        {
            if (target == null || flightCamera == null || Mode==DroneFlightCameraMode.Free) return;
            if (Mode == DroneFlightCameraMode.Fpv)
            {
                // A provisional camera mount outside the profile's nose, independent of any imported GLB hierarchy.
                var mount = SensorRig!=null ? SensorRig.LocalPosition(DroneLab.Sensors.SensorKind.Camera) : new Vector3(0, dimensions.y * .15f, dimensions.z * .5f + .06f);
                var fpvRotation=target.rotation*(SensorRig!=null ? SensorRig.LocalRotation(DroneLab.Sensors.SensorKind.Camera) : Quaternion.identity);
                if(SensorRig!=null) flightCamera.fieldOfView=(float)SensorRig.Settings(DroneLab.Sensors.SensorKind.Camera).cameraFovDeg;
                transform.SetPositionAndRotation(target.TransformPoint(mount), fpvRotation);
                snap = false; return;
            }
            var focus = target.position + Vector3.up * Mathf.Max(.18f, dimensions.y * .3f);
            float heading = DroneFlightMath.Heading(target.forward, target.eulerAngles.y);
            var rotation = Quaternion.Euler(orbitPitch, heading + orbitYaw, 0);
            var offset = rotation * Vector3.back * distance;
            float safeDistance = distance;
            int count = UnityEngine.Physics.SphereCastNonAlloc(focus, .12f, offset.normalized, obstacles, distance,
                UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var hits = count == obstacles.Length ? UnityEngine.Physics.SphereCastAll(focus, .12f, offset.normalized, distance,
                UnityEngine.Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) : obstacles;
            if (!ReferenceEquals(hits, obstacles)) count = hits.Length;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(target)) continue;
                safeDistance = Mathf.Min(safeDistance, Mathf.Max(.2f, hit.distance - .08f));
            }
            var desired = focus + offset.normalized * safeDistance;
            // Pull in immediately at obstacles, smooth out after leaving them.
            bool obstructed = safeDistance < distance - .01f;
            float blend = snap || obstructed ? 1 : 1 - Mathf.Exp(-7 * Mathf.Max(0, dt));
            transform.position = Vector3.Lerp(transform.position, desired, blend);
            var direction = focus - transform.position;
            if (direction.sqrMagnitude > .00001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            snap = false;
        }
        private void ReleaseOrbit()
        {
            if (!capturingOrbit) return;
            UnityEngine.Cursor.lockState = previousCursorLock; UnityEngine.Cursor.visible = previousCursorVisible; capturingOrbit = false;
        }
        private void OnDisable() => ReleaseOrbit();
        private void OnDestroy()
        {
            ReleaseOrbit();
            if (configured && flightCamera != null) { flightCamera.fieldOfView = originalFov; flightCamera.nearClipPlane = originalNear; }
            if (hdCamera != null) {
                hdCamera.renderingPathCustomFrameSettings = savedFrames;
                hdCamera.renderingPathCustomFrameSettingsOverrideMask = savedMask; hdCamera.customRenderingSettings = savedCustomSettings;
            }
        }
    }
}
