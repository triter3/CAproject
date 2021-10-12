using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleColliderComponent : MonoBehaviour
{
    public Transform P1;
    public Transform P2;
    public Transform P3;

    void Awake() 
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.vertices = new Vector3[] {P1.position, P2.position, P3.position};
        mesh.triangles = new int[] {0, 1, 2};
        Vector3 normal = Vector3.Cross(P2.position - P1.position, P3.position - P1.position).normalized;
        mesh.normals = new Vector3[] {normal, normal, normal};
    }

    private float GetSin(Vector3 a, Vector3 b)
    {
        float cos = Vector3.Dot(a.normalized, b.normalized);
        return Mathf.Sqrt(1.0f - cos*cos);
    }

    public Colliders.Triangle GetCollider()
    {
        Colliders.Triangle t = new Colliders.Triangle();
        t.A = P1.position;
        t.V = P2.position - t.A;
        t.W = P3.position - t.A;
        t.N = Vector3.Cross(t.V, t.W);
        t.Nlength = t.N.magnitude;
        t.Normal = t.N.normalized;
        t.D = -Vector3.Dot(t.Normal, t.A);

        t.minUOffset = 0.0f;
        t.minVOffset = 0.0f;
        t.uOffset = 1.0f;
        t.vOffset = 1.0f;
        t.uvOffset = t.uOffset * t.vOffset;

        // float sin = GetSin(t.V.normalized, t.W.normalized);
        // t.minUOffset = 0.1f / (t.V.magnitude * sin);
        // t.minVOffset = 0.1f / (t.W.magnitude * sin);
        // Vector3 BC = P3.position - P2.position;
        // t.uOffset = 1.0f + 0.1f / (t.V.magnitude * GetSin(BC, t.V));
        // t.vOffset = 1.0f + 0.1f / (t.W.magnitude * GetSin(-BC, t.W));
        // t.uvOffset = t.uOffset * t.vOffset;
        
        return t;
    }
}
