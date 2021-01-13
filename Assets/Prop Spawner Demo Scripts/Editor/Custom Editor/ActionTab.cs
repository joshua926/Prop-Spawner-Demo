using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PropSpawner
{
    public class ActionTab : VisualElement
    {
        PropSpawnerSettings settings;
        ObjectField terrainField;
        public Terrain Terrain => terrainField.value as Terrain;

        public ActionTab(PropSpawnerSettings settings, Action spawnButtonClick, Action settingsButtonClick)
        {
            this.settings = settings;
            Resources.Load<VisualTreeAsset>(UIStrings.actionTabUXMLPath).CloneTree(this);
            styleSheets.Add(Resources.Load<StyleSheet>(UIStrings.actionTabUSSPath));
            name = UIStrings.actionTabName;
            AddToClassList(UIStrings.rowContainerClass);
        
            var spawnButton = this.Q<Button>(name: UIStrings.spawnButtonName);
            //spawnButton.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>(UIStrings.imagesFolder + "/lock-icon"));
            spawnButton.clicked += spawnButtonClick;

            var settingsButton = this.Q<Button>(name: UIStrings.settingsButtonName);
            settingsButton.clicked += settingsButtonClick;

            FindTerrain();
            EditorApplication.hierarchyChanged += FindTerrain;
            RegisterTerrainFieldChangeCallback();
        }

        void FindTerrain()
        {
            terrainField = this.Q<ObjectField>();
            terrainField.objectType = typeof(Terrain);
            terrainField.value = null; // this allows the field text to update in case of name change
            Guid savedTagGuid = settings.TerrainGuid;
            Terrain activeTerrain = PropSpawnerTerrainTag.FindTerrain(savedTagGuid);
            if (activeTerrain)
            {
                var activeTag = activeTerrain.GetComponent<PropSpawnerTerrainTag>();
                terrainField.value = activeTerrain;
                settings.TerrainGuid = activeTag.Guid;
            }
            else
            {
                terrainField.value = null;
            }            
        }

        void RegisterTerrainFieldChangeCallback()
        {
            terrainField = this.Q<ObjectField>();
            terrainField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(e =>
            {
                Terrain terrain = e.newValue as Terrain;
                if (terrain == null) { return; }
                if (terrain.gameObject.scene.name == null) 
                {
                    terrainField.value = null;
                    return; 
                }
                var tag = terrain.GetComponent<PropSpawnerTerrainTag>();
                if (tag == null)
                {
                    tag = terrain.gameObject.AddComponent<PropSpawnerTerrainTag>();
                }
                settings.TerrainGuid = tag.Guid;
            });
        }

        public void CleanUp()
        {
            EditorApplication.hierarchyChanged -= FindTerrain;
        }
    }
}