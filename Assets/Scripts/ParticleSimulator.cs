using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Stopwatch = System.Diagnostics.Stopwatch;

public class ParticleSimulator : MonoBehaviour
{
    public Mesh ParticleMesh;
    public Material ParticleMaterial;

    public Mesh CylinderMesh;
    public Material CylinderMaterial;

    public Spawn.Spawns SpawnType;
    public int NumParticles = 1;
    public float SpawnVelocity = 5.0f;
    [Range(0.0f, 1.0f)]
    public float BouncingCoeff = 1.0f;
    [Range(0.0f, 1.0f)]
    public float FrictionCoeff = 0.0f;
    [Range(0.0f, 1.0f)]
    public float Drag = 0.0f;
    public float Gravity = 9.8f;
    public float ParticleSize = 0.1f;
    public float RenderParticleSize = 0.1f;
    public float MaxDeltaTime = 0.01f;
    public bool UseRenderDeltaTime = false;
    public bool ScalarFieldCollision = false;

    // Particles Data
    private NativeList<PhysicsEngine.Particle> InputParticles;
    private NativeList<PhysicsEngine.Particle> OutputParticles;
    private NativeArray<Vector4> ParticleTransforms;
    

    // Springs Data
    private NativeList<PhysicsEngine.Spring> Springs;
    public NativeArray<PhysicsEngine.ParticleSpringsInfo> ParticlesSpringsInfo;
    public NativeArray<PhysicsEngine.Spring> ParticlesSprings;

    public struct CylinderData
    {
        public Vector4 position; // position + radius
        public Vector4 direction; // direction + length
    }
    private NativeArray<CylinderData> CylindersData;

    private PhysicsEngine PhysicsEngine;


    private NativeCounter FreeParticlesCounter;
    private NativeCounter SpawnCounter;

    // Instancing spheres data
    private ComputeBuffer SpherePositionBuffer;
    private ComputeBuffer SphereArgsBuffer;

    // Instanceing cylinder data
    private ComputeBuffer CylinderDataBuffer;
    private ComputeBuffer CylinderArgsBuffer;
    private uint[] InstArgs = new uint[5] { 0, 0, 0, 0, 0 };

    private bool Started = false;

    void Awake() 
    {
        Application.targetFrameRate = 120;
        if(!InputParticles.IsCreated || !Springs.IsCreated)
            Init();
    }

    void Init()
    {
        InputParticles = new NativeList<PhysicsEngine.Particle>(0, Allocator.Persistent);
        OutputParticles = new NativeList<PhysicsEngine.Particle>(0, Allocator.Persistent);
        Springs = new NativeList<PhysicsEngine.Spring>(0, Allocator.Persistent);
    }

    void Start()
    {
        Started = true;

        FreeParticlesCounter = new NativeCounter(Allocator.Persistent);
        SpawnCounter = new NativeCounter(Allocator.Persistent);

        FreeParticlesCounter.Count = NumParticles;

        // Init paticle system

        System.Random r = new System.Random();
        for(int i=0; i < NumParticles; i++)
        {
            PhysicsEngine.Particle p;
            p.Position = new Vector3(0.0f, 3.0f, 0.0f);
            p.Velocity = Vector3.zero;
            p.Force = Vector3.zero;
            p.InvMass = 1.0f;
            p.Lifetime = -1.0f;
            p.Seed = (uint)r.Next();
            p.RadiusMultiplier = 1.0f;
            InputParticles.Add(p);
            OutputParticles.Add(p);
        }

        List<int>[] particleSpringsList = new List<int>[InputParticles.Length];

        for(int s=0; s < Springs.Length; s++)
        {
            List<int> list = particleSpringsList[Springs[s].ParticleId1];
            if(list == null)
            {
                particleSpringsList[Springs[s].ParticleId1] = new List<int>();
                list = particleSpringsList[Springs[s].ParticleId1];
            }
            list.Add(s);

            list = particleSpringsList[Springs[s].ParticleId2];
            if(list == null)
            {
                particleSpringsList[Springs[s].ParticleId2] = new List<int>();
                list = particleSpringsList[Springs[s].ParticleId2];
            }
            list.Add(s);
        }

        ParticlesSpringsInfo = new NativeArray<PhysicsEngine.ParticleSpringsInfo>(InputParticles.Length, Allocator.Persistent);
        ParticlesSprings = new NativeArray<PhysicsEngine.Spring>(2 * Springs.Length, Allocator.Persistent);
        int index = 0;
        for(int p=0; p < InputParticles.Length; p++)
        {
            PhysicsEngine.ParticleSpringsInfo info;
            info.startIndex = index;
            if(particleSpringsList[p] == null) 
                continue;
            info.NumSprings = particleSpringsList[p].Count;
            for(int s=0; s < particleSpringsList[p].Count; s++)
            {
                PhysicsEngine.Spring spring = Springs[particleSpringsList[p][s]];
                if(spring.ParticleId1 != p)
                {
                    spring.ParticleId2 = spring.ParticleId1;
                    spring.ParticleId1 = p;
                }
                ParticlesSprings[index++] = spring;
            }
            ParticlesSpringsInfo[p] = info;
        }


        ParticleTransforms = new NativeArray<Vector4>(InputParticles.Length, Allocator.Persistent);
        CylindersData = new NativeArray<CylinderData>(Springs.Length, Allocator.Persistent);

        PhysicsEngine = new PhysicsEngine();

        PhysicsEngine.MyPhysicsCollisions.ParticlesSpringsInfo = ParticlesSpringsInfo;
        PhysicsEngine.MyPhysicsCollisions.ParticlesSprings = ParticlesSprings;

        PhysicsEngine.MyPhysicsSprings.Springs = Springs;

        PhysicsEngine.MyUpdatePositions.ParticlesOutput = ParticleTransforms;
        PhysicsEngine.MyUpdatePositions.Springs = Springs;
        PhysicsEngine.MyUpdatePositions.SpringsOutput = CylindersData;

        UpdatePhysicsEngineProperties();

        SetupSphereShader();
        SetupCylinderShader();
        LoadColliders();
    }

    public int AddParticle(Vector3 position, float mass, bool active = true, bool print = true)
    {
        Debug.Assert(!Started, "Add only suppoted during initialization step");
        if(!InputParticles.IsCreated || !Springs.IsCreated)
            Init();

        PhysicsEngine.Particle p;
        p.Position = position;
        p.Velocity = Vector3.zero;
        p.Force = Vector3.zero;
        p.InvMass = 1.0f / mass;
        p.Lifetime = (active) ? float.PositiveInfinity : -1.0f;
        p.Seed = 0;
        p.RadiusMultiplier = print ? 1.0f : 0.0f;
        InputParticles.Add(p);
        OutputParticles.Add(p);
        return InputParticles.Length - 1;
    }

    public void AddSpring(int pId1, int pId2, float targetDistance, float ke, float kd, bool print = true)
    {
        if(!InputParticles.IsCreated || !Springs.IsCreated)
            Init();

        PhysicsEngine.Spring s = new PhysicsEngine.Spring();
        s.ParticleId1 = pId1;
        s.ParticleId2 = pId2;
        s.TargetDistance = targetDistance;
        s.Ke = ke;
        s.Kd = kd;
        s.RadiusMultiplier = print ? 1.0f : 0.0f;
        Springs.Add(s);
    }

    public void AddSpringToNearParticle(int pId, float ke, float kd)
    {
        if(!InputParticles.IsCreated || !Springs.IsCreated)
            Init();

        Vector3 pPos = InputParticles[pId].Position;
        int pId2 = -1;
        float minDistance = float.PositiveInfinity;
        for(int p=0; p < InputParticles.Length; p++)
        {
            float aux = (pPos - InputParticles[p].Position).sqrMagnitude;
            if(aux < minDistance)
            {
                minDistance = aux;
                pId2 = p; 
            }
        }

        if(pId2 >= 0)
        {
            PhysicsEngine.Spring s = new PhysicsEngine.Spring();
            s.ParticleId1 = pId;
            s.ParticleId2 = pId2;
            s.TargetDistance = Mathf.Sqrt(minDistance);
            s.Ke = ke;
            s.Kd = kd;
            Springs.Add(s);
        }
    }

    public void SetParticlePosition(int pId, Vector3 position)
    {
        PhysicsEngine.Particle p = InputParticles[pId];
        p.Position = position;
        InputParticles[pId] = p;
    }

    public Vector3 GetParticlePosition(int pId)
    {
        return InputParticles[pId].Position;
    }

    public void ActiveParticle(int pId)
    {
        PhysicsEngine.Particle p = InputParticles[pId];
        p.Lifetime = float.PositiveInfinity;
        InputParticles[pId] = p;
    }

    public void DisableParticle(int pId)
    {
        PhysicsEngine.Particle p = InputParticles[pId];
        p.Lifetime = -1.0f;
        InputParticles[pId] = p;
    }

    public void SetSphereColliderPosition(int colliderId, Vector3 pos)
    {
        Colliders.Sphere s = PhysicsEngine.MyPhysicsCollisions.Spheres[colliderId];
        s.DisVector = pos - s.Position;
        s.Position = pos;
        s.DotPosition = Vector3.Dot(s.Position, s.Position);
        PhysicsEngine.MyPhysicsCollisions.Spheres[colliderId] = s;
    }

    void UpdatePhysicsEngineProperties()
    {
        PhysicsEngine.MyPhysicsCollisions.ConstantForce = Vector3.zero;
        PhysicsEngine.MyPhysicsCollisions.ConstantAcceleration = new Vector3(0.0f, -Gravity, 0.0f);
        PhysicsEngine.MyPhysicsCollisions.ScalarFieldCollision = ScalarFieldCollision;
        PhysicsEngine.MyPhysicsCollisions.BallRadius = ParticleSize;
        PhysicsEngine.MyPhysicsCollisions.BouncingCoeff = BouncingCoeff;
        PhysicsEngine.MyPhysicsCollisions.FrictionCoeff = FrictionCoeff;
        PhysicsEngine.MyPhysicsCollisions.Drag = Drag;
        PhysicsEngine.MyPhysicsCollisions.SpawnType = SpawnType;
        PhysicsEngine.MyPhysicsCollisions.MaxDeltaTime = 0.2f * MaxDeltaTime;

        PhysicsEngine.MyUpdatePositions.BallRadius = RenderParticleSize;
    }

    void OnDisable() 
    {
        InputParticles.Dispose();
        OutputParticles.Dispose();
        ParticlesSpringsInfo.Dispose();
        ParticlesSprings.Dispose();
        ParticleTransforms.Dispose();
        CylindersData.Dispose();
        PhysicsEngine.MyPhysicsCollisions.Dispose();
        PhysicsEngine.MyPhysicsSprings.Dispose();

        FreeParticlesCounter.Dispose();
        SpawnCounter.Dispose();

        SpherePositionBuffer.Dispose();
        SphereArgsBuffer.Dispose();

        CylinderDataBuffer.Dispose();
        CylinderArgsBuffer.Dispose();
    }

    private void SetupSphereShader()
    {
        SphereArgsBuffer = new ComputeBuffer(1, InstArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        SpherePositionBuffer = new ComputeBuffer(InputParticles.Length, 16);
        ParticleMaterial.SetBuffer("_InstancesPosition", SpherePositionBuffer);

        InstArgs[0] = (uint)ParticleMesh.GetIndexCount(0);
        InstArgs[1] = (uint)InputParticles.Length;
        InstArgs[2] = (uint)ParticleMesh.GetIndexStart(0);
        InstArgs[3] = (uint)ParticleMesh.GetBaseVertex(0);

        SphereArgsBuffer.SetData(InstArgs);
    }

    private void SetupCylinderShader()
    {
        CylinderArgsBuffer = new ComputeBuffer(1, InstArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        CylinderDataBuffer = new ComputeBuffer(Mathf.Max(Springs.Length, 1), 32);
        CylinderMaterial.SetBuffer("_InstancesData", CylinderDataBuffer);

        InstArgs[0] = (uint)CylinderMesh.GetIndexCount(0);
        InstArgs[1] = (uint)Springs.Length;
        InstArgs[2] = (uint)CylinderMesh.GetIndexStart(0);
        InstArgs[3] = (uint)CylinderMesh.GetBaseVertex(0);

        CylinderArgsBuffer.SetData(InstArgs);
    }

    private void LoadColliders()
    {
        PlaneColliderComponent[] planes = GameObject.FindObjectsOfType<PlaneColliderComponent>();
        PhysicsEngine.MyPhysicsCollisions.Planes = new NativeArray<Colliders.Plane>(planes.Length, Allocator.Persistent);
        for(int i=0; i < planes.Length; i++)
        {
            PhysicsEngine.MyPhysicsCollisions.Planes[i] = planes[i].GetCollider();
        }

        SphereColliderComponent[] spheres = GameObject.FindObjectsOfType<SphereColliderComponent>();
        PhysicsEngine.MyPhysicsCollisions.Spheres = new NativeArray<Colliders.Sphere>(spheres.Length, Allocator.Persistent);
        for(int i=0; i < spheres.Length; i++)
        {
            PhysicsEngine.MyPhysicsCollisions.Spheres[i] = spheres[i].GetCollider(i);
        }

        TriangleColliderComponent[] triangles = GameObject.FindObjectsOfType<TriangleColliderComponent>();
        MeshColliderComponent[] meshes = GameObject.FindObjectsOfType<MeshColliderComponent>();
        int numMeshTriangles = 0;
        for(int i=0; i < meshes.Length; i++)
        {
            numMeshTriangles += meshes[i].NumOfColliders();
        }

        PhysicsEngine.MyPhysicsCollisions.Triangles = new NativeArray<Colliders.Triangle>(triangles.Length + numMeshTriangles, Allocator.Persistent);

        for(int i=0; i < triangles.Length; i++)
        {
            PhysicsEngine.MyPhysicsCollisions.Triangles[i] = triangles[i].GetCollider();
        }

        int tIndex = triangles.Length;
        for(int i=0; i < meshes.Length; i++)
        {
            meshes[i].AddColliders(PhysicsEngine.MyPhysicsCollisions.Triangles, tIndex, PhysicsEngine.MyPhysicsCollisions.BallRadius);
            tIndex += meshes[i].NumOfColliders();
        }

        SdfMeshColliderComponent[] sdfMeshes = GameObject.FindObjectsOfType<SdfMeshColliderComponent>();
        PhysicsEngine.MyPhysicsCollisions.SdfFunctions = new NativeArray<SdfFunction>(sdfMeshes.Length, Allocator.Persistent);
        for(int i=0; i < sdfMeshes.Length; i++)
        {
            PhysicsEngine.MyPhysicsCollisions.SdfFunctions[i] = sdfMeshes[i].GetSdf();
        }

        DistanceFunction distanceFunction = GameObject.FindObjectOfType<DistanceFunction>();
        if(distanceFunction)
        {
            PhysicsEngine.MyPhysicsCollisions.ScalarFieldOrigin = distanceFunction.transform.position;
        }
        else
        {
            PhysicsEngine.MyPhysicsCollisions.ScalarFieldCollision = false;
        }
    }

    private float AccTime = 0.0f;
    private float AccSpawnTime = 0.0f;
    private float AccPhysicsTime = 0.0f;
    private uint PhysicsTimeSamples = 0;
    Stopwatch Timer = new Stopwatch();
    void Update()
    {
        // Decide the number of particles to spawn
        AccSpawnTime += Time.deltaTime;
        //SpawnCounter.Count = Mathf.Min(FreeParticlesCounter.Count, (int)(AccSpawnTime * SpawnVelocity));
        SpawnCounter.Count = (int)(AccSpawnTime * SpawnVelocity);
        AccSpawnTime = AccSpawnTime % (1.0f/SpawnVelocity);
        
        PhysicsEngine.MyPhysicsCollisions.FreeParticlesCounter = FreeParticlesCounter;
        PhysicsEngine.MyPhysicsCollisions.SpawnCounter = SpawnCounter;

        float resultingTime = Time.deltaTime + AccTime;

        while(resultingTime > MaxDeltaTime*0.99f || UseRenderDeltaTime)
        {
            float deltaTime = (UseRenderDeltaTime) ? resultingTime : Mathf.Min(resultingTime, MaxDeltaTime);

            // Timer.Restart();
            // JobHandle springHandle = PhysicsEngine.MyPhysicsSprings.Schedule();
            // PhysicsEngine.MyPhysicsCollisions.DeltaTime = deltaTime;
            // springHandle.Complete();
            // Timer.Stop();
            // Debug.Log("Update Spings: " + Timer.ElapsedMilliseconds);

            // Timer.Restart();
            PhysicsEngine.MyPhysicsCollisions.DeltaTime = 0.2f * deltaTime;
            PhysicsEngine.MyPhysicsCollisions.InputParticles = InputParticles.AsDeferredJobArray();
            PhysicsEngine.MyPhysicsCollisions.OutputParticles = OutputParticles.AsDeferredJobArray();
            // JobHandle collisionsHandle = PhysicsEngine.MyPhysicsCollisions.Schedule(, 32); PARALLEL

            
            Timer.Restart();
            PhysicsEngine.MyPhysicsCollisions.Run(InputParticles.Length);
            Timer.Stop();
            if(Time.time > 20.0f && Time.time < 40.0f)
            {
                AccPhysicsTime += (float)Timer.Elapsed.TotalMilliseconds;
                PhysicsTimeSamples++;
            } else if(Time.time > 40.0f && PhysicsTimeSamples > 0)
            {
                Debug.Log("Time: " + (AccPhysicsTime/(float)PhysicsTimeSamples));
                PhysicsTimeSamples=0;
            }
            
            // Swap buffers
            {
                NativeList<PhysicsEngine.Particle> aux = InputParticles;
                InputParticles = OutputParticles;
                OutputParticles = aux;
            }

            // collisionsHandle.Complete(); PARALLEL

            // Timer.Stop();
            // Debug.Log("Check Collisions & spings: " + Timer.ElapsedMilliseconds);

            for(int i=0; i < PhysicsEngine.MyPhysicsCollisions.Spheres.Length; i++)
            {
                Colliders.Sphere s = PhysicsEngine.MyPhysicsCollisions.Spheres[i];
                s.DisVector = Vector3.zero;
                PhysicsEngine.MyPhysicsCollisions.Spheres[i] = s;
            }

            resultingTime -= deltaTime;

            if(UseRenderDeltaTime) break;
        }

        PhysicsEngine.MyUpdatePositions.Particles = InputParticles;
        JobHandle updatePositionsJob = PhysicsEngine.MyUpdatePositions.Schedule();
        updatePositionsJob.Complete();

        AccTime = resultingTime;

        SpherePositionBuffer.SetData(ParticleTransforms.ToArray());
        CylinderDataBuffer.SetData(CylindersData.ToArray());

        Graphics.DrawMeshInstancedIndirect(ParticleMesh, 0, ParticleMaterial, 
                                           new Bounds(Vector3.zero, Vector3.one * 1000.0f), 
                                           SphereArgsBuffer, receiveShadows: false, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off);

        Graphics.DrawMeshInstancedIndirect(CylinderMesh, 0, CylinderMaterial, 
                                           new Bounds(Vector3.zero, Vector3.one * 1000.0f),
                                           CylinderArgsBuffer, receiveShadows: false, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off);
    }
}
