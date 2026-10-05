using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DroneLab.Physics.Tests
{
    public sealed class PilotInputTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Gamepad gamepad;
        public override void Setup()
        {
            base.Setup();
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            keyboard=InputSystem.AddDevice<Keyboard>(); gamepad=InputSystem.AddDevice<Gamepad>();
            InputSystem.Update();
            // Register edge tracking on fresh ButtonControls before sending the first press.
            DronePilotInput.Read(PilotDevice.Keyboard,0.38);
            DronePilotInput.Read(PilotDevice.Gamepad,0.38);
        }
        [Test] public void KeyboardMapsSignsActionsAndZeroThrottleOnRelease()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.W,Key.E,Key.Space,Key.F)); InputSystem.Update();
            var frame=DronePilotInput.Read(PilotDevice.Keyboard,0.38);
            Assert.That(frame.Command.Roll,Is.EqualTo(1)); Assert.That(frame.Command.Pitch,Is.EqualTo(1));
            Assert.That(frame.Command.Yaw,Is.EqualTo(1)); Assert.That(frame.Command.Climb,Is.EqualTo(1));
            Assert.That(frame.Command.Throttle,Is.EqualTo(0.38)); Assert.That(frame.Arm,Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
            var released=DronePilotInput.Read(PilotDevice.Keyboard,0.38);
            Assert.That(released.Command.Throttle,Is.Zero); Assert.That(released.Arm,Is.False);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F)); InputSystem.Update();
            Assert.That(DronePilotInput.Read(PilotDevice.Keyboard,0.38).Arm,Is.True);
        }
        [Test] public void GamepadMapsTwoSticksTriggerAndModeButtons()
        {
            var state=new GamepadState { leftStick=Vector2.one, rightStick=-Vector2.one, rightTrigger=0.5f };
            state=state.WithButton(GamepadButton.Start).WithButton(GamepadButton.West).WithButton(GamepadButton.South);
            InputSystem.QueueStateEvent(gamepad,state); InputSystem.Update();
            var frame=DronePilotInput.Read(PilotDevice.Gamepad,0.38);
            Assert.That(frame.Available && frame.Arm && frame.Mode && frame.Altitude,Is.True);
            Assert.That(frame.Command.Roll,Is.LessThan(-0.5)); Assert.That(frame.Command.Pitch,Is.LessThan(-0.5));
            Assert.That(frame.Command.Yaw,Is.GreaterThan(0.5)); Assert.That(frame.Command.Climb,Is.GreaterThan(0.5));
            Assert.That(frame.Command.Throttle,Is.EqualTo(0.5).Within(1e-6));
            InputSystem.Update();
            var held=DronePilotInput.Read(PilotDevice.Gamepad,0.38);
            Assert.That(held.Arm || held.Mode || held.Altitude,Is.False);
            Assert.That(held.Command.Throttle,Is.EqualTo(0.5).Within(1e-6));
        }
    }
}
