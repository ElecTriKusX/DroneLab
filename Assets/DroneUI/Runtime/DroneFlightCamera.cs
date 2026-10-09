using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneLab.UI
{
    public enum DroneFlightCameraMode { Chase, Fpv }

    /// <summary>One HDRP camera, two views. Never owns input axes or physical forces.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class DroneFlightCamera : MonoBehaviour
    {
        public DroneFlightCameraMode Mode { get; private set; }
        public bool Paused { get; set; }
        public bool PointerOverUI { get; set; }
        private Transform target;
        private Camera flightCamera;
        private Vector3 dimensions;
        private float distance, baseDistance, orbitYaw, orbitPitch = 18;
        private float originalFov, originalNear;
        private bool configured, snap;
        private bool capturingOrbit, previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private readonly RaycastHit[] obstacles = new RaycastHit[32];

        public void Configure(Transform selectedTarget, Vector3 size)
        {
            target = selectedTarget; dimensions = size;
            flightCamera = GetComponent<Camera>();
            if (!configured) { originalFov = flightCamera.fieldOfView; originalNear = flightCamera.nearClipPlane; }
            configured = true;
            // Reuse the scene camera: HDRP exposure, volumes and weather bindings stay intact.
            var legacy = GetComponent<DroneTestCamera>(); if (legacy != null) legacy.enabled = false;
            baseDistance = Mathf.Max(2.5f, Mathf.Max(size.x, Mathf.Max(size.y, size.z)) * 3);
            distance = baseDistance; flightCamera.nearClipPlane = .02f;
            SetMode(DroneFlightCameraMode.Chase); SnapToTarget();
        }
        public void ToggleMode() => SetMode(Mode == DroneFlightCameraMode.Chase ? DroneFlightCameraMode.Fpv : DroneFlightCameraMode.Chase);
        public void SetMode(DroneFlightCameraMode mode)
        {
            ReleaseOrbit();
            Mode = mode; snap = true;
            if (flightCamera != null) flightCamera.fieldOfView = mode == DroneFlightCameraMode.Fpv ? 90 : originalFov;
        }
        public void ResetOrbit() { orbitYaw = 0; orbitPitch = 18; distance = baseDistance; snap = true; }
        public void SnapToTarget() { snap = true; PlaceCamera(0); }
        private void LateUpdate()
        {
            if (!configured || target == null) return;
            var mouse = Mouse.current;
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
                    orbitYaw += delta.x * .18f; orbitPitch = Mathf.Clamp(orbitPitch - delta.y * .18f, -15, 80);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > .01f)
                    distance = Mathf.Clamp(distance * Mathf.Exp(-scroll * .0015f), baseDistance * .45f, baseDistance * 6);
                if (mouse.middleButton.wasPressedThisFrame) ResetOrbit();
            }
            // Physics remains interpolated; camera follows the rendered transform, not the raw Rigidbody pose.
            PlaceCamera(Paused ? 0 : Time.deltaTime);
        }
        private void PlaceCamera(float dt)
        {
            if (target == null || flightCamera == null) return;
            if (Mode == DroneFlightCameraMode.Fpv)
            {
                // A provisional camera mount outside the profile's nose, independent of any imported GLB hierarchy.
                var mount = new Vector3(0, dimensions.y * .15f, dimensions.z * .5f + .06f);
                transform.SetPositionAndRotation(target.TransformPoint(mount), target.rotation);
                snap = false; return;
            }
            var focus = target.position + Vector3.up * dimensions.y * .15f;
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
        }
    }
}
