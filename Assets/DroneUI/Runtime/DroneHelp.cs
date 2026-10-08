using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal static class DroneHelp
    {
        public static VisualElement Stage(VisualElement element)
        {
            for (var parent = element; parent != null; parent = parent.parent) if (parent.ClassListContains("stage")) return parent;
            return element.panel.visualTree;
        }
        public static void Attach(VisualElement field, Func<string> text)
        {
            var button = new Button { text = "?" }; button.AddToClassList("parameter-help"); field.Add(button);
            VisualElement popup = null;
            void Hide() { popup?.RemoveFromHierarchy(); popup = null; }
            void Show()
            {
                Hide(); if (button.panel == null) return;
                var host = Stage(button); var anchor = host.WorldToLocal(button.worldBound.max);
                popup = new VisualElement(); popup.AddToClassList("parameter-tooltip"); popup.pickingMode = PickingMode.Ignore;
                var label = new Label(text()); label.pickingMode = PickingMode.Ignore; label.selection.isSelectable = false; popup.Add(label);
                popup.style.left = Mathf.Clamp(anchor.x + 8, 12, Mathf.Max(12, host.resolvedStyle.width - 432));
                popup.style.top = Mathf.Max(12, anchor.y - 16); host.Add(popup);
                popup.RegisterCallback<GeometryChangedEvent>(_ => { if (popup != null) popup.style.top = Mathf.Min(anchor.y - 16, Mathf.Max(12, host.resolvedStyle.height - popup.resolvedStyle.height - 12)); });
            }
            button.RegisterCallback<PointerEnterEvent>(_ => Show()); button.RegisterCallback<PointerLeaveEvent>(_ => Hide());
            button.RegisterCallback<FocusInEvent>(_ => Show()); button.RegisterCallback<FocusOutEvent>(_ => Hide());
            button.clicked += () => { if (popup == null) Show(); else Hide(); };
            field.RegisterCallback<DetachFromPanelEvent>(_ => Hide());
        }
    }
}
