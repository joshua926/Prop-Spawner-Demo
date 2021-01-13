using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PropSpawner
{
    [InitializeOnLoad]
    public static class EditorRefresherManager
    {
        static string objectName = "Editor Refresher Object To Be Dirtied";

        static EditorRefresherManager()
        {
            var settings = Resources.Load<PropSpawnerSettings>(UIStrings.settingsPath);
            Init(settings ? settings.IsRefreshingEditor : false);
        }

        public static void Init(bool useEditorRefresher)
        {
            if (useEditorRefresher)
            {
                EditorSceneManager.sceneOpened += (scene, mode) => { CreateEditorRefresher(); };
                CreateEditorRefresher();
            }
            else
            {
                EditorSceneManager.sceneOpened += (scene, mode) => { DeleteEditorRefresher(); };
                DeleteEditorRefresher();
            }
        }

        static void CreateEditorRefresher()
        {
            var editorRefresher = GameObject.FindObjectOfType<EditorRefresher>();
            if (editorRefresher == null)
            {
                GameObject objectToDirty = new GameObject();
                objectToDirty.tag = "EditorOnly";
                objectToDirty.name = objectName;
                objectToDirty.AddComponent<EditorRefresher>();
            }
        }

        static void DeleteEditorRefresher()
        {
            var editorRefresher = GameObject.FindObjectOfType<EditorRefresher>();
            if (editorRefresher != null)
            {
                GameObject.DestroyImmediate(editorRefresher.gameObject, false);
            }
        }

    }
}
