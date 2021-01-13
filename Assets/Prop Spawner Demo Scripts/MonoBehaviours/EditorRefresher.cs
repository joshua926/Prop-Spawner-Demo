using UnityEngine;
using System.Threading.Tasks;

namespace PropSpawner
{
    [ExecuteAlways]
    public class EditorRefresher : MonoBehaviour
    {
#if UNITY_EDITOR
        //Moving position like this is a silly hack to get the in-editor scene view to refresh so that mesh instances will be culled in real time. This should have no effect in builds.
        public float yPositionOffset = .001f;
        public int refreshDelayMs = 500;

        private void OnEnable()
        {
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            StartTimedEditorRefreshes();
        }

        async void StartTimedEditorRefreshes()
        {
            while (!Application.isPlaying && this != null && enabled)
            {
                transform.position += new Vector3(0, yPositionOffset, 0);
                yPositionOffset *= -1;
                await Task.Delay(refreshDelayMs);
            }
        }
#endif
    } 
}


