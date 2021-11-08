using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public class MeshColliderComponent : MonoBehaviour
{
    private Mesh Mesh;

    void Awake() 
    {
        Mesh = GetComponent<MeshFilter>().mesh;
    }

    private float GetSin(Vector3 a, Vector3 b)
    {
        float cos = Vector3.Dot(a.normalized, b.normalized);
        return Mathf.Sqrt(1.0f - cos*cos);
    }

    public void AddColliders(NativeArray<Colliders.Triangle> triangles, int startIndex, float ballRadius)
    {
        for(int i=0; i < Mesh.triangles.Length; i+=3)
        {
            Colliders.Triangle t = new Colliders.Triangle();
            Vector3 B = transform.TransformPoint(Mesh.vertices[Mesh.triangles[i+1]]);
            Vector3 C = transform.TransformPoint(Mesh.vertices[Mesh.triangles[i+2]]);
            t.A = transform.TransformPoint(Mesh.vertices[Mesh.triangles[i]]);
            t.V = B - t.A;
            t.W = C - t.A;
            t.N = Vector3.Cross(t.V, t.W);
            t.Nlength = t.N.magnitude;
            t.Normal = t.N.normalized;
            t.D = -Vector3.Dot(t.Normal, t.A);

            // float sin = GetSin(t.V.normalized, t.W.normalized);
            // t.minUOffset = 0.1f / (t.V.magnitude * sin);
            // t.minVOffset = 0.1f / (t.W.magnitude * sin);
            // Vector3 BC = C - B;
            // t.uOffset = 1.0f + 0.1f / (t.V.magnitude * GetSin(BC, t.V));
            // t.vOffset = 1.0f + 0.1f / (t.W.magnitude * GetSin(-BC, t.W));
            // t.uvOffset = t.uOffset * t.vOffset;
            t.minUOffset = 0.0f;
            t.minVOffset = 0.0f;
            t.uOffset = 1.0f;
            t.vOffset = 1.0f;
            t.uvOffset = t.uOffset * t.vOffset;

            triangles[startIndex++] = t;
        }
    }

    public int NumOfColliders()
    {
        Mesh = GetComponent<MeshFilter>().mesh;
        return Mesh.triangles.Length/3;
    }
}
