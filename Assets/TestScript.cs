using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public MeshFilter MeshFilter;

    // Start is called before the first frame update
    void Start()
    {

        // Mesh mesh = MeshFilter.sharedMesh;
        // IntPtr sdf = SdfLib.createExactOctreeSdf(mesh.vertices, mesh.GetIndices(0),
        //                                                 new Vector3(-1.5f, -1.5f, -1.5f),
        //                                                 new Vector3(1.5f, 1.5f, 1.5f),
        //                                                 3, 7, 64);

        // Debug.Log("Distance: " + SdfLib.getDistance(sdf, new Vector3(0.0f, 0.0f, 0.0f)));
        // Debug.Log("Distance: " + SdfLib.getDistance(sdf, new Vector3(1.0f, 0.0f, 0.0f)));
        // Debug.Log("Distance: " + SdfLib.getDistance(sdf, new Vector3(2.0f, 0.0f, 0.0f)));

        // SdfLib.deleteSdf(sdf);
    }
}