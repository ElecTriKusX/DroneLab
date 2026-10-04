using DroneLab.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DroneLab.Physics.Tests
{
    public sealed class PilotInputTests
    {
        private Keyboard keyboard, previousKeyboard;
        private Gamepad gamepad, previousGamepad;
        [SetUp] public void SetUp()
        {
            previousKeyboard=Keyboard.current; previousGamepad=Gamepad.current;
            keyboard=InputSystem.AddDevice<Keyboard>(); gamepad=InputSystem.AddDevice<Gamepad>();
        }
        [TearDown] public void TearDown()
        {
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(gamepad);
            previousKeyboard?.MakeCurrent(); previousGamepad?.MakeCurrent();
        }
        [Test] public void KeyboardMapsSignsActionsAndZeroThrottleOnRelease()
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.W,Key.E,Key.Space,Key.F)); InputSystem.Update();
            var frame=DronePilotInput.Read(PilotDevice.Keyboard,0.38);
            Assert.That(frame.Command.Roll,Is.EqualTo(1)); Assert.That(frame.Command.Pitch,Is.EqualTo(1));
            Assert.That(frame.Command.Yaw,Is.EqualTo(1)); Assert.That(frame.Command.Climb,Is.EqualTo(1));
            Assert.That(frame.Command.Throttle,Is.EqualTo(0.38)); Assert.That(frame.Arm,Is.True);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
            Assert.That(DronePilotInput.Read(PilotDevice.Keyboard,0.38).Command.Throttle,Is.Zero);
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
        }
    }
}
