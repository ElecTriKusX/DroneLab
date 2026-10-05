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
            double roll=(kb.dKey.isPressed?1:0)-(kb.aKey.isPressed?1:0);
            double pitch=(kb.wKey.isPressed?1:0)-(kb.sKey.isPressed?1:0);
            double yaw=(kb.eKey.isPressed?1:0)-(kb.qKey.isPressed?1:0);
            double climb=(kb.spaceKey.isPressed?1:0)-((kb.leftCtrlKey.isPressed||kb.rightCtrlKey.isPressed)?1:0);
            return new PilotInputFrame(new FlightInput(roll,pitch,yaw,climb,climb>0 ? keyboardThrust : 0),
                true,kb.fKey.wasPressedThisFrame,kb.zKey.wasPressedThisFrame,
                kb.hKey.wasPressedThisFrame,kb.backspaceKey.wasPressedThisFrame,kb.deviceId);
        }
    }
}
