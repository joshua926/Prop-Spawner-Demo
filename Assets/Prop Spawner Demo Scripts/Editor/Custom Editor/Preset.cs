using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PropSpawner
{
    [Serializable]
    public class PresetCollection
    {
        [SerializeField] List<Preset> presets;



        [Serializable]
        class Preset
        {
            [SerializeField] List<TileableAsset> assets;
            [SerializeField] List<bool> enabledStates;
            Dictionary<TileableAsset, bool> stateDictionary;

            public void Init(TileableAsset[] projectAssets)
            {
                if (assets == null) 
                {
                    assets = new List<TileableAsset>(); 
                }
                if (enabledStates == null) 
                { 
                    enabledStates = new List<bool>(); 
                }
                if (stateDictionary == null)
                {
                    stateDictionary = new Dictionary<TileableAsset, bool>();
                    for (int i = 0; i < assets.Count; i++)
                    {
                        if (!stateDictionary.ContainsKey(assets[i]))
                        {
                            stateDictionary.Add(assets[i], enabledStates[i]);
                        }
                    }
                }
                foreach (var projectAsset in projectAssets)
                {
                    if (!stateDictionary.ContainsKey(projectAsset))
                    {
                        assets.Add(projectAsset);
                        enabledStates.Add(true);
                        stateDictionary.Add(projectAsset, true);
                    }
                }
            }
        }
        [Serializable]
        public class AssetState
        {
            public TileableAsset asset;
            public bool state;
        }
    }
}