using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PropSpawner
{
    public class DetailsPanel : ScrollView
    {
        PropSpawnerWindow window;

        public DetailsPanel(PropSpawnerWindow window)
        {
            this.window = window;
            name = UIStrings.detailsPanelName;
            style.minWidth = PropSpawnerWindow.detailsPanelMinWidth;
            ShowSelection();
        }

        public void ShowSelection()
        {
            Clear();
            var selection = window.SelectedAssets;
            if (selection.Count != 1) { return; }
            if (selection[0] is Rules)
            {
                Add(new RulesDetailsTile(selection[0] as Rules));
            }
        }

        public void ShowSettings(PropSpawnerSettings settings)
        {
            Clear();
            var settingsDetailsTile = new SettingsDetailsTile(settings);
            Add(settingsDetailsTile);
            var closeButton = settingsDetailsTile.Q<Button>(className: UIStrings.closeButtonClass);
            closeButton.clicked += (() =>
            {
                ShowSelection();
            });
        }
    }
}