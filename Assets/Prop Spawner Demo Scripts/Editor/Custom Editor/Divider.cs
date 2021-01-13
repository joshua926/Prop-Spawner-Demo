using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PropSpawner
{
    public class Divider : VisualElement
    {
        public EventHandler<float> OnResize;
        PropSpawnerWindow window;
        PropSpawnerSettings settings;

        public Divider(PropSpawnerWindow window, PropSpawnerSettings settings)
        {
            this.window = window;
            this.settings = settings;
            name = UIStrings.dividerName;
            style.width = PropSpawnerWindow.dividerWidth;
            RegisterCallback<MouseDownEvent>(e =>
            {
                this.CaptureMouse();
                RegisterCallback<MouseMoveEvent>(ResizeAssetPanelWithCursor);
            });
            RegisterCallback<MouseUpEvent>(e =>
            {
                this.ReleaseMouse();
                UnregisterCallback<MouseMoveEvent>(ResizeAssetPanelWithCursor);
            });
            window.rootVisualElement.RegisterCallback<GeometryChangedEvent>((e) =>
            {
                ResizeAssetPanel(0);
            });

            void ResizeAssetPanelWithCursor(MouseMoveEvent e)
            {
                ResizeAssetPanel(e.localMousePosition.x);
            }

        }
        public void ResizeAssetPanel(float offset)
        {
            float rawWidth = settings.AssetPanelWidth + offset;
            float currentMaxWidth =
                window.position.width - PropSpawnerWindow.dividerWidth - PropSpawnerWindow.detailsPanelMinWidth;
            float newWidth = Mathf.Clamp(rawWidth, PropSpawnerWindow.assetPanelMinWidth, currentMaxWidth);
            OnResize?.Invoke(this, newWidth);
        }
    }
}