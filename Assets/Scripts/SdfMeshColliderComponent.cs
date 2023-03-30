using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
public class SdfMeshColliderComponent : MonoBehaviour
{
    public enum SdfTypes
    {
        EXACT_OCTREE_SDF,
        OCTREE_SDF
    };

    public SdfTypes SdfType;
    public string Path = "";
    public bool IsNormalized = true;
    public uint BuildingNumThreads = 1;
    public float BuildingMaxError = 1e-3f;

    private bool SdfFunctionLoaded = false;
    private SdfFunction SdfFunction;

    private MeshFilter MeshFilter;

    void Awake()
    {
        MeshFilter = GetComponent<MeshFilter>();
    }

    void OnDisable()
    {
        if (SdfFunctionLoaded)
        {
            SdfFunction.Dispose();
            SdfFunctionLoaded = false;
        }
    }

    public SdfFunction GetSdf()
    {
        if (!SdfFunctionLoaded)
        {
            if (!System.IO.File.Exists(Path))
            {
                CreateSdf();
            }
            else
            {
                float scale = 1.0f;
                Vector3 center = Vector3.zero;
                Debug.Log(MeshFilter.sharedMesh.bounds.min.x + ", " + MeshFilter.sharedMesh.bounds.min.y + ", " + MeshFilter.sharedMesh.bounds.min.z);
                Debug.Log(MeshFilter.sharedMesh.bounds.max.x + ", " + MeshFilter.sharedMesh.bounds.max.y + ", " + MeshFilter.sharedMesh.bounds.max.z);
                // Debug.Log("Vertices: " + MeshFilter.sharedMesh.vertices.Length);
                // Debug.Log("Indices: " + MeshFilter.sharedMesh.triangles.Length);

                if (IsNormalized)
                {
                    Vector3 size = MeshFilter.sharedMesh.bounds.size;
                    scale = 2.0f / Mathf.Max(size.x, size.y, size.z);

                    center = MeshFilter.sharedMesh.bounds.center;
                }
                SdfFunction = SdfFunction.LoadSdf(Path);
                SdfFunction.SetTransfrom(Matrix4x4.Scale(new Vector3(scale, scale, scale)) * Matrix4x4.Translate(-center) * transform.worldToLocalMatrix, transform.rotation);
            }

            SdfFunctionLoaded = true;
        }

        return SdfFunction;
    }

    public void CreateSdf()
    {
        MeshFilter = GetComponent<MeshFilter>();
        float offset = 0.2f * Mathf.Max(Mathf.Max(MeshFilter.sharedMesh.bounds.size.x, MeshFilter.sharedMesh.bounds.size.y), MeshFilter.sharedMesh.bounds.size.z);
        Vector3 min = MeshFilter.sharedMesh.bounds.min - new Vector3(offset, offset, offset);
        Vector3 max = MeshFilter.sharedMesh.bounds.max + new Vector3(offset, offset, offset);

        Stopwatch timer = new Stopwatch();
        timer.Start();

        switch (SdfType)
        {
            case SdfTypes.EXACT_OCTREE_SDF:
                SdfFunction = SdfFunction.CreateExactOctreeSdf(MeshFilter.sharedMesh, min, max, 3, 8, 64, BuildingNumThreads);
                break;
            case SdfTypes.OCTREE_SDF:
                // SdfFunction = SdfFunction.CreateOctreeSdf(MeshFilter.sharedMesh, min, max, 3, 8, 1e-3f);
                SdfFunction = SdfFunction.CreateOctreeSdf(MeshFilter.sharedMesh, min, max, 3, 8, BuildingMaxError / transform.localScale.x, BuildingNumThreads);
                Debug.Log("MaxError: " + (BuildingMaxError / transform.localScale.x));
                // SdfFunction = SdfFunction.CreateOctreeSdf(MeshFilter.sharedMesh, min, max, 4, 4, 1e-6f);
                break;
        }

        Debug.Log(((float)timer.ElapsedMilliseconds) / 1000.0f);

        SdfFunction.SetTransfrom(transform.worldToLocalMatrix, transform.rotation);

        if (Path != "")
        {
            SdfFunction.SaveSdf(Path);
        }
    }

    public void DeleteCurrentSdf()
    {
        if (SdfFunctionLoaded)
        {
            SdfFunction.Dispose();
            SdfFunctionLoaded = false;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SdfMeshColliderComponent))]
[CanEditMultipleObjects]
public class SdfMeshColliderComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SdfMeshColliderComponent obj = target as SdfMeshColliderComponent;
        DrawDefaultInspector();
        string commentText = (System.IO.File.Exists(obj.Path)) ? "File found)" : "File not found)";
        if (GUILayout.Button("Build Field (" + commentText))
        {
            if (System.IO.File.Exists(obj.Path))
            {
                System.IO.File.Delete(obj.Path);
            }
            obj.CreateSdf();
            obj.DeleteCurrentSdf();
        }
    }
}
#endif
