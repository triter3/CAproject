using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

public class ParticleSimulator : MonoBehaviour
{
    public Mesh ParticleMesh;
    public Material ParticleMaterial;
    public Spawn.Spawns SpawnType;
    public int NumParticles = 1;
    public float SpawnVelocity = 5.0f;
    [Range(0.0f, 1.0f)]
    public float BouncingCoeff = 1.0f;
    [Range(0.0f, 1.0f)]
    public float FrictionCoeff = 0.0f;
    [Range(0.0f, 1.0f)]
    public float Drag = 0.0f;
    public float ParticleSize = 0.1f;
    public float MaxDeltaTime = 0.01f;
    public bool ScalarFieldCollision = false;

    private NativeArray<PhysicsEngine.Particle> ParticleProperties;
    private NativeArray<Vector4> ParticleTransforms;
    private PhysicsEngine PhysicsEngine;

    private NativeCounter FreeParticlesCounter;
    private NativeCounter SpawnCounter;

    // Instancing data
    private ComputeBuffer PositionBuffer;
    private ComputeBuffer ArgsBuffer;
    private uint[] InstArgs = new uint[5] { 0, 0, 0, 0, 0 };

    void Awake() 
    {
        Application.targetFrameRate = 120;

        FreeParticlesCounter = new NativeCounter(Allocator.Persistent);
        SpawnCounter = new NativeCounter(Allocator.Persistent);

        FreeParticlesCounter.Count = NumParticles;

        // Init paticle system
        ParticleProperties = new NativeArray<PhysicsEngine.Particle>(NumParticles, Allocator.Persistent);
        ParticleTransforms = new NativeArray<Vector4>(NumParticles, Allocator.Persistent);
        System.Random r = new System.Random();
        for(int i=0; i < NumParticles; i++)
        {
            PhysicsEngine.Particle p;
            p.Position = new Vector3(0.0f, 3.0f, 0.0f);
            p.Velocity = Vector3.zero;
            p.Lifetime = -1.0f;
            p.seed = (uint)r.Next();
            ParticleProperties[i] = p;
        }

        PhysicsEngine = new PhysicsEngine();
        PhysicsEngine.Particles = ParticleProperties;
        PhysicsEngine.Output = ParticleTransforms;
        PhysicsEngine.ConstantForce = new Vector3(0.0f, -9.8f, 0.0f);
        // PhysicsEngine.ConstantForce = new Vector3(0.0f, 0.0f, 0.0f);
        PhysicsEngine.ScalarFieldCollision = ScalarFieldCollision;
        PhysicsEngine.DeltaTime = 0.01f;
        PhysicsEngine.BallRadius = ParticleSize;
        PhysicsEngine.BouncingCoeff = BouncingCoeff;
        PhysicsEngine.FrictionCoeff = FrictionCoeff;
        PhysicsEngine.Drag = Drag;
        PhysicsEngine.SpawnType = SpawnType;

        PhysicsEngine.MaxDeltaTime = MaxDeltaTime;

        SetupShader();
    }

    void OnEnable()
    {
        LoadColliders();
    }

    void OnDisable() 
    {
        ParticleProperties.Dispose();
        ParticleTransforms.Dispose();
        PhysicsEngine.Planes.Dispose();
        PhysicsEngine.Spheres.Dispose();
        PhysicsEngine.Triangles.Dispose();

        FreeParticlesCounter.Dispose();
        SpawnCounter.Dispose();

        PositionBuffer.Dispose();
        ArgsBuffer.Dispose();
    }

    private void SetupShader()
    {
        ArgsBuffer = new ComputeBuffer(1, InstArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        PositionBuffer = new ComputeBuffer(NumParticles, 16);
        ParticleMaterial.SetBuffer("_InstancesPosition", PositionBuffer);

        InstArgs[0] = (uint)ParticleMesh.GetIndexCount(0);
        InstArgs[1] = (uint)NumParticles;
        InstArgs[2] = (uint)ParticleMesh.GetIndexStart(0);
        InstArgs[3] = (uint)ParticleMesh.GetBaseVertex(0);

        ArgsBuffer.SetData(InstArgs);
    }

    private void LoadColliders()
    {
        PlaneColliderComponent[] planes = GameObject.FindObjectsOfType<PlaneColliderComponent>();
        PhysicsEngine.Planes = new NativeArray<Colliders.Plane>(planes.Length, Allocator.Persistent);
        for(int i=0; i < planes.Length; i++)
        {
            PhysicsEngine.Planes[i] = planes[i].GetCollider();
        }

        SphereColliderComponent[] spheres = GameObject.FindObjectsOfType<SphereColliderComponent>();
        PhysicsEngine.Spheres = new NativeArray<Colliders.Sphere>(spheres.Length, Allocator.Persistent);
        for(int i=0; i < spheres.Length; i++)
        {
            PhysicsEngine.Spheres[i] = spheres[i].GetCollider();
        }

        TriangleColliderComponent[] triangles = GameObject.FindObjectsOfType<TriangleColliderComponent>();
        MeshColliderComponent[] meshes = GameObject.FindObjectsOfType<MeshColliderComponent>();
        int numMeshTriangles = 0;
        for(int i=0; i < meshes.Length; i++)
        {
            numMeshTriangles += meshes[i].NumOfColliders();
        }

        PhysicsEngine.Triangles = new NativeArray<Colliders.Triangle>(triangles.Length + numMeshTriangles, Allocator.Persistent);

        for(int i=0; i < triangles.Length; i++)
        {
            PhysicsEngine.Triangles[i] = triangles[i].GetCollider();
        }

        int tIndex = triangles.Length;
        for(int i=0; i < meshes.Length; i++)
        {
            meshes[i].AddColliders(PhysicsEngine.Triangles, tIndex, PhysicsEngine.BallRadius);
            tIndex += meshes[i].NumOfColliders();
        }

        DistanceFunction distanceFunction = GameObject.FindObjectOfType<DistanceFunction>();
        if(distanceFunction)
        {
            PhysicsEngine.ScalarFieldOrigin = distanceFunction.transform.position;
        }
        else
        {
            PhysicsEngine.ScalarFieldCollision = false;
        }
    }

    private float AccTime = 0.0f;
    private float AccSpawnTime = 0.0f;
    void Update()
    {
        // Decide the number of particles to spawn
        AccSpawnTime += Time.deltaTime;
        //SpawnCounter.Count = Mathf.Min(FreeParticlesCounter.Count, (int)(AccSpawnTime * SpawnVelocity));
        SpawnCounter.Count = (int)(AccSpawnTime * SpawnVelocity);
        AccSpawnTime = AccSpawnTime % (1.0f/SpawnVelocity);

        PhysicsEngine.FreeParticlesCounter = FreeParticlesCounter;
        PhysicsEngine.SpawnCounter = SpawnCounter;
        PhysicsEngine.DeltaTime = Time.deltaTime + AccTime;

        JobHandle handle = PhysicsEngine.Schedule(NumParticles, 32);
        AccTime = PhysicsEngine.DeltaTime % PhysicsEngine.MaxDeltaTime;
        if(AccTime > 0.99f*MaxDeltaTime) AccTime = 0.0f;
        handle.Complete();

        PositionBuffer.SetData(ParticleTransforms.ToArray());

        Graphics.DrawMeshInstancedIndirect(ParticleMesh, 0, ParticleMaterial, 
                                           new Bounds(Vector3.zero, Vector3.one * 1000.0f), 
                                           ArgsBuffer, receiveShadows: false, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off);
        //Graphics.DrawMeshInstanced(ParticleMesh, 0, ParticleMaterial, ParticleTransforms.ToArray(), ParticleTransforms.Length);
    }
}
