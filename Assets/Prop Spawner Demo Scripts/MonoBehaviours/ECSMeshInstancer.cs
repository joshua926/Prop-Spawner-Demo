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
    public class ECSMeshInstancer : MonoBehaviour
    {
        static int nextAvailableID;
        [SerializeField] GameObject prefab = null;
        [SerializeField] ShadowCastingMode shadowCastingMode = ShadowCastingMode.On;
        [SerializeField] bool receiveShadows = true; 
        [SerializeField] int layer = 0;
        [SerializeField, HideInInspector] float3[] translations = null;
        [SerializeField, HideInInspector] quaternion[] rotations = null;
        [SerializeField, HideInInspector] float3[] scales = null;
        [SerializeField, HideInInspector] int id = 0;
        [SerializeField, HideInInspector] bool initialized = false;
        Allocator alloc = Allocator.TempJob;
        EntityManager manager;
        bool entitiesCreated;
        struct MeshInstancerIndex : ISharedComponentData { public int Value; }

        public int InstanceCount => translations.Length;

        public (float3 position, quaternion rotation, float3 scale) this[int i]
        {
            get => (translations[i], rotations[i], scales[i]);
        }

        void OnEnable()
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

        void OnDisable()
        {
            DisposeOfEntities();
        }

        public void Init(
            GameObject prefab,
            float3[] translations,
            quaternion[] rotations,
            float3[] scales)
        {
            this.prefab = prefab;
            this.translations = translations;
            this.rotations = rotations;
            this.scales = scales;
            initialized = true;
            id = nextAvailableID++;
            DisposeOfEntities();
            CreateEntities();
        }

        public void DisposeOfEntities()
        {
            if (World.DefaultGameObjectInjectionWorld == null || !entitiesCreated) { return; }
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = manager.CreateEntityQuery(typeof(MeshInstancerIndex));
            query.SetSharedComponentFilter(new MeshInstancerIndex { Value = id });
            manager.DestroyEntity(query);
            entitiesCreated = false;
        }

        public void CreateEntities()
        {
            if (!initialized || entitiesCreated) { return; }
            manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            ComponentType[] entityComponents = new ComponentType[]
            {
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(PerInstanceCullingTag),
            };
            ComponentType[] lodGroupComponents = new ComponentType[]
            {
                typeof(MeshLODGroupComponent),
                typeof(LocalToWorld),
                typeof(Translation),
            };
            ComponentType[] lodComponents = new ComponentType[]
            {
                typeof(MeshLODComponent),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(PerInstanceCullingTag),
            };
            EntityArchetype entityArchetype = manager.CreateArchetype(entityComponents);
            EntityArchetype lodGroupArchetype = manager.CreateArchetype(lodGroupComponents);
            EntityArchetype lodArchetype = manager.CreateArchetype(lodComponents);
            MeshFamily meshFamily = new MeshFamily(prefab);
            int lodGroupCount = meshFamily.LodGroupCount;
            int meshCount = meshFamily.MeshCount;
            int instanceCount = translations.Length;

            //Create LODGroup entities if any
            NativeArray<Entity>[] lodGroupEntityArrays = new NativeArray<Entity>[lodGroupCount];
            for (int g = 0; g < lodGroupCount; g++)
            {
                var lodGroupEntities = new NativeArray<Entity>(instanceCount, Allocator.Temp);
                manager.CreateEntity(lodGroupArchetype, lodGroupEntities);
                var lodGroupQuery = manager.CreateEntityQuery(lodGroupComponents);
                manager.AddSharedComponentData(lodGroupQuery, new MeshInstancerIndex { Value = id });
                var lodDistances = meshFamily.GetLodDistances(g);
                for (int i = 0; i < instanceCount; i++)
                {
                    Entity lodGroupEntity = lodGroupEntities[i];
                    manager.SetComponentData(lodGroupEntity, new MeshLODGroupComponent
                    {
                        LODDistances0 = lodDistances.lodDistances0,
                        LODDistances1 = lodDistances.lodDistances1,
                    });
                    manager.SetComponentData(lodGroupEntity, new Translation
                    {
                        Value = translations[i]
                    });
                }
                lodGroupEntityArrays[g] = lodGroupEntities;
            }

            // Calculate matrices for meshes
            NativeArray<float4x4> matrices = new NativeArray<float4x4>(meshCount * instanceCount, alloc);
            NativeArray<float4x4> meshMatrices = new NativeArray<float4x4>(meshCount, alloc);
            for (int m = 0; m < meshCount; m++)
            {
                meshMatrices[m] = meshFamily[m].Matrix;
            }
            CalcMatrices job = new CalcMatrices
            {
                meshCount = meshCount,
                instanceCount = instanceCount,
                translations = new NativeArray<float3>(translations, alloc),
                rotations = new NativeArray<quaternion>(rotations, alloc),
                scales = new NativeArray<float3>(scales, alloc),
                meshMatrices = meshMatrices,
                matrices = matrices
            };
            job.Schedule().Complete();

            //Create RenderMesh entities and then add MeshLODComponent if needed     
            for (int m = 0; m < meshCount; m++)
            {
                MeshObject meshObj = meshFamily[m];
                Mesh mesh = meshObj.Mesh;
                int subMeshCount = mesh.subMeshCount;
                for (int s = 0; s < subMeshCount; s++)
                {
                    var entities = new NativeArray<Entity>(instanceCount, Allocator.Temp);
                    EntityQuery entityQuery;
                    if (meshObj.IsLOD)
                    {
                        manager.CreateEntity(lodArchetype, entities);
                        entityQuery = manager.CreateEntityQuery(lodComponents);
                    }
                    else
                    {
                        manager.CreateEntity(entityArchetype, entities);
                        entityQuery = manager.CreateEntityQuery(entityComponents);
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
                    manager.AddSharedComponentData(entityQuery, renderMesh);
                    manager.AddSharedComponentData(entityQuery, new MeshInstancerIndex { Value = id });
                    RenderBounds renderBounds = new RenderBounds
                    { Value = new AABB { Center = mesh.bounds.center, Extents = mesh.bounds.extents } };
                    for (int i = 0; i < instanceCount; i++)
                    {
                        Entity entity = entities[i];
                        manager.SetComponentData(entity, renderBounds);
                        manager.SetComponentData(entity, new LocalToWorld
                        {
                            Value = matrices[i + m * instanceCount]
                        });
                        if (meshObj.IsLOD)
                        {
                            MeshLODComponent lodComp = new MeshLODComponent
                            {
                                Group = lodGroupEntityArrays[meshObj.LodGroupIndex][i],
                                LODMask = 1 << meshObj.LodIndex
                            };
                            manager.SetComponentData(entity, lodComp);
                        }
                    }
                    entities.Dispose();
                }
            }
            matrices.Dispose();

            //Dispose of lodGroupEntityArrays
            for (int g = 0; g < lodGroupCount; g++)
            {
                lodGroupEntityArrays[g].Dispose();
            }
            entitiesCreated = true;
        }

        [BurstCompile(CompileSynchronously = true)]
        struct CalcMatrices : IJob
        {
            public int meshCount;
            public int instanceCount;
            [DeallocateOnJobCompletion]
            public NativeArray<float3> translations;
            [DeallocateOnJobCompletion]
            public NativeArray<quaternion> rotations;
            [DeallocateOnJobCompletion]
            public NativeArray<float3> scales;
            [DeallocateOnJobCompletion]
            public NativeArray<float4x4> meshMatrices;
            public NativeArray<float4x4> matrices;
            public void Execute()
            {
                for (int m = 0; m < meshCount; m++)
                {
                    for (int i = 0; i < instanceCount; i++)
                    {
                        float4x4 instanceMatrix = float4x4.TRS(translations[i], rotations[i], scales[i]);
                        matrices[i + m * instanceCount] = math.mul(instanceMatrix, meshMatrices[m]);
                    }
                }
            }
        }


#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        static void CallOnRecompile()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var instancers = FindObjectsOfType<ECSMeshInstancer>();
                for (int i = 0, length = instancers.Length; i < length; i++)
                {
                    instancers[i].CreateEntities();
                }
            }
        }
#endif
    }
}