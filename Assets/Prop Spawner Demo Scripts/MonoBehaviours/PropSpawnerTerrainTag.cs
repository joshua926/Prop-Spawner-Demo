using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;

namespace PropSpawner
{
    [ExecuteAlways]
    public class PropSpawnerTerrainTag : MonoBehaviour
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif
    {
#if UNITY_EDITOR
        [SerializeField, HideInInspector] byte[] guidByteArray = null;
        public Guid Guid { get; private set; } = Guid.Empty;

        /// <summary>
        /// Returns the terrain with target Guid if found, else a terrain with a PropSpawnerTerrainTag, 
        /// else a terrain, else null;
        /// </summary>
        public static Terrain FindTerrain(Guid targetGuid)
        {
            Terrain activeTerrain = null;
            PropSpawnerTerrainTag activeTag = null;
            var terrains = FindObjectsOfType<Terrain>();
            foreach (var terrain in terrains)
            {
                activeTerrain = terrain;
                var terrainTag = terrain.GetComponent<PropSpawnerTerrainTag>();
                if (terrainTag)
                {
                    activeTag = terrainTag;
                    if (activeTag.Guid == targetGuid)
                    {
                        break;
                    }
                }
            }
            if (activeTag)
            {
                activeTerrain = activeTag.GetComponent<Terrain>();
            }
            if (activeTerrain && !activeTag)
            {
                activeTerrain.gameObject.AddComponent<PropSpawnerTerrainTag>();
            }            
            return activeTerrain;
        }

        public void OnEnable()
        {
            if (GetComponent<Terrain>() == null)
            {
                return;
            }
            if (Guid == Guid.Empty)
            {
                Guid = Guid.NewGuid();
            }
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }

        public void OnBeforeSerialize()
        {
            guidByteArray = Guid.ToByteArray();
            if (gameObject.scene.name == null)
            {
                Guid = Guid.Empty;
                guidByteArray = new byte[16];
            }
        }

        public void OnAfterDeserialize()
        {
            Guid = new Guid(guidByteArray);
        }
#endif
    }
}