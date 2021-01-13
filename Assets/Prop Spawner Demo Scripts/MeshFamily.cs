using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace PropSpawner
{
    public class MeshFamily
    {
        public MeshObject this[int i]
        {
            get { return meshObjects[i]; }
        }
        public int MeshCount { get { return meshObjects.Count; } }       
        public int LodGroupCount { get; private set; }
        public Bounds Bounds { get; private set; }
        List<List<float>> lodGroupDistances = new List<List<float>>();
        List<MeshObject> meshObjects = new List<MeshObject>();    
        List<(GameObject obj, int groupIndex, int lodIndex)> lodIDs = new List<(GameObject, int, int)>();

        public (float4 lodDistances0, float4 lodDistances1) GetLodDistances(int lodGroupIndex)
        {
            float4 lodDistances0 = float4.zero;
            float4 lodDistances1 = float4.zero;
            if (LodGroupCount > 0)
            {
                var distances = lodGroupDistances[lodGroupIndex];
                for (int i = 0; i < distances.Count; i++)
                {
                    if (i / 4 == 0)
                    {
                        lodDistances0[i % 4] = distances[i];
                    }
                    else
                    {
                        lodDistances1[i % 4] = distances[i];
                    }
                }
            }
            return (lodDistances0, lodDistances1);
        }

        public MeshFamily(GameObject parent)
        {
            if (!parent) { return; }
            GetFamilyData(parent, parent, Matrix4x4.identity);
            CalculateFamilyBoundingBox();
        }

        void GetFamilyData(GameObject obj, GameObject root, Matrix4x4 matrix)
        {            
            // Create LOD ids if this obj has a LODGroup component that contains renderers and then calculate lodDistances
            LODGroup lodGroup = obj.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                List<float> lodDistances = new List<float>();
                int groupIndex = LodGroupCount;
                LOD[] lods = lodGroup.GetLODs();
                int lodIndex = 0;
                for (int i = 0; i < lods.Length; i++)
                {
                    LOD lod = lods[i];
                    if (lod.renderers == null || lod.renderers.Length == 0) continue;
                    for (int j = 0; j < lod.renderers.Length; j++)
                    {
                        lodIDs.Add((lod.renderers[j].gameObject, groupIndex, lodIndex++));
                    }
                    lodDistances.Add(LodDistance(lodGroup.size, lod.screenRelativeTransitionHeight));
                }
                if (lodDistances.Count > 0)
                {
                    lodGroupDistances.Add(lodDistances);
                    LodGroupCount++;
                }
            }
            // calculate matrix that transforms obj into parent's local space          
            matrix = obj == root ?
                Matrix4x4.identity :
                matrix * Matrix4x4.TRS(
                    obj.transform.localPosition,
                    obj.transform.localRotation,
                    obj.transform.localScale);
            // if obj has mesh, material, and only uses tri topology, then add its data to lists
            MeshFilter filter = obj.GetComponent<MeshFilter>();
            MeshRenderer rend = obj.GetComponent<MeshRenderer>();
            if (filter != null &&
                filter.sharedMesh != null &&
                UsesTris(filter.sharedMesh) &&
                rend != null &&
                rend.sharedMaterials != null &&
                rend.sharedMaterials.Length != 0)
            {
                MeshObject meshObj = new MeshObject
                {
                    Mesh = filter.sharedMesh,
                    Materials = rend.sharedMaterials,
                    Matrix = matrix
                };
                // if this obj has already been identified as part of a LODGroup, then add it to that group's list
                var lodId = lodIDs.Find((x) => x.obj == obj);
                if (lodId.obj)
                {
                    meshObj.IsLOD = true;
                    meshObj.LodGroupIndex = lodId.groupIndex;
                    meshObj.LodIndex = lodId.lodIndex;
                }
                meshObjects.Add(meshObj);
            }
            // recursively get data for all object's children
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                GetFamilyData(obj.transform.GetChild(i).gameObject, root, matrix);
            }
        }

        float LodDistance(float groupSize, float screenRelativeTransitionHeight) 
        {
            float lodFOV = screenRelativeTransitionHeight * Camera.main.fieldOfView * Mathf.Deg2Rad;
            return groupSize / 2 / math.tan(lodFOV / 2) * 1.1f; 
            // I don't know why, but without this * 1.1, the resulting distances are shorter than Unity's standard Lod system's   
        }

        bool UsesTris(Mesh mesh)
        {
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if (mesh.GetTopology(i) != MeshTopology.Triangles)
                {
                    return false;
                }
            }
            return true;
        }

        void CalculateFamilyBoundingBox()
        {
            if (MeshCount == 0) { return; }

            // create bounding box to encapsulate all children of model
            float3 initialVertex = meshObjects[0].Mesh.vertices[0];
            Bounds totalBounds = new Bounds(initialVertex, Vector3.zero);
            for (int i = 0; i < MeshCount; i++)
            {
                MeshObject meshObj = meshObjects[i];
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Matrix4x4 matrix = meshObj.Matrix;
                            Bounds childbb = meshObj.Mesh.bounds;
                            Vector3 cornerOffset = new Vector3(
                                childbb.extents.x * x,
                                childbb.extents.y * y,
                                childbb.extents.z * z);
                            Vector3 transformedBbCorner = matrix.MultiplyPoint3x4(childbb.center + cornerOffset);                            
                            totalBounds.Encapsulate(transformedBbCorner);
                        }
                    }
                }
            }
            Bounds = totalBounds;
        }

    }
    public class MeshObject
    {
        public Mesh Mesh { get; set; }
        public Material[] Materials { get; set; }
        public Matrix4x4 Matrix { get; set; }
        public bool IsLOD { get; set; }
        public int LodGroupIndex { get; set; }
        public int LodIndex { get; set; }

    }
}