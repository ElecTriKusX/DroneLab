using DroneLab.Physics;
using UnityEngine.InputSystem;

namespace DroneLab.Simulation
{
    public enum PilotDevice { Keyboard, Gamepad }
    public readonly struct PilotInputFrame
    {
        public readonly FlightInput Command;
        public readonly bool Available, Arm, Mode, Altitude, Reset;
        public readonly int DeviceId;
        public PilotInputFrame(FlightInput command, bool available, bool arm, bool mode, bool altitude, bool reset, int deviceId)
        { Command=command; Available=available; Arm=arm; Mode=mode; Altitude=altitude; Reset=reset; DeviceId=deviceId; }
    }
    // Only this adapter knows about Unity input devices. FlightInput is usable by a future radio/AI adapter.
    public static class DronePilotInput
    {
        public static PilotInputFrame Read(PilotDevice device, double keyboardThrust)
        {
            if(device==PilotDevice.Gamepad)
            {
                var pad=Gamepad.current;
                if(pad==null) return default;
                var left=pad.leftStick.ReadValue(); var right=pad.rightStick.ReadValue();
                return new PilotInputFrame(new FlightInput(right.x,right.y,left.x,left.y,pad.rightTrigger.ReadValue()),
                    true,pad.startButton.wasPressedThisFrame,pad.buttonWest.wasPressedThisFrame,
                    pad.buttonSouth.wasPressedThisFrame,pad.buttonNorth.wasPressedThisFrame,pad.deviceId);
            }
            var kb=Keyboard.current;
            if(kb==null) return default;
            double roll=(DroneKeyBindings.Held(kb,FlightKeyAction.RollRight)?1:0)-(DroneKeyBindings.Held(kb,FlightKeyAction.RollLeft)?1:0);
            double pitch=(DroneKeyBindings.Held(kb,FlightKeyAction.PitchForward)?1:0)-(DroneKeyBindings.Held(kb,FlightKeyAction.PitchBack)?1:0);
            double yaw=(DroneKeyBindings.Held(kb,FlightKeyAction.YawRight)?1:0)-(DroneKeyBindings.Held(kb,FlightKeyAction.YawLeft)?1:0);
            double climb=(DroneKeyBindings.Held(kb,FlightKeyAction.Climb)?1:0)-(DroneKeyBindings.Held(kb,FlightKeyAction.Descend)?1:0);
            return new PilotInputFrame(new FlightInput(roll,pitch,yaw,climb,climb>0 ? keyboardThrust : 0),
                true,DroneKeyBindings.Pressed(kb,FlightKeyAction.Arm),DroneKeyBindings.Pressed(kb,FlightKeyAction.ControlMode),
                DroneKeyBindings.Pressed(kb,FlightKeyAction.AltitudeHold),DroneKeyBindings.Pressed(kb,FlightKeyAction.Reset),kb.deviceId);
        }
    }
}
