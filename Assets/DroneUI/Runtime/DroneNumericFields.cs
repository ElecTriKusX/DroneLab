using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal static class DroneTextInput
    {
        public static void Configure<T>(TextInputBaseField<T> field)
        {
            field.textSelection.selectAllOnFocus = false;
            field.textSelection.selectAllOnMouseUp = false;
        }
    }
    internal sealed class DroneDoubleField : DoubleField
    {
        public DroneDoubleField(string label = null) : base(label) { DroneTextInput.Configure(this); }
        public void RestoreInput(string input) => text = input;
    }
    internal sealed class DroneIntegerField : IntegerField
    {
        public DroneIntegerField(string label = null) : base(label) { DroneTextInput.Configure(this); }
        public void RestoreInput(string input) => text = input;
    }
}
