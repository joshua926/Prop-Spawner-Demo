using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace PropSpawner
{
    public partial class Rules : TileableAsset
    {
        public bool IsEnabled => isEnabled;
        [SerializeField] bool isEnabled = true;

        public GameObject Prefab => prefab;
        [SerializeField] GameObject prefab = null;

        public Radii Radii => new Radii { Base = baseRadius, sameModel = sameModelRadius };
        [SerializeField] float baseRadius = 1;
        [SerializeField] float sameModelRadius = 1;

        public RotSclRange RotSclRange => new RotSclRange
        {
            rMin = rotationMin,
            rMax = rotationMax,
            sMin = scaleMin,
            sMax = scaleMax
        };
        [SerializeField] Vector3 rotationMin = Vector3.zero;
        [SerializeField] Vector3 rotationMax = new Vector3(0, 360, 0);
        [SerializeField] Vector3 scaleMin = Vector3.one;
        [SerializeField] Vector3 scaleMax = Vector3.one;

        public bool UseECSRendering => useECSRendering;       
        [Space]        
        [SerializeField] bool useECSRendering = true;
        public bool KeepGameObjects => keepGameObjects;
        [SerializeField] bool keepGameObjects = false;

        public float2 ZoneSize => zoneSize;
        [Tooltip("Side length of the square zone of instances that will be rendered by " +
            "each instancer GameObject for this rule set.")]
        [SerializeField] Vector2 zoneSize = new Vector2(250, 250);

        [SerializeField] Texture2D icon = null;
        Texture2D cachedPreview = null;

        [SerializeField] bool isSelected = false;
        public override bool IsSelected
        {
            get => isSelected;
            set
            {
                var so = new SerializedObject(this);
                var sp = so.FindProperty(nameof(isSelected));
                sp.boolValue = value;
                so.ApplyModifiedProperties();
            }
        }

        public int CellCountPerJob { get; private set; }
        public float CellWidth { get; private set; }
        public void CalculateCellDetails(float jobWidth)
        {
            CellWidth = sameModelRadius * 2;
            CellCountPerJob = (int)(jobWidth / CellWidth);
            CellCountPerJob = math.max(1, CellCountPerJob);
            CellWidth = jobWidth / CellCountPerJob;
        }

        public void Init(GameObject prefab)
        {
            this.prefab = prefab;
        } 

        public void OnValidate()
        {
            baseRadius = math.max(.05f, baseRadius);
            sameModelRadius = math.max(baseRadius, sameModelRadius);
            rotationMax = math.max(rotationMin, rotationMax);
            scaleMax = math.max(scaleMin, scaleMax);
            zoneSize = math.max(Vector2.one, zoneSize);
            if (!useECSRendering)
            {
                keepGameObjects = true;
            }
        }

        public async override void GetAssetIcon(System.Action<Texture2D> action)
        {
            if (icon != null)
            {
                action(icon);
                return;
            }
            if (cachedPreview != null)
            {
                action(cachedPreview);
                return;
            }
            Texture2D preview = null;
            MeshFilter filter = prefab.GetComponentInChildren<MeshFilter>();
            if (prefab != null && filter != null && filter.sharedMesh != null)
            {
                int msLimit = 2000;
                int msElapsed = 0;
                int msInterval = 100;
                while (preview == null && msElapsed < msLimit)
                {
                    preview = AssetPreview.GetAssetPreview(filter.gameObject);
                    msElapsed += msInterval;
                    await Task.Delay(msInterval);
                }
                if (preview != null) 
                {
                    cachedPreview = preview;
                    action(preview);
                    return; 
                }
            }
            action(AssetPreview.GetMiniTypeThumbnail(typeof(GameObject)));
        }
    }
}