using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    /// <summary>Runtime dropdown. The popup stays inside the same scaled stage and USS theme.</summary>
    public sealed class DroneDropdown : BaseField<string>
    {
        public List<string> choices { get; }
        public event Action Reselected;
        public int index {
            get => choices.IndexOf(value);
            set { if (value >= 0 && value < choices.Count) this.value = choices[value]; }
        }
        private readonly Button trigger;
        private readonly Label caption;
        private VisualElement overlay;
        private Action stageEscape;
        public DroneDropdown(string title, List<string> items, int initial) : base(title, new VisualElement())
        {
            choices = items; AddToClassList("drone-dropdown");
            var input = this.Q<VisualElement>(className: "unity-base-field__input");
            trigger = new Button(Toggle); trigger.AddToClassList("drone-dropdown-trigger"); input.Add(trigger);
            caption = new Label(); caption.AddToClassList("drone-dropdown-value"); caption.pickingMode = PickingMode.Ignore; trigger.Add(caption);
            var arrow = new Label("⌄"); arrow.AddToClassList("drone-dropdown-arrow"); arrow.pickingMode = PickingMode.Ignore; trigger.Add(arrow);
            SetValueWithoutNotify(items.Count == 0 ? "" : items[Mathf.Clamp(initial, 0, items.Count - 1)]);
            RegisterValueChangedCallback(_ => caption.text = value);
            trigger.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow) { Open(); evt.StopPropagation(); }
            });
            RegisterCallback<DetachFromPanelEvent>(_ => Close());
        }
        public override void SetValueWithoutNotify(string newValue) { base.SetValueWithoutNotify(newValue); if (caption != null) caption.text = newValue; }
        public void SetCaption(string text) => caption.text = text;
        private void Toggle() { if (overlay == null) Open(); else Close(); }
        private void Open()
        {
            if (overlay != null || panel == null || choices.Count == 0) return;
            VisualElement host = DroneHelp.Stage(this);
            var anchor = host.WorldToLocal(trigger.worldBound.min);
            float width = Mathf.Max(180, host.WorldToLocal(trigger.worldBound.max).x - anchor.x);
            float bottom = host.WorldToLocal(trigger.worldBound.max).y;
            float height = Mathf.Min(300, choices.Count * 34 + 12);
            float y = bottom + height < host.resolvedStyle.height - 12 ? bottom + 4 : Mathf.Max(12, anchor.y - height - 4);
            overlay = new VisualElement(); overlay.AddToClassList("drone-dropdown-overlay"); host.Add(overlay);
            var popup = new ScrollView(); popup.AddToClassList("drone-dropdown-popup"); overlay.Add(popup);
            popup.style.left = Mathf.Clamp(anchor.x, 12, Mathf.Max(12, host.resolvedStyle.width - width - 12)); popup.style.top = y;
            popup.style.width = width; popup.style.height = height;
            popup.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            popup.verticalScroller.AddToClassList("graphite-scroller");
            popup.verticalScroller.lowButton.style.display = DisplayStyle.None; popup.verticalScroller.highButton.style.display = DisplayStyle.None;
            overlay.RegisterCallback<PointerDownEvent>(evt => { if (evt.target == overlay) { Close(); evt.StopPropagation(); } });
            var buttons = new List<Button>();
            for (int i = 0; i < choices.Count; i++) {
                int selected = i;
                var option = new Button(() => { bool same = index == selected; index = selected; if (same) Reselected?.Invoke(); Close(); trigger.Focus(); }) { text = choices[i] };
                option.AddToClassList("drone-dropdown-option"); option.EnableInClassList("selected", i == index); popup.Add(option); buttons.Add(option);
                option.RegisterCallback<KeyDownEvent>(evt => {
                    if (evt.keyCode == KeyCode.Escape) { Close(); trigger.Focus(); evt.StopPropagation(); }
                    else if (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow) {
                        int next = (selected + (evt.keyCode == KeyCode.DownArrow ? 1 : -1) + buttons.Count) % buttons.Count;
                        buttons[next].Focus(); popup.ScrollTo(buttons[next]); evt.StopPropagation();
                    }
                });
            }
            popup.schedule.Execute(() => { int active = Mathf.Max(0, index); buttons[active].Focus(); popup.ScrollTo(buttons[active]); });
            // Capture Escape before the menu's own navigation handler.
            stageEscape = () => { Close(); trigger.Focus(); };
            host.RegisterCallback<KeyDownEvent>(Escape, TrickleDown.TrickleDown);
        }
        private void Escape(KeyDownEvent evt) { if (evt.keyCode == KeyCode.Escape) { stageEscape?.Invoke(); evt.StopImmediatePropagation(); } }
        private void Close()
        {
            if (overlay == null) return;
            overlay.parent?.UnregisterCallback<KeyDownEvent>(Escape, TrickleDown.TrickleDown);
            overlay.RemoveFromHierarchy(); overlay = null; stageEscape = null;
        }
    }
}
