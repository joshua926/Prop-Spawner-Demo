using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace PropSpawner
{
    public class AssetTile : VisualElement
    {
        float disabledOpacity = .5f;
        public TileableAsset Asset { get; private set; }
        public int Index { get; private set; }
        public AssetTile(TileableAsset asset, int index)
        {
            Asset = asset;
            Index = index;
            AddToClassList(UIStrings.assetTileClass);
            Resources.Load<VisualTreeAsset>(UIStrings.assetTileUXMLPath).CloneTree(this);
            styleSheets.Add(Resources.Load<StyleSheet>(UIStrings.assetTileUSSPath));
            this.Bind(new SerializedObject(asset));
            // Set backgound color here in C# rather than USS because opacity can be set here
            RegisterCallback<MouseEnterEvent>((e) =>
            {
                if (Asset.IsSelected) { return; }
                style.backgroundColor = new Color(.4f, .4f, .4f, .4f);
            });
            RegisterCallback<MouseLeaveEvent>((e) =>
            {
                if (Asset.IsSelected) { return; }
                style.backgroundColor = StyleKeyword.None;
            });
            var enabledToggle = this.Q<Toggle>();
            if (!enabledToggle.value) 
            { 
                style.opacity = disabledOpacity; 
            }
            enabledToggle.RegisterCallback<ChangeEvent<bool>>(e =>
            {
                if (e.newValue)
                {
                    style.opacity = 1;
                }
                else
                {
                    style.opacity = disabledOpacity;
                }
            });
        }

        public void SetSelectionHighlight(bool enable)
        {
            if (enable)
            {
                // Set backgound color here in C# rather than USS because opacity can be set here
                style.backgroundColor = new Color(0.2f, 0.4f, 0.6f, .9f);
                this.Q<Label>().style.color = Color.white;
            }
            else
            {
                style.backgroundColor = StyleKeyword.None;
                if (EditorGUIUtility.isProSkin)
                {
                    this.Q<Label>().style.color = new Color(.8f, .8f, .8f);
                }
                else
                {
                    this.Q<Label>().style.color = Color.black;
                }
                
            }
        }
    }
}