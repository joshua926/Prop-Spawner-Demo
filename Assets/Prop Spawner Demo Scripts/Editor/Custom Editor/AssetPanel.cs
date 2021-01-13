using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PropSpawner
{
    public class AssetPanel : VisualElement
    {
        public EventHandler OnDrop;
        public EventHandler<IList<TileableAsset>> OnSelection;
        PropSpawnerWindow window;
        PropSpawnerSettings settings;
        ScrollView assetPanelScrollView;
        VisualElement assetContainer;
        AssetTile[] tiles;
        List<TileableAsset> selectedAssets = new List<TileableAsset>();
        public IList<TileableAsset> SelectedAssets => selectedAssets.AsReadOnly();

        public AssetPanel(
            PropSpawnerWindow window,
            TileableAsset[] assets,
            PropSpawnerSettings settings)
        {
            this.window = window;
            this.settings = settings;
            style.width = settings.AssetPanelWidth;
            name = UIStrings.assetPanelName;
            style.backgroundColor = new Color(0, 0, 0, .1f);
            styleSheets.Add(Resources.Load<StyleSheet>(UIStrings.assetPanelUSSPath));

            assetPanelScrollView = new ScrollView() { name = UIStrings.assetPanelScrollViewName };
            assetContainer = new VisualElement() { name = UIStrings.assetContainerName };
            Add(assetPanelScrollView);
            assetPanelScrollView.Add(assetContainer);
            
            RegisterForDragAndDrop();
            CreateAssetTiles(assets);
            RegisterForMouseDownSelection();
            UpdateSelection();
            HighlightSelection();
            ResizeIcons(null, settings.IconSize);
            settings.OnIconSizeChanged += ResizeIcons;
            RegisterForRefreshingIconsOnGeometryChanged();
            AddExtraSpaceForClicking();
            Undo.undoRedoPerformed += UpdateSelection;
            Undo.undoRedoPerformed += HighlightSelection;
        }

        void RegisterForDragAndDrop()
        {
            RegisterCallback<DragUpdatedEvent>(e =>
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (!(obj is GameObject)) { return; }
                }
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            });
            RegisterCallback<DragPerformEvent>(e =>
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject &&
                        (obj as GameObject).scene.name == null)
                    {
                        Rules rules = ScriptableObject.CreateInstance<Rules>();
                        rules.Init(obj as GameObject);
                        FileUtility.CreateAsset(
                            rules,
                            UIStrings.rulesAssetsFolder + "/" + rules.Prefab.name,
                            window);
                    }
                }
                OnDrop?.Invoke(this, EventArgs.Empty);
            });
        }

        void CreateAssetTiles(TileableAsset[] assets)
        {
            tiles = new AssetTile[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                var tile = new AssetTile(assets[i], i);
                assetContainer.Add(tile);
                assets[i].Tile = tile;
                tiles[i] = tile;
            }
        }

        void RegisterForMouseDownSelection()
        {
            RegisterCallback<MouseDownEvent>(e =>
            {
                var selectedTile = (e.target as VisualElement).GetFirstAncestorOfType<AssetTile>();

                bool tileClicked = selectedTile != null;
                bool shift = (EventModifiers.Shift & e.modifiers) > 0;
                bool ctrlCmnd = ((EventModifiers.Control | EventModifiers.Command) & e.modifiers) > 0;
                bool previousSelection = selectedAssets.Count > 0;

                if (!tileClicked || (!shift && !ctrlCmnd))
                {
                    ClearSelection();
                }
                if (tileClicked)
                {
                    int selectedTileIndex = selectedTile.Index;
                    int min = selectedTileIndex;
                    int max = selectedTileIndex;

                    if (shift && !ctrlCmnd && previousSelection)
                    {
                        int lastIndex = selectedAssets[selectedAssets.Count - 1].Tile.Index;
                        min = Mathf.Min(min, lastIndex);
                        max = Mathf.Max(max, lastIndex);
                    }

                    for (int i = min; i <= max; i++)
                    {
                        var asset = tiles[i].Asset;
                        asset.IsSelected = ctrlCmnd ? !asset.IsSelected : true;
                        if (asset.IsSelected)
                        {
                            selectedAssets.Add(asset);
                        }
                        else
                        {
                            selectedAssets.Remove(asset);
                        }
                    }
                }
                HighlightSelection();
                OnSelection?.Invoke(this, SelectedAssets);
            });
        }

        void ClearSelection()
        {
            foreach (var asset in selectedAssets)
            {
                asset.IsSelected = false;
            }
            selectedAssets.Clear();
        }

        void UpdateSelection()
        {
            selectedAssets.Clear();
            foreach (var tile in tiles)
            {
                if (tile.Asset.IsSelected)
                {
                    selectedAssets.Add(tile.Asset);
                }
            }
        }

        void HighlightSelection()
        {
            foreach (var tile in tiles)
            {
                tile.SetSelectionHighlight(tile.Asset.IsSelected);
            }
        }

        void ResizeIcons(System.Object sender, float sizeValue)
        {
            if (sizeValue == 0)
            {
                assetContainer.style.flexDirection = FlexDirection.Column;
                assetContainer.style.flexWrap = Wrap.NoWrap;
                foreach (var tile in tiles)
                {
                    tile.Q(className: UIStrings.assetIconClass).style.display = DisplayStyle.None;
                    tile.style.height = StyleKeyword.Auto;
                    tile.style.width = StyleKeyword.Auto;
                }
                return;
            }
            float size = Mathf.Lerp(PropSpawnerWindow.iconWidthRange.x, PropSpawnerWindow.iconWidthRange.y, sizeValue);
            assetContainer.style.flexDirection = FlexDirection.Row;
            assetContainer.style.flexWrap = Wrap.Wrap;
            foreach (var tile in tiles)
            {
                var asset = tile.Asset;
                Image image = tile.Q<Image>(className: UIStrings.assetIconClass);
                if (image.image == null)
                {
                    image.uv = new Rect(.05f, .05f, .9f, .9f);
                    (asset as Rules).GetAssetIcon((icon) =>
                    {
                        image.image = icon;
                    });
                }
                image.style.display = DisplayStyle.Flex;
                tile.style.width = size;
                tile.style.height = size + 10;
            }
        }

        // this is intended to fix an occasional bug where the icons all become the same icon or blank 
        // when the panels are resized
        void RegisterForRefreshingIconsOnGeometryChanged()
        {
            RegisterCallback<GeometryChangedEvent>(e =>
            {
                foreach (var tile in tiles)
                {
                    tile.Asset.GetAssetIcon((tex) =>
                    {
                        tile.Q<Image>().image = tex;
                    });
                }
            });
        }

        void AddExtraSpaceForClicking()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList(UIStrings.assetPanelSpacer);
            assetContainer.Add(spacer);
        }
    }
}