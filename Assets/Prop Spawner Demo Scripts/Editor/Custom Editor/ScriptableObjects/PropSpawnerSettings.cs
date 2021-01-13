using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace PropSpawner
{
    public class PropSpawnerSettings : ScriptableObject
    {
        public EventHandler<float> OnIconSizeChanged;
        [SerializeField] int samples = 30;
        [SerializeField, Min(1)] float assetPanelWidth = PropSpawnerWindow.assetPanelMinWidth;
        float previousIconSize;
        [SerializeField, Range(0, 1)] float iconSize = .5f;
        [HideInInspector] byte[] terrainGuidByteArray = null;
        [Tooltip("Refresh editor every .5 seconds to ensure that ECS Renderers update in edit mode. Play mode and builds are unaffected by this setting.")]
        [SerializeField] bool isRefreshingEditor = true;

        public bool IsRefreshingEditor => isRefreshingEditor;
        public int Samples
        {
            get => samples;
            set
            {
                var so = new SerializedObject(this);
                var sp = so.FindProperty(nameof(samples));
                sp.intValue = value;
                so.ApplyModifiedProperties();
            }
        }

        public float AssetPanelWidth
        {
            get => assetPanelWidth;
            set
            {
                var so = new SerializedObject(this);
                var sp = so.FindProperty(nameof(assetPanelWidth));
                sp.floatValue = Mathf.Max(1, value);
                so.ApplyModifiedProperties();
            }
        }
        public float IconSize
        {
            get => iconSize;
            set
            {
                var so = new SerializedObject(this);
                var sp = so.FindProperty(nameof(iconSize));
                sp.floatValue = Mathf.Clamp(value, 0, 1);
                so.ApplyModifiedProperties();
            }
        }

        public Guid TerrainGuid
        {
            get
            {
                if (terrainGuidByteArray == null || terrainGuidByteArray.Length == 0)
                {
                    terrainGuidByteArray = new byte[16];
                }
                return new Guid(terrainGuidByteArray);
            }
            set
            {
                Undo.RecordObject(this, "Change Selected Terrain");
                terrainGuidByteArray = value.ToByteArray();
            }
        }

        private void OnValidate()
        {
            CheckForIconSizeChange();
        }

        void CheckForIconSizeChange()
        {
            if (previousIconSize != iconSize)
            {
                OnIconSizeChanged?.Invoke(this, iconSize);
                previousIconSize = iconSize;
            }
        }        

        
    }
}