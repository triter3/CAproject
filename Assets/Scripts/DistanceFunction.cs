using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class DistanceFunction : MonoBehaviour
{
    static float3[] g = new float3[]  { new float3(1,1,0), new float3(-1,1,0), new float3(1,-1,0), new float3(-1,-1,0),   
                                        new float3(1,0,1), new float3(-1,0,1), new float3(1,0,-1), new float3(-1,0,-1), 
                                        new float3(0,1,1), new float3(0,-1,1), new float3(0,1,-1), new float3(0,-1,-1),
                                        new float3(1,1,0), new float3(0,-1,1), new float3(-1,1,0),  new float3(0,-1,-1) };


    static int randomValue(int3 pos, int seed)
    {
        int h = seed + pos.x*374761393 + pos.y*668265263 + pos.z*568205581; //all constants are prime
        h = (h^(h >> 13))*1274126177;
        return h^(h >> 16);
    }

    static int randomValue(int pos, int seed)
    {
        int h = seed + pos*374761393; //all constants are prime
        h = (h^(h >> 13))*1274126177;
        return h^(h >> 16);
    }

    static float3 fade(float3 t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    static float gradperm(int3 P, float3 p, int seed)
    {
        return math.dot(g[randomValue(P, seed) & 0x0F], p);
    }

    static float opSmoothIntersection( float d1, float d2, float k ) 
    {
        float h = math.clamp( 0.5f - 0.5f*(d2-d1)/k, 0.0f, 1.0f );
        return math.lerp( d2, d1, h ) + k*h*(1.0f-h); 
    }

    static float inoise(float3 p, int seed)
    {
        int3 P = new int3((int)math.floor(p.x), 
                          (int)math.floor(p.y), 
                          (int)math.floor(p.z));      		// FIND UNIT CUBE THAT CONT AINS POINT
        p -= math.floor(p);                                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
        float3 f = fade(p);                                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z.
        
        // HASH COORDINATES OF THE 8 CUBE CORNERS                            
        return math.lerp( math.lerp( math.lerp( gradperm(P, p, seed ),  
                                gradperm(P + new int3(1, 0, 0), p + new float3(-1, 0, 0), seed ), f.x),
                          math.lerp( gradperm(P + new int3(0, 1, 0), p + new float3(0, -1, 0), seed ),
                                gradperm(P + new int3(1, 1, 0), p + new float3(-1, -1, 0), seed ), f.x), f.y),
                                    
                          math.lerp( math.lerp( gradperm(P + new int3(0, 0, 1), p + new float3(0, 0, -1), seed ),
                                gradperm(P + new int3(1, 0, 1), p + new float3(-1, 0, -1), seed ), f.x),
                          math.lerp( gradperm(P + new int3(0, 1, 1), p + new float3(0, -1, -1), seed ),
                                gradperm(P + new int3(1, 1, 1), p + new float3(-1, -1, -1), seed ), f.x), f.y), f.z);
    }

    public static float Evaluate(float3 p)
    {
        //return opSmoothIntersection(inoise(p, 22222), math.dot(p, p) - 20.0f, 0.1f);
        return math.max(inoise(p*0.75f, 22222), math.dot(p, p) - 22.0f);
        //return math.dot(p, p) - 20.0f;
    }

    void Awake() 
    {  
        MarchingCubes m = new MarchingCubes();
        m.CreateData(new Vector3(-5.0f, -5.0f, -5.0f), new Vector3(5.0f, 5.0f, 5.0f), 10.0f/164.0f);
        m.MakeIt();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = m.vertices.ToArray();
		mesh.SetIndices(m.faces.ToArray(), MeshTopology.Triangles,0);
		mesh.RecalculateNormals();
        mesh.RecalculateBounds();
		GetComponent<MeshFilter> ().mesh = mesh;

        Debug.Log("Num Vertices: " + mesh.vertices.Length);
        Debug.Log("Num Triangles: " + mesh.triangles.Length);

        // int3 gridSize = new int3(64, 64, 64);
        // float[] dValues = new float[gridSize.x*gridSize.y*gridSize.z];
        // float dx = 6.0f/gridSize.x;
        // float3 org = new float3(-3.0f, -3.0f, -3.0f);

        // int id = 0;
        // for (int i = 0; i < gridSize.x; i++) {
		// 	float x = i * dx;
		// 	for (int j = 0; j < gridSize.y; j++) {
		// 		float y = j * dx;
		// 		for (int k = 0; k < gridSize.z; k++) {
		// 			float z = k * dx;
		// 			dValues[id++] = Evaluate(new float3(x, y, z) + org);
		// 		}
		// 	}
		// }

        // Nezix.MarchingCubesBurst mcb = new Nezix.MarchingCubesBurst(dValues, gridSize, org, dx);

        // mcb.computeIsoSurface(0.0f);

        // Vector3[] newVerts = mcb.getVertices();
		// Vector3[] newNorms = mcb.getNormals();

		// for (int i = 0; i < newVerts.Length; i++) {
		// 	newNorms[i] *= -1;
		// }

        // int[] newTri = mcb.getTriangles();

        // Debug.Log("Num Vertices: " + newVerts.Length);
        // Debug.Log("Num Triangles: " + newTri.Length);

        // Mesh mesh = new Mesh();
        // mesh.vertices = newVerts;
        // mesh.triangles = newTri;
        // mesh.normals = newNorms;
        // GetComponent<MeshFilter>().mesh = mesh;
        // mesh.RecalculateBounds();
        // //mesh.RecalculateNormals();

        // mcb.Clean();
    }
}
