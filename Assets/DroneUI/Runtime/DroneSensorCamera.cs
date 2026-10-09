using System.Collections.Generic;
using DroneLab.Physics;
using DroneLab.Sensors;
using DroneLab.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DroneLab.UI
{
    [DefaultExecutionOrder(400)]
    public sealed class DroneSensorCamera : MonoBehaviour
    {
        private const int Width = 320, Height = 180;
        private struct Frame { public SensorReading reading; public RenderTexture pixels; }
        private readonly Queue<Frame> frames = new Queue<Frame>();
        private readonly Stack<RenderTexture> pool = new Stack<RenderTexture>();
        private Camera sensorCamera;
        private RenderTexture capture, displayed;
        private DroneSensorRig rig;
        private DronePhysicsBody body;
        private double capturingAt, previousTime;
        public Texture Image => rig.Channel(SensorKind.Camera).Latest?.Valid == true ? displayed : null;
        public Transform Mount => sensorCamera != null ? sensorCamera.transform : null;
        public void Configure(DroneSensorRig selectedRig, DronePhysicsBody selectedBody, Camera source)
        {
            rig = selectedRig; body = selectedBody;
            var go = new GameObject("DroneLab sensor camera"); go.transform.SetParent(transform, false);
            sensorCamera = go.AddComponent<Camera>();
            if (source != null) sensorCamera.CopyFrom(source);
            var hd = go.AddComponent<HDAdditionalCameraData>();
            if (source != null) source.GetComponent<HDAdditionalCameraData>()?.CopyTo(hd);
            DroneFlightCamera.MakeSharp(hd);
            sensorCamera.nearClipPlane = .02f; sensorCamera.depth = -10; sensorCamera.enabled = false;
            capture = Texture(24); displayed = Texture(0); sensorCamera.targetTexture = capture; sensorCamera.aspect = 16f / 9;
            RenderPipelineManager.endCameraRendering += Rendered;
        }
        private static RenderTexture Texture(int depth)
        { var texture = new RenderTexture(Width, Height, depth, RenderTextureFormat.ARGB32) { name = "DroneLab sensor frame" }; texture.Create(); return texture; }
        private void LateUpdate()
        {
            if (rig == null || sensorCamera == null || body == null || !body.IsReady) return;
            double now = body.SimulationTimeS;
            if (now < previousTime) ClearFrames(); previousTime = now;
            var channel = rig.Channel(SensorKind.Camera);
            channel.Advance(now); Deliver(now);
            sensorCamera.transform.localPosition = rig.LocalPosition(SensorKind.Camera);
            sensorCamera.transform.localRotation = rig.LocalRotation(SensorKind.Camera);
            sensorCamera.fieldOfView = (float)channel.Settings.cameraFovDeg;
            // A rendered frame is never fabricated to satisfy a requested rate higher than render FPS.
            sensorCamera.enabled = Time.timeScale > 0 && Application.isFocused && channel.Due(now);
            capturingAt = now;
        }
        private void Rendered(ScriptableRenderContext context, Camera rendered)
        {
            if (rendered != sensorCamera || rig == null) return;
            var channel = rig.Channel(SensorKind.Camera);
            if (!channel.Due(capturingAt)) return;
            var reading = channel.Capture(capturingAt, new DVector3(Width, Height, sensorCamera.fieldOfView), true);
            var pixels = pool.Count > 0 ? pool.Pop() : Texture(0);
            // Keep copies ordered after the camera's SRP commands, before the display camera reads them.
            var commands = CommandBufferPool.Get("DroneLab sensor frame");
            commands.Blit(capture, pixels); frames.Enqueue(new Frame { reading = reading, pixels = pixels });
            channel.Advance(capturingAt); Deliver(capturingAt, commands);
            context.ExecuteCommandBuffer(commands); CommandBufferPool.Release(commands);
        }
        private void Deliver(double now, CommandBuffer commands = null)
        {
            while (frames.Count > 0 && frames.Peek().reading.DeliverAt <= now + 1e-8) {
                var frame = frames.Dequeue();
                if (commands != null) commands.Blit(frame.pixels, displayed); else Graphics.Blit(frame.pixels, displayed);
                pool.Push(frame.pixels);
            }
        }
        public void ClearFrames()
        { while (frames.Count > 0) pool.Push(frames.Dequeue().pixels); if (sensorCamera != null) sensorCamera.enabled = false; }
        private void OnDestroy()
        {
            RenderPipelineManager.endCameraRendering -= Rendered;
            if (sensorCamera != null) sensorCamera.targetTexture = null;
            ClearFrames(); Release(capture); Release(displayed);
            while (pool.Count > 0) Release(pool.Pop());
            if (sensorCamera != null) Destroy(sensorCamera.gameObject);
        }
        private static void Release(RenderTexture texture) { if (texture != null) { texture.Release(); Destroy(texture); } }
    }
}
