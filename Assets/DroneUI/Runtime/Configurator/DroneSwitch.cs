using System;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Button-based switch, isolated from TextField and Foldout input styles.</summary>
    internal sealed class DroneSwitch : Button
    {
        public bool Value { get; private set; }
        public event Action<bool> ValueChanged;
        private readonly string caption;
        public DroneSwitch(string caption,bool value)
        {
            this.caption=caption;AddToClassList("drone-switch");
            var label=new Label(caption){pickingMode=PickingMode.Ignore};label.AddToClassList("drone-switch-label");label.selection.isSelectable=false;Add(label);
            var track=new VisualElement{pickingMode=PickingMode.Ignore};track.AddToClassList("drone-switch-track");Add(track);
            var knob=new VisualElement{pickingMode=PickingMode.Ignore};knob.AddToClassList("drone-switch-knob");track.Add(knob);
            clicked+=()=>{SetValueWithoutNotify(!Value);ValueChanged?.Invoke(Value);};SetValueWithoutNotify(value);
        }
        public void SetValueWithoutNotify(bool value)
        {
            Value=value;EnableInClassList("is-on",value);tooltip=caption+": "+(value?"включено":"выключено");
        }
    }
}
