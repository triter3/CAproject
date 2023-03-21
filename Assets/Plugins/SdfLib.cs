using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;

public struct SdfFunction
{
    private IntPtr SdfPtr;
    private Matrix4x4 Transform; // Transform from world to SDF space
    private Quaternion Rotation; // Rotation from SDF sace to world
    private float DistanceScale;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    AtomicSafetyHandle Safety;
#endif

    private static Quaternion QuaternionFromMatrix(Matrix4x4 m) {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] + m[1,1] + m[2,2] ) ) / 2; 
        q.x = Mathf.Sqrt( Mathf.Max( 0, 1 + m[0,0] - m[1,1] - m[2,2] ) ) / 2; 
        q.y = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] + m[1,1] - m[2,2] ) ) / 2; 
        q.z = Mathf.Sqrt( Mathf.Max( 0, 1 - m[0,0] - m[1,1] + m[2,2] ) ) / 2; 
        q.x *= Mathf.Sign( q.x * ( m[2,1] - m[1,2] ) );
        q.y *= Mathf.Sign( q.y * ( m[0,2] - m[2,0] ) );
        q.z *= Mathf.Sign( q.z * ( m[1,0] - m[0,1] ) );
        return q;
    }

    [BurstDiscard]
    public void SaveSdf(string outpath)
    {
        SdfLib.saveSdf(SdfPtr, outpath);
    }

    [BurstDiscard]
    public static SdfFunction LoadSdf(string path)
    {
        SdfFunction sdf = new SdfFunction();
        sdf.SdfPtr = SdfLib.loadSdf(path);
        Debug.Log(sdf.SdfPtr);
        sdf.Transform = Matrix4x4.identity;
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            sdf.Safety = AtomicSafetyHandle.Create();
        #endif
        return sdf;
    }

    [BurstDiscard]
    public static SdfFunction CreateExactOctreeSdf(Mesh mesh, 
                                Vector3 minBB, Vector3 maxBB,
                                uint startOctreeDepth,
                                uint maxOctreeDepth,
                                uint minTrianglesPerNode,
                                uint numThreads=1)
    {
        SdfFunction sdf = new SdfFunction();
        sdf.SdfPtr = SdfLib.createExactOctreeSdf(mesh.vertices, mesh.GetIndices(0),
                                                 minBB, maxBB,
                                                 startOctreeDepth,
                                                 maxOctreeDepth,
                                                 minTrianglesPerNode,
                                                 numThreads);
        sdf.Transform = Matrix4x4.identity;
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            sdf.Safety = AtomicSafetyHandle.Create();
        #endif
        return sdf;
    }


    [BurstDiscard]
    public static SdfFunction CreateOctreeSdf(Mesh mesh, 
                                Vector3 minBB, Vector3 maxBB,
                                uint startOctreeDepth,
                                uint maxOctreeDepth,
                                float maxError,
                                uint numThreads=1)
    {
        SdfFunction sdf = new SdfFunction();
        sdf.SdfPtr = SdfLib.createOctreeSdf(mesh.vertices, mesh.GetIndices(0),
                                            minBB, maxBB,
                                            startOctreeDepth,
                                            maxOctreeDepth,
                                            maxError,
                                            numThreads);
        sdf.Transform = Matrix4x4.identity;
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            sdf.Safety = AtomicSafetyHandle.Create();
        #endif
        return sdf;
    }

    /// <summary>
    /// Set transfrom SDF transfrom from world to SDF space
    /// </summary>
    public void SetTransfrom(Matrix4x4 transform, Quaternion rot)
    {
        Transform = transform;

        Vector3 scale = transform.lossyScale;
        Debug.Assert(Mathf.Abs(scale.x - scale.y) < 1e-4 &&
                     Mathf.Abs(scale.x - scale.z) < 1e-4 &&
                     Mathf.Abs(scale.y - scale.z) < 1e-4, "Sdf only supports uniform scales in the three axis");
        
        DistanceScale = 1.0f / scale.x;

        // Rotation = Quaternion.Inverse(QuaternionFromMatrix(transform));
        Rotation = rot;
    }

    public float GetDistance(Vector3 point)
    {
        return DistanceScale * SdfLib.getDistance(SdfPtr, Transform.MultiplyPoint(point));
    }

    public float GetDistance(Vector3 point, out Vector3 gradient)
    {
        float res = DistanceScale * SdfLib.getDistanceAndGradient(SdfPtr, Transform.MultiplyPoint(point), out gradient);
        gradient = Rotation * gradient;
        return res;
    }

    public Vector3 GetBBMinPoint()
    {
        return SdfLib.getBBMinPoint(SdfPtr);
    }

    public Vector3 GetBBSize()
    {
        return SdfLib.getBBSize(SdfPtr);
    }

    public uint GetStartGridSize()
    {
        return SdfLib.getStartGridSize(SdfPtr);
    }

    public uint GetOctreeDataSize()
    {
        return SdfLib.getOctreeDataSize(SdfPtr);
    }

    public void GetOctreeData(uint[] data)
    {
        SdfLib.getOctreeData(SdfPtr, data);
    }

    public void Dispose()
    {
        #if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(Safety);
        #endif

        SdfLib.deleteSdf(SdfPtr);
    }
}

internal class SdfLib
{
    public static IntPtr createExactOctreeSdf(Vector3[] vertices, int[] indices,
                                              Vector3 minBB,
                                              Vector3 maxBB,
                                              uint startOctreeDepth,
                                              uint maxOctreeDepth,
                                              uint minTrianglesPerNode,
                                              uint numThreads=1)
    {
        return createExactOctreeSdf(vertices, (uint)vertices.Length, 
                                    indices, (uint)indices.Length,
                                    minBB.x, minBB.y, minBB.z,
                                    maxBB.x, maxBB.y, maxBB.z,
                                    startOctreeDepth,
                                    maxOctreeDepth,
                                    minTrianglesPerNode,
                                    numThreads);
    }

    public static IntPtr createOctreeSdf(Vector3[] vertices, int[] indices,
                                              Vector3 minBB,
                                              Vector3 maxBB,
                                              uint startOctreeDepth,
                                              uint maxOctreeDepth,
                                              float maxError,
                                              uint numThreads=1)
    {
        return createOctreeSdf(vertices, (uint)vertices.Length, 
                                    indices, (uint)indices.Length,
                                    minBB.x, minBB.y, minBB.z,
                                    maxBB.x, maxBB.y, maxBB.z,
                                    startOctreeDepth,
                                    maxOctreeDepth,
                                    maxError, 
                                    numThreads);
    }

    [DllImport("SdfLib.dll")]
    public static extern void saveSdf(IntPtr sdfPointer, string path);

    [DllImport("SdfLib.dll")]
    public static extern IntPtr loadSdf(string path);

    [DllImport("SdfLib.dll")]
    private static extern IntPtr createExactOctreeSdf(Vector3[] vertices, uint numVertices, 
                                                     int[] indices, uint numIndices,
                                                     float bbMinX, float bbMinY, float bbMinZ,
                                                     float bbMaxX, float bbMaxY, float bbMaxZ,
                                                     uint startOctreeDepth,
                                                     uint maxOctreeDepth,
                                                     uint minTrianglesPerNode,
                                                     uint numThreads);

    [DllImport("SdfLib.dll")]
    private static extern IntPtr createOctreeSdf(Vector3[] vertices, uint numVertices, 
                                                 int[] indices, uint numIndices,
                                                 float bbMinX, float bbMinY, float bbMinZ,
                                                 float bbMaxX, float bbMaxY, float bbMaxZ,
                                                 uint startOctreeDepth,
                                                 uint maxOctreeDepth,
                                                 float maxError,
                                                 uint numThreads);

    public static float getDistance(IntPtr sdfPointer, Vector3 point)
    {
        return getDistance(sdfPointer, point.x, point.y, point.z);
    }

    public static float getDistanceAndGradient(IntPtr sdfPointer, Vector3 point, out Vector3 gradient)
    {
        return getDistanceAndGradient(sdfPointer, point.x, point.y, point.z, out gradient);
    }

    [DllImport("SdfLib.dll")]
    private static extern float getDistance(IntPtr sdfPointer, float pointX, float pointY, float pointZ);

    [DllImport("SdfLib.dll")]
    private static extern float getDistanceAndGradient(IntPtr sdfPointer, float pointX, float pointY, float pointZ, out Vector3 gradient);

    [DllImport("SdfLib.dll")]
    public static extern Vector3 getBBMinPoint(IntPtr sdfPointer);

    [DllImport("SdfLib.dll")]
    public static extern Vector3 getBBSize(IntPtr sdfPointer);

    [DllImport("SdfLib.dll")]
    public static extern uint getStartGridSize(IntPtr sdfPointer);

    [DllImport("SdfLib.dll")]
    public static extern uint getOctreeDataSize(IntPtr sdfPointer);

    [DllImport("SdfLib.dll")]
    public static extern void getOctreeData(IntPtr sdfPointer, uint[] data);

    [DllImport("SdfLib.dll")]
    public static extern void deleteSdf(IntPtr sdfPointer);
}
