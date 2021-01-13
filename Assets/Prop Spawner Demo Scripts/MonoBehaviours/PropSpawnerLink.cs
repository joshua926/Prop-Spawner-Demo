using System.Collections.Generic;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PropSpawner
{
    [ExecuteAlways]
    public class PropSpawnerLink : MonoBehaviour
    {
#if UNITY_EDITOR
        static Dictionary<GameObject, List<PropSpawnerLink>> listDictionary;
        public static float startTime = 0; // debugging
        public static bool printedTime = false;
        public bool isLocked = false;
        [SerializeField] GameObject prefab;
        GameObject oldPrefab;
        int propIndex;
        public GameObject Prefab => prefab;

        public static List<GameObject> GetPrefabs()
        {
            CreateDictionary();
            return new List<GameObject>(listDictionary.Keys);
        }

        public static IList<PropSpawnerLink> GetLinks(GameObject prefab)
        {
            CreateDictionary();
            return listDictionary[prefab].AsReadOnly();
        }

        static void CreateDictionary()
        {
            if (listDictionary == null)
            {
                listDictionary = new Dictionary<GameObject, List<PropSpawnerLink>>();
            }
        }

        private void OnEnable()
        {
            if (prefab == null)
            {
                prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            }
            AddToDictionary(prefab);
        }

        private void OnDisable()
        {
            RemoveFromDictionary(prefab);
        }

        private void OnValidate()
        {
            if (prefab != oldPrefab)
            {
                RemoveFromDictionary(oldPrefab);
                oldPrefab = prefab;
                AddToDictionary(prefab);
            }
            if (GetComponent<ECSMeshInstancer>())
            {
                isLocked = false;
            }
        }

        public void Init(GameObject prefab)
        {
            this.prefab = prefab;
            this.oldPrefab = prefab;
            AddToDictionary(prefab);
        }

        void AddToDictionary(GameObject prefabValue)
        {
            if (prefabValue == null) { return; }
            CreateDictionary();
            if (!listDictionary.ContainsKey(prefabValue))
            {
                listDictionary.Add(prefabValue, new List<PropSpawnerLink>());
            }
            propIndex = listDictionary[prefabValue].Count;
            listDictionary[prefabValue].Add(this);
        }

        void RemoveFromDictionary(GameObject prefabValue)
        {
            if (prefabValue == null) { return; }
            if (listDictionary == null) { return; }
            if (listDictionary.ContainsKey(prefabValue))
            {
                var list = listDictionary[prefabValue];
                if (list[propIndex] == this)
                {
                    list[propIndex] = null;
                }
                else
                {
                    list[list.IndexOf(this)] = null;
                }
                if (list.Count == 0)
                {
                    listDictionary.Remove(prefabValue);
                }
            }
        }

        [CustomEditor(typeof(PropSpawnerLink)), CanEditMultipleObjects]
        public class PropSpawnerLink_Editor : Editor { }
#endif
    }
}