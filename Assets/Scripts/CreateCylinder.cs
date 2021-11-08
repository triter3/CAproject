using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateCylinder : MonoBehaviour
{
    public ParticleSimulator Simulator;

    public float GridXLength;
    public int GridXSize;

    public float GridYLength;
    public int GridYSize;
    public float Ke;
    public float BlendKe;
    public float Kd;
    public float ResetTime = float.PositiveInfinity;
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
        // float percentatgeX = 2.0f * GridXLength / (GridXLength + GridYLength);
        // int AxisParticles = (int)Mathf.Sqrt(NumParticles);
        // int GridXSize = (int)(float)(percentatgeX * AxisParticles);
        // int GridYSize = 2 * AxisParticles - GridXSize;

        Debug.Log("GridSize: " + GridXSize + ", " + GridYSize);

        for(int i=0; i < CustomParticles.Count; i++)
        {
            CustomParticle cp = CustomParticles[i];
            if(cp.GridCoord.x < 0) cp.GridCoord.x = GridXSize + cp.GridCoord.x;
            if(cp.GridCoord.y < 0) cp.GridCoord.y = GridYSize + cp.GridCoord.y;
            CustomParticles[i] = cp;
        }

        float radius = GridXLength/(2*Mathf.PI);

        float stepX = GridXLength / GridXSize;
        float stepY = GridYLength / GridYSize;
        float stepXY = Mathf.Sqrt(stepX * stepX + stepY * stepY);

        Grid = new int[GridXSize * GridYSize];
        MeshVertices = new Vector3[GridXSize * GridYSize];
        if(DrawMesh)
        {
            MeshTriangles = new int[6 * (GridXSize - 1) * (GridYSize - 1)];
        }

        for(int j=0; j < GridYSize; j++)
        {
            for(int i=0; i < GridXSize; i++)
            {
                float angle = 2.0f * Mathf.PI * (float)i/GridXSize;
                Vector3 pos = transform.TransformPoint(new Vector3(radius * Mathf.Cos(angle), j * stepY, radius * Mathf.Sin(angle)));
                MeshVertices[j * GridXSize + i] = pos;
                int pId = GetParticleId(new Vector2Int(i, j), pos);
                Grid[i + j * GridXSize] = pId;

                if(i > 0)
                {
                    Simulator.AddSpring(pId, Grid[i - 1 + j * GridXSize], stepX, Ke, Kd, !DrawMesh);
                }

                if(j > 0)
                {
                    Simulator.AddSpring(pId, Grid[i + (j - 1) * GridXSize], stepY, Ke, Kd, !DrawMesh);
                }

                if(i > 0 && j > 0)
                {
                    Simulator.AddSpring(pId, Grid[(i - 1) + (j - 1) * GridXSize], stepXY, Ke, Kd, false);
                }

                if(i < GridXSize - 1 && j > 0)
                {
                    Simulator.AddSpring(pId, Grid[(i + 1) + (j - 1) * GridXSize], stepXY, Ke, Kd, false);
                }

                for(int o=2; o < GridXSize; o++)
                {
                    if(i >= o)
                    {
                        float deltaAngle = 2.0f * Mathf.PI / GridXSize;
                        float dist = (pos - transform.TransformPoint(
                                            new Vector3(radius * Mathf.Cos(angle - o*deltaAngle), j * stepY, 
                                                        radius * Mathf.Sin(angle - o*deltaAngle)))).magnitude;
                        Simulator.AddSpring(pId, Grid[i - o + j * GridXSize], dist, BlendKe, Kd, false);
                    }
                }

                if(j > 1)
                {
                    Simulator.AddSpring(pId, Grid[i + (j - 2) * GridXSize], 2.0f * stepY, GridXSize * BlendKe, Kd, false);
                }

                if(DrawMesh)
                {
                    if(i > 0 && j > 0)
                    {
                        int tIndex = 6 * ((j-1) * (GridXSize - 1) + i - 1);
                        MeshTriangles[tIndex] = j * GridXSize + i;
                        MeshTriangles[tIndex + 1] = (j - 1) * GridXSize + i - 1;
                        MeshTriangles[tIndex + 2] = (j - 1) * GridXSize + i;

                        MeshTriangles[3 + tIndex] = j * GridXSize + i;
                        MeshTriangles[3 + tIndex + 1] = j * GridXSize + i - 1;
                        MeshTriangles[3 + tIndex + 2] = (j - 1) * GridXSize + i - 1;
                    }
                }
            }

            // Connect first with last
            {
                int pId = Grid[j * GridXSize];

                Simulator.AddSpring(pId, Grid[GridXSize - 1 + j * GridXSize], stepX, Ke, Kd, !DrawMesh);

                if(j > 0)
                {
                    Simulator.AddSpring(pId, Grid[(GridXSize - 1) + (j - 1) * GridXSize], stepXY, Ke, Kd, false);
                    Simulator.AddSpring(Grid[(GridXSize - 1) + j * GridXSize], Grid[(j - 1) * GridXSize], stepXY, Ke, Kd, false);
                }

                // if(GridXSize > 2)
                // {
                //     Simulator.AddSpring(pId, Grid[(GridXSize - 2) + j * GridXSize], 2.0f * stepX, BlendKe, Kd, false);
                //     Simulator.AddSpring(Grid[(GridXSize - 1) + j * GridXSize], Grid[1 + j * GridXSize], 2.0f * stepX, BlendKe, Kd, false);
                // }

                // if(DrawMesh)
                // {
                //     if(i > 0 && j > 0)
                //     {
                //         int tIndex = 6 * ((j-1) * (GridXSize - 1) + i - 1);
                //         MeshTriangles[tIndex] = j * GridXSize + i;
                //         MeshTriangles[tIndex + 1] = (j - 1) * GridXSize + i - 1;
                //         MeshTriangles[tIndex + 2] = (j - 1) * GridXSize + i;

                //         MeshTriangles[3 + tIndex] = j * GridXSize + i;
                //         MeshTriangles[3 + tIndex + 1] = j * GridXSize + i - 1;
                //         MeshTriangles[3 + tIndex + 2] = (j - 1) * GridXSize + i - 1;
                //     }
                // }
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

    float accTime = 0;
    void LateUpdate()
    {
        accTime += Time.deltaTime;
        if(accTime > ResetTime)
        {
            accTime = 0;
            for(int i=0; i < Grid.Length; i++)
            {
                Simulator.SetParticlePosition(Grid[i], MeshVertices[i]);
            }
        }

        if(Mesh != null)
        {
            for(int i=0; i < Grid.Length; i++)
            {
                MeshVertices[i] = Simulator.GetParticlePosition(Grid[i]);
            }

            Mesh.SetVertices(MeshVertices);
            Mesh.RecalculateNormals();

            Graphics.DrawMesh(Mesh, Vector3.zero, Quaternion.identity, MeshMaterial, 0);
        }
    }
}
