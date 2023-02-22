using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
public class SdfMeshColliderComponent : MonoBehaviour
{
    public bool LoadCacheFile = false;
    public bool SaveWhenBuild = false;
    public string Path = "";
    public bool IsNormalized = true;

    private bool SdfFunctionLoaded = false;
    private SdfFunction SdfFunction;

    private MeshFilter MeshFilter;

    void Awake()
    {
        MeshFilter = GetComponent<MeshFilter>();
    }

    void OnDisable()
    {
        if(SdfFunctionLoaded)
        {
            SdfFunction.Dispose();
            SdfFunctionLoaded = false;
        }
    }

    public SdfFunction GetSdf()
    {
        if(!SdfFunctionLoaded)
        {
            if(!LoadCacheFile)
            {
                const float offset = 0.8f;
                Vector3 min = MeshFilter.sharedMesh.bounds.min - new Vector3(offset, offset, offset);
                Vector3 max = MeshFilter.sharedMesh.bounds.max + new Vector3(offset, offset, offset);
                SdfFunction = SdfFunction.CreateExactOctreeSdf(MeshFilter.sharedMesh, min, max, 3, 8, 64);
                SdfFunction.SetTransfrom(transform.worldToLocalMatrix, transform.rotation);

                if(SaveWhenBuild)
                {
                    SdfFunction.SaveExactOctreeSdf(Path);
                }
            }
            else
            {
                float scale = 1.0f;
                Vector3 center = Vector3.zero;
                // Debug.Log(MeshFilter.sharedMesh.bounds.min.x + ", " + MeshFilter.sharedMesh.bounds.min.y + ", " + MeshFilter.sharedMesh.bounds.min.z);
                // Debug.Log(MeshFilter.sharedMesh.bounds.max.x + ", " + MeshFilter.sharedMesh.bounds.max.y + ", " + MeshFilter.sharedMesh.bounds.max.z);
                // Debug.Log("Vertices: " + MeshFilter.sharedMesh.vertices.Length);
                // Debug.Log("Indices: " + MeshFilter.sharedMesh.triangles.Length);

                if(IsNormalized)
                {
                    Vector3 size = MeshFilter.sharedMesh.bounds.size;
                    scale = 2.0f / Mathf.Max(size.x, size.y, size.z);

                    center = MeshFilter.sharedMesh.bounds.center;
                }
                SdfFunction = SdfFunction.LoadExactOctreeSdf(Path);
                SdfFunction.SetTransfrom(Matrix4x4.Scale(new Vector3(scale, scale, scale)) * Matrix4x4.Translate(-center) * transform.worldToLocalMatrix, transform.rotation);
            }

            SdfFunctionLoaded = true;
        }

        return SdfFunction;
    }
}
