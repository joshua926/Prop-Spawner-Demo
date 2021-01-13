using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Burst;

namespace PropSpawner
{
    [ExecuteAlways]
    public class ECSMeshRenderer : MonoBehaviour
    {
        static Dictionary<GameObject, MeshFamily> familyDictionary;
        [SerializeField] GameObject prefab = null;
        [SerializeField] ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
        [SerializeField] bool receiveShadows = true;
        [SerializeField] int layer = 0;
        [SerializeField, HideInInspector] bool initialized = false;
        EntityManager manager;
        bool entitiesCreated;
        List<Entity> entities;

        public void Init(GameObject prefab)
        {
            this.prefab = prefab;
            initialized = true;
        }

        public void UpdateECSTransform()
        {
            if (!entitiesCreated) { return; }
            Entity parent = entities[0];
            manager.SetComponentData(parent, new Translation
            {
                Value = transform.position
            });
            manager.SetComponentData(parent, new Rotation
            {
                Value = transform.rotation
            });
            manager.SetComponentData(parent, new NonUniformScale
            {
                Value = transform.localScale
            });
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                DefaultWorldInitialization.DefaultLazyEditModeInitialize();
            }
#endif
            if (World.DefaultGameObjectInjectionWorld == null || entitiesCreated) { return; }
            DisposeOfEntities();
            CreateEntities();
        }

        void Start()
        {
            if (entitiesCreated) { return; }
            DisposeOfEntities();
            CreateEntities();
        }

        private void OnDisable()
        {
            DisposeOfEntities();
        }

        void DisposeOfEntities()
        {
            if (World.DefaultGameObjectInjectionWorld == null || !entitiesCreated) { return; }
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entitiesNativeArray = new NativeArray<Entity>(entities.ToArray(), Allocator.Temp);
            manager.DestroyEntity(entitiesNativeArray);
            entitiesNativeArray.Dispose();
            entitiesCreated = false;
        }

        void CreateEntities()
        {
            if (!initialized || entitiesCreated) { return; }
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            ComponentType[] parentComponents = new ComponentType[]
            {
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(Rotation),
                typeof(NonUniformScale)
            };
            ComponentType[] lodGroupComponents = new ComponentType[]
            {
                typeof(MeshLODGroupComponent),
                typeof(LocalToWorld),
                typeof(LocalToParent),
                typeof(Parent)
            };
            ComponentType[] entityComponents = new ComponentType[]
            {
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(PerInstanceCullingTag),
                typeof(LocalToParent),
                typeof(Parent)
            };
            MeshFamily meshFamily = GetMeshFamily();
            int lodGroupCount = meshFamily.LodGroupCount;
            int meshCount = meshFamily.MeshCount;

            entities = new List<Entity>();
            entities.Add(manager.CreateEntity(parentComponents));
            entitiesCreated = true;
            UpdateECSTransform();

            //Create LODGroup entities if any            
            for (int g = 0; g < lodGroupCount; g++)
            {
                var lodGroupEntity = manager.CreateEntity(lodGroupComponents);
                var lodDistances = meshFamily.GetLodDistances(g);
                manager.SetComponentData(lodGroupEntity, new MeshLODGroupComponent
                {
                    LODDistances0 = lodDistances.lodDistances0,
                    LODDistances1 = lodDistances.lodDistances1,
                });                
                manager.SetComponentData(lodGroupEntity, new Parent
                {
                    Value = entities[0]
                });
                manager.SetComponentData(lodGroupEntity, new LocalToParent
                {
                    Value = float4x4.identity
                });
                entities.Add(lodGroupEntity);
            }

            //Create RenderMesh entities and then add MeshLODComponent if needed     
            for (int m = 0; m < meshCount; m++)
            {
                MeshObject meshObj = meshFamily[m];
                Mesh mesh = meshObj.Mesh;
                int subMeshCount = mesh.subMeshCount;
                for (int s = 0; s < subMeshCount; s++)
                {
                    Entity entity = manager.CreateEntity(entityComponents);
                    if (meshObj.IsLOD)
                    {
                        MeshLODComponent lodComp = new MeshLODComponent
                        {
                            Group = entities[meshObj.LodGroupIndex + 1],
                            LODMask = 1 << meshObj.LodIndex
                        };
                        manager.AddComponentData(entity, lodComp);
                    }
                    RenderMesh renderMesh = new RenderMesh
                    {
                        mesh = mesh,
                        subMesh = s,
                        material = meshObj.Materials[s],
                        castShadows = shadowCastingMode,
                        receiveShadows = receiveShadows,
                        layer = layer
                    };
                    manager.AddSharedComponentData(entity, renderMesh);
                    AABB meshAABB = new AABB { Center = mesh.bounds.center, Extents = mesh.bounds.extents };
                    RenderBounds renderBounds = new RenderBounds { Value = meshAABB };
                    manager.SetComponentData(entity, renderBounds);
                    manager.SetComponentData(entity, new LocalToParent
                    {
                        Value = meshObj.Matrix
                    });
                    manager.SetComponentData(entity, new Parent
                    {
                        Value = entities[0]
                    });
                    entities.Add(entity);
                }
            }
        }

        MeshFamily GetMeshFamily()
        {
            if (familyDictionary == null)
            {
                familyDictionary = new Dictionary<GameObject, MeshFamily>();
            }
            if (familyDictionary.ContainsKey(prefab))
            {
                return familyDictionary[prefab];
            }
            else
            {
                MeshFamily family = new MeshFamily(prefab);
                familyDictionary.Add(prefab, family);
                return family;
            }
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        static void CallOnRecompile()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var renderers = FindObjectsOfType<ECSMeshRenderer>();
                for (int i = 0, length = renderers.Length; i < length; i++)
                {
                    renderers[i].CreateEntities();
                }
            }
        }
#endif
    }
}