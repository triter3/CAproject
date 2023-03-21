using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SdfOctreePlane : MonoBehaviour
{
    public string Path = "";
    public bool IsNormalized = true;
    public Transform TargetTransfrom;
    
    private bool SdfFunctionLoaded = false;
    private SdfFunction SdfFunction;
    private MeshFilter MeshFilter;
    private MeshRenderer MeshRenderer;
    private Matrix4x4 StaticTransfrom;
    private ComputeBuffer OctreeData;

    void Awake()
    {
        MeshFilter = GetComponent<MeshFilter>();
        MeshRenderer = GetComponent<MeshRenderer>();
        if(!SdfFunctionLoaded)
        {
            float scale = 1.0f;
            Vector3 center = Vector3.zero;
            
            SdfFunction = SdfFunction.LoadSdf(Path);
            SdfFunction.SetTransfrom(Matrix4x4.Scale(new Vector3(scale, scale, scale)) * Matrix4x4.Translate(-center) * TargetTransfrom.transform.worldToLocalMatrix, 
                                     TargetTransfrom.transform.rotation);

            Vector3 min = SdfFunction.GetBBMinPoint();
            Vector3 size = SdfFunction.GetBBSize();

            uint gridSize = SdfFunction.GetStartGridSize();

            StaticTransfrom = Matrix4x4.Scale(new Vector3(1.0f / size.x, 1.0f / size.y, 1.0f / size.z)) * 
                              Matrix4x4.Translate(-min);

            MeshRenderer.material.SetMatrix("octreeTransform", StaticTransfrom * TargetTransfrom.transform.worldToLocalMatrix);

            MeshRenderer.material.SetVector("startGridSize", new Vector3(gridSize, gridSize, gridSize));

            uint octreeSize = SdfFunction.GetOctreeDataSize();
            Debug.Log(octreeSize);
            uint[] octreeData = new uint[octreeSize];
            SdfFunction.GetOctreeData(octreeData);

            for(uint i=0; i < gridSize * gridSize * gridSize; i++)
            {
                if((octreeData[i] & (1 << 31)) == 0)
                {
                    Debug.Log("Is not leaf");
                }
            }

            Debug.Log(octreeData[gridSize * gridSize * gridSize]);

            OctreeData = new ComputeBuffer((int)octreeSize, 4);
            OctreeData.SetData(octreeData);
            MeshRenderer.material.SetBuffer("octreeData", OctreeData);
        }
    }

    void OnDisable()
    {
        if(SdfFunctionLoaded)
        {
            SdfFunction.Dispose();
            SdfFunctionLoaded = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        MeshRenderer.material.SetMatrix("octreeTransform", StaticTransfrom * TargetTransfrom.transform.worldToLocalMatrix);
        //MeshRenderer.material.SetMatrix("octreeTransform", TargetTransfrom.transform.worldToLocalMatrix);
    }
}
