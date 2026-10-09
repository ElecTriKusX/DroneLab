using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DroneLab.Simulation
{
    public enum FlightKeyAction { RollLeft, RollRight, PitchForward, PitchBack, YawLeft, YawRight,
        Climb, Descend, Arm, ControlMode, AltitudeHold, Reset, Cinema, Pilot, Engineer, Diagnostics, Camera, Controls, PositionHold, Route }

    /// <summary>Shared keyboard assignments for the pilot and flight UI. Escape always remains available.</summary>
    public static class DroneKeyBindings
    {
        private const string Preference = "DroneLab.FlightKeys.v1";
        private static readonly Key[] Defaults = { Key.A, Key.D, Key.W, Key.S, Key.Q, Key.E, Key.Space,
            Key.LeftCtrl, Key.F, Key.Z, Key.H, Key.Backspace, Key.F1, Key.F2, Key.F3, Key.F4, Key.C, Key.F10, Key.J, Key.F5 };
        private static readonly string[] Names = { "Крен влево", "Крен вправо", "Тангаж вперёд", "Тангаж назад",
            "Поворот влево", "Поворот вправо", "Тяга / подъём", "Снижение (удержание высоты)", "Моторы вкл. / выкл.",
            "Angle / ручной режим", "Удержание высоты", "Вернуть дрон на старт", "Кино", "Пилот", "Инженер", "Диагностика", "Третье лицо / FPV", "Настройки управления", "Удержание точки X/Z", "Маршрут / свободная камера" };
        private static Key[] keys;
        [Serializable] private sealed class Saved { public int[] keys; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => keys = null;
        public static void Reload() => keys = null;
        public static int Count => Defaults.Length;
        public static string Name(FlightKeyAction action) => Names[(int)action];
        public static Key Get(FlightKeyAction action) { EnsureLoaded(); return keys[(int)action]; }
        public static string Caption(FlightKeyAction action)
        {
            var key = Get(action);
            if (key == Key.LeftCtrl) return "Ctrl";
            if (key == Key.Space) return "Space";
            return key.ToString();
        }
        private static Key Normalize(Key key) => key == Key.RightCtrl ? Key.LeftCtrl : key;
        public static bool TryAssign(FlightKeyAction action, Key key, out string message)
        {
            EnsureLoaded(); key = Normalize(key);
            if (key == Key.None || key == Key.Escape || !Enum.IsDefined(typeof(Key), key))
            { message = "Esc зарезервирован для закрытия меню и паузы."; return false; }
            for (int i = 0; i < keys.Length; i++)
                if (i != (int)action && keys[i] == key)
                { message = "Клавиша уже назначена: " + Names[i] + ". Сначала измените это назначение."; return false; }
            keys[(int)action] = key; Save(); message = "Назначение сохранено."; return true;
        }
        public static void RestoreDefaults() { keys = (Key[])Defaults.Clone(); Save(); }
        private static void EnsureLoaded()
        {
            if (keys != null) return;
            keys = (Key[])Defaults.Clone();
            try {
                var saved = JsonUtility.FromJson<Saved>(PlayerPrefs.GetString(Preference, ""));
                if (saved?.keys == null || saved.keys.Length > Count || saved.keys.Length < 18) return;
                var candidate = (Key[])Defaults.Clone();
                for (int i = 0; i < saved.keys.Length; i++) {
                    var key = Normalize((Key)saved.keys[i]);
                    if (key == Key.None || key == Key.Escape || !Enum.IsDefined(typeof(Key), key)) return;
                    for (int j = 0; j < i; j++) if (candidate[j] == key) return;
                    candidate[i] = key;
                }
                for (int i=saved.keys.Length;i<Count;i++) {
                    bool duplicate=false; for(int j=0;j<i;j++) duplicate|=candidate[j]==candidate[i];
                    if(duplicate) foreach(Key fallback in Enum.GetValues(typeof(Key))) {
                        if(fallback==Key.None || fallback==Key.Escape) continue;
                        bool used=false; for(int j=0;j<i;j++) used|=Normalize(fallback)==candidate[j];
                        if(!used) { candidate[i]=Normalize(fallback); break; }
                    }
                }
                keys = candidate;
            } catch (ArgumentException) { /* Corrupt preferences fall back to a complete valid map. */ }
        }
        private static void Save()
        {
            var saved = new Saved { keys = new int[Count] };
            for (int i = 0; i < Count; i++) saved.keys[i] = (int)keys[i];
            PlayerPrefs.SetString(Preference, JsonUtility.ToJson(saved)); PlayerPrefs.Save();
        }
        public static bool Held(Keyboard keyboard, FlightKeyAction action)
        {
            if (keyboard == null) return false;
            var key = Get(action);
            return keyboard[key].isPressed || key == Key.LeftCtrl && keyboard.rightCtrlKey.isPressed;
        }
        public static bool Pressed(Keyboard keyboard, FlightKeyAction action) => keyboard != null && keyboard[Get(action)].wasPressedThisFrame;
    }
}
