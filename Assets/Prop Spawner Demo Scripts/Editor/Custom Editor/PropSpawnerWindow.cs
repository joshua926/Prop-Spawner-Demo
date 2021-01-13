using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PropSpawner
{
    public partial class PropSpawnerWindow : EditorWindow
    {
        TileableAsset[] assets;
        PropSpawnerSettings settings;

        VisualElement subRoot;
        ActionTab actionTab;
        VisualElement panelContainer;
        AssetPanel assetPanel;
        Divider divider;
        DetailsPanel detailsPanel;
        public IList<TileableAsset> SelectedAssets => assetPanel.SelectedAssets;

        public static readonly float assetPanelMinWidth = 100;
        public static readonly float dividerWidth = 8;
        public static readonly float detailsPanelMinWidth = 175;
        public static readonly float windowMinHeight = 150;
        public static readonly Vector2 iconWidthRange = new Vector2(50, 250);


        [MenuItem("Window/Prop Spawner &P")]
        public static void ShowWindow()
        {
            var window = GetWindow<PropSpawnerWindow>("Prop Spawner");
            window.minSize = new Vector2(
                assetPanelMinWidth + dividerWidth + detailsPanelMinWidth,
                windowMinHeight);
        }

        void OnEnable()
        {
            DrawWindow();
        }

        private void OnDisable()
        {
            CleanUp();
        }

        void OnProjectChange()
        {
            DrawWindow();
        }

        void DrawWindow()
        {
            CleanUp();
            rootVisualElement.Clear();

            assets = Resources.LoadAll<Rules>(UIStrings.rulesAssetsFolder);
            settings = FileUtility.GetOrCreateAsset<PropSpawnerSettings>(UIStrings.settingsPath, this);
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>(UIStrings.mainUSSPath));

            subRoot = new VisualElement() { name = UIStrings.subRootName };
            rootVisualElement.Add(subRoot);

            actionTab = new ActionTab(
                settings,
                StartSpawnJob,
                () => { detailsPanel.ShowSettings(settings); });
            subRoot.Add(actionTab);

            panelContainer = new VisualElement() { name = UIStrings.panelContainerName };
            panelContainer.AddToClassList(UIStrings.rowContainerClass);
            subRoot.Add(panelContainer);

            assetPanel = new AssetPanel(this, assets, settings);
            panelContainer.Add(assetPanel);

            divider = new Divider(this, settings);
            panelContainer.Add(divider);

            detailsPanel = new DetailsPanel(this);
            panelContainer.Add(detailsPanel);

            SetUpSubscriptions();
        }

        void StartSpawnJob()
        {
            var rulesList = new List<Rules>();
            foreach (var asset in assets)
            {
                if (asset is Rules)
                {
                    rulesList.Add(asset as Rules);
                }
            }
            var spawnJobManager = new SpawnJobManager(settings.Samples, actionTab.Terrain, rulesList);
            spawnJobManager.BeginSpawningProcess();
        }

        void SetUpSubscriptions()
        {
            assetPanel.OnDrop += (System.Object sender, EventArgs e) =>
            {
                DrawWindow();
            };
            divider.OnResize += (System.Object sender, float newAssetPanelWidth) =>
            {
                settings.AssetPanelWidth = newAssetPanelWidth;
                assetPanel.style.width = newAssetPanelWidth;
            };
            assetPanel.OnSelection += (System.Object sender, IList<TileableAsset> selection) =>
            {
                detailsPanel.ShowSelection();
            };
        }

        void CleanUp()
        {
            if (actionTab != null)
            {
                actionTab.CleanUp();
            }
        }
    }
}
