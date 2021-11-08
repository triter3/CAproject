using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateGrid : MonoBehaviour
{
    public ParticleSimulator Simulator;
    public float GridXLength;
    public float GridYLength;
    public float NumParticles;
    public float Ke;
    public float BlendKe;
    public float Kd;
    public bool DrawMesh;
    public Material MeshMaterial;
    [System.Serializable]
    public struct CustomParticle
    {
        public Vector2Int GridCoord;
        public ParticleComponent Particle;
    }
    public List<CustomParticle> CustomParticles;

    private int[] Grid;
    private Mesh Mesh = null;
    private Vector3[] MeshVertices;
    private int[] MeshTriangles; 

    void Awake()
    {
        float percentatgeX = 2.0f * GridXLength / (GridXLength + GridYLength);
        int AxisParticles = (int)Mathf.Sqrt(NumParticles);
        int gridXSize = (int)(float)(percentatgeX * AxisParticles);
        int gridYSize = 2 * AxisParticles - gridXSize;

        Debug.Log("GridSize: " + gridXSize + ", " + gridYSize);

        for(int i=0; i < CustomParticles.Count; i++)
        {
            CustomParticle cp = CustomParticles[i];
            if(cp.GridCoord.x < 0) cp.GridCoord.x = gridXSize + cp.GridCoord.x;
            if(cp.GridCoord.y < 0) cp.GridCoord.y = gridYSize + cp.GridCoord.y;
            CustomParticles[i] = cp;
        }

        float stepX = GridXLength / gridXSize;
        float stepY = GridYLength / gridYSize;
        float stepXY = Mathf.Sqrt(stepX * stepX + stepY * stepY);

        Grid = new int[gridXSize * gridYSize];
        if(DrawMesh)
        {
            MeshVertices = new Vector3[gridXSize * gridYSize];
            MeshTriangles = new int[6 * (gridXSize - 1) * (gridYSize - 1)];
        }

        for(int j=0; j < gridYSize; j++)
        {
            for(int i=0; i < gridXSize; i++)
            {
                Vector3 pos = transform.TransformPoint(new Vector3(i * stepX, j * stepY, 0.0f));
                if(DrawMesh) MeshVertices[j * gridXSize + i] = pos;
                int pId = GetParticleId(new Vector2Int(i, j), pos);
                Grid[i + j * gridXSize] = pId;
                if(i > 0)
                {
                    Simulator.AddSpring(pId, Grid[i - 1 + j * gridXSize], stepX, Ke, Kd, !DrawMesh);
                }

                if(j > 0)
                {
                    Simulator.AddSpring(pId, Grid[i + (j - 1) * gridXSize], stepY, Ke, Kd, !DrawMesh);
                }

                if(i > 0 && j > 0)
                {
                    Simulator.AddSpring(pId, Grid[(i - 1) + (j - 1) * gridXSize], stepXY, Ke, Kd, !DrawMesh);
                }

                if(i < gridXSize - 1 && j > 0)
                {
                    Simulator.AddSpring(pId, Grid[(i + 1) + (j - 1) * gridXSize], stepXY, Ke, Kd, !DrawMesh);
                }

                if(i > 1)
                {
                    Simulator.AddSpring(pId, Grid[i - 2 + j * gridXSize], 2.0f * stepX, BlendKe, Kd, false);
                }

                if(j > 1)
                {
                    Simulator.AddSpring(pId, Grid[i + (j - 2) * gridXSize], 2.0f * stepY, BlendKe, Kd, false);
                }

                if(DrawMesh)
                {
                    if(i > 0 && j > 0)
                    {
                        int tIndex = 6 * ((j-1) * (gridXSize - 1) + i - 1);
                        MeshTriangles[tIndex] = j * gridXSize + i;
                        MeshTriangles[tIndex + 1] = (j - 1) * gridXSize + i - 1;
                        MeshTriangles[tIndex + 2] = (j - 1) * gridXSize + i;

                        MeshTriangles[3 + tIndex] = j * gridXSize + i;
                        MeshTriangles[3 + tIndex + 1] = j * gridXSize + i - 1;
                        MeshTriangles[3 + tIndex + 2] = (j - 1) * gridXSize + i - 1;
                    }
                }
            }
        }

        if(DrawMesh)
        {
            Mesh = new Mesh();
            Mesh.SetVertices(MeshVertices);
            Mesh.SetTriangles(MeshTriangles, 0);
        }
    }

    private int GetParticleId(Vector2Int gridCoord, Vector3 spectedPosition)
    {
        for(int i=0; i < CustomParticles.Count; i++)
        {
            if(gridCoord == CustomParticles[i].GridCoord)
            {
                CustomParticles[i].Particle.transform.position = spectedPosition;
                return CustomParticles[i].Particle.GetId();
            }
        }

        return Simulator.AddParticle(spectedPosition, 1.0f, true, !DrawMesh);
    }

    void LateUpdate()
    {
        if(Mesh != null)
        {
            for(int i=0; i < Grid.Length; i++)
            {
                MeshVertices[i] = Simulator.GetParticlePosition(Grid[i]);
            }

            Mesh.SetVertices(MeshVertices);
            Mesh.RecalculateNormals();

            Graphics.DrawMesh(Mesh, Vector3.zero, Quaternion.identity, MeshMaterial, 0, null, 0, null, false, false);
        }
    }
}
