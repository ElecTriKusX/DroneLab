using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal sealed class DroneDoubleField : DoubleField
    {
        public DroneDoubleField(string label = null) : base(label) { }
        public void RestoreInput(string input) => text = input;
    }
    internal sealed class DroneIntegerField : IntegerField
    {
        public DroneIntegerField(string label = null) : base(label) { }
        public void RestoreInput(string input) => text = input;
    }
}
