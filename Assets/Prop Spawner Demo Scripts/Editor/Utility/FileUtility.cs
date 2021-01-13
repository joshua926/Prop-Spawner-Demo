using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PropSpawner
{
    public static class FileUtility
    {        
        public static T GetOrCreateAsset<T>(
            string pathUnderResourcesFolderNoExt,
            PropSpawnerWindow window) where T : ScriptableObject
        {
            T asset = Resources.Load<T>(pathUnderResourcesFolderNoExt);
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance<T>();
                CreateAsset(asset, pathUnderResourcesFolderNoExt, window);
            }
            return asset;
        }

        public static void CreateAsset<T>(
            T asset, 
            string pathUnderResourcesFolderNOExt,
            PropSpawnerWindow window) where T : ScriptableObject
        {
            if(pathUnderResourcesFolderNOExt[0] == '/')
            {
                pathUnderResourcesFolderNOExt = pathUnderResourcesFolderNOExt.Substring(1);
            }
            string path = GetResourcesFolderPath(window) + "/" + pathUnderResourcesFolderNOExt + ".asset";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(asset, uniquePath);
        }

        static string GetResourcesFolderPath(PropSpawnerWindow window)
        {
           
            string windowScriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(window));
            int indexOfLastSlash = windowScriptPath.LastIndexOf('/');
            string windowScriptParentPath = windowScriptPath.Substring(0, indexOfLastSlash);
            FixFolderStructure(windowScriptParentPath);
            return windowScriptParentPath + "/Resources";
        }

        static void FixFolderStructure(string windowScriptParentPath)
        {
            List<string> folderPaths = new List<string>();
            folderPaths.Add(windowScriptParentPath + "/Resources");
            folderPaths.Add(folderPaths[0] + "/PropSpawner Resources");
            folderPaths.Add(folderPaths[1] + "/Rules Assets");
            foreach (var path in folderPaths)
            {
                int indexOfLastSlash = path.LastIndexOf('/');
                string parentPath = path.Substring(0, indexOfLastSlash);
                string folderName = path.Substring(indexOfLastSlash + 1);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder(parentPath, folderName);
                }
            }
        }
    }
}