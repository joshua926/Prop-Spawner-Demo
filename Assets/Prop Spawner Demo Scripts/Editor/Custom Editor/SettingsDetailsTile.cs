using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PropSpawner
{
    public class SettingsDetailsTile : VisualElement
    {
        public SettingsDetailsTile(PropSpawnerSettings settings)
        {
            Resources.Load<VisualTreeAsset>(UIStrings.settingsDetailsUXMLPath).CloneTree(this);
            styleSheets.Add(Resources.Load<StyleSheet>(UIStrings.settingsDetailsUSSPath));
            AddToClassList(UIStrings.settingsDetailsTileClass);
            var terrainField = this.Q<ObjectField>();
            terrainField.objectType = typeof(Terrain);
            //terrainField.RegisterCallback<ChangeEvent<GameObject>>(e =>
            //{
            //    // only allow scene terrains
            //    if (e.newValue.scene == null ||
            //        !e.newValue.GetComponent<Terrain>())
            //    {
            //        Debug.Log("resetting terrain"); // debugging
            //        settings.Terrain = null;
            //    }
            //});
            this.Q<Toggle>(className: UIStrings.isRefreshingEditorToggleClassName).RegisterCallback<ChangeEvent<bool>>(e =>
            {
                EditorRefresherManager.Init(e.newValue);
            });
            this.Bind(new SerializedObject(settings));
        }
    }
}