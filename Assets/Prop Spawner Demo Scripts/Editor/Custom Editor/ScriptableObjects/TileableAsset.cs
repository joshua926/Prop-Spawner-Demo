using UnityEngine;

namespace PropSpawner
{
    public abstract class TileableAsset : ScriptableObject
    {
        public abstract void GetAssetIcon(System.Action<Texture2D> action);

        public AssetTile Tile { get; set; }

        public abstract bool IsSelected { get; set; }

    }
}