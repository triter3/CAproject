using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

public struct PhysicsEngine : IJobParallelFor
{

    public struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Lifetime;
        public uint seed;
    }

    // System Particles
    public NativeArray<Particle> Particles;
    public NativeArray<Vector4> Output;
    public float BallRadius;
    public float BouncingCoeff;
    public float FrictionCoeff;
    public Spawn.Spawns SpawnType;

    // System Colliders
    [ReadOnly]
    public NativeArray<Colliders.Plane> Planes;
    [ReadOnly]
    public NativeArray<Colliders.Sphere> Spheres;
    [ReadOnly]
    public NativeArray<Colliders.Triangle> Triangles;

    // Scalar field collisions
    public Vector3 ScalarFieldOrigin;
    public bool ScalarFieldCollision;

    public float MaxDeltaTime;
    public float DeltaTime;

    public Vector3 ConstantForce;
    public float Drag;

    public NativeCounter.Concurrent FreeParticlesCounter;
    public NativeCounter.Concurrent SpawnCounter;

    private Vector3 GetForce(ref Particle p)
    {
        return ConstantForce - Drag*p.Velocity;
    }
    
    public void Execute(int particleId)
    {
        // Update position and velocity
        Particle p = Particles[particleId];
        if(p.Lifetime < 0.0f)
        {
            float spawnIndex = SpawnCounter.Decrement();
            if(spawnIndex < 0) 
            {
                Output[particleId] = Vector4.zero;
                return;
            }
            Spawn.InitParticle(SpawnType, ref p);
        }
        p.Lifetime -= DeltaTime;

        // Time Loop
        float resultingTime = DeltaTime;
        while(resultingTime > MaxDeltaTime*0.99f)
        {
            float deltaTime = Mathf.Min(resultingTime, MaxDeltaTime);
            
            Vector3 lastPos = p.Position;
            p.Position += p.Velocity * deltaTime + GetForce(ref p) * deltaTime * deltaTime;
            p.Velocity = (p.Position - lastPos) / deltaTime;
            // p.Position += p.Velocity + ConstantForce * deltaTime * deltaTime;
            // p.Velocity = p.Position - lastPos;

            // Check for plane collisions
            bool collision = true;
            int it = 0;
            while(it < 3 && collision)
            {
                collision = false;
                for(int i=0; i < Planes.Length && !collision; i++)
                {
                    collision = Planes[i].SolveCollision(ref p, BallRadius, BouncingCoeff, FrictionCoeff);
                }

                for(int i=0; i < Spheres.Length && !collision; i++)
                {
                    collision = Spheres[i].SolveCollision(ref p, ref lastPos, deltaTime, BallRadius, BouncingCoeff, FrictionCoeff);
                }

                for(int i=0; i < Triangles.Length && !collision; i++)
                {
                    collision = Triangles[i].SolveCollision(ref p, ref lastPos, deltaTime, BallRadius, BouncingCoeff, FrictionCoeff);
                }

                if(ScalarFieldCollision)
                {
                    Vector3 dir = p.Position - lastPos;
                    Vector3 nDir = dir.normalized;
                    float d = DistanceFunction.Evaluate(p.Position + nDir*BallRadius - ScalarFieldOrigin);

                    if(d <= 0.0f)
                    {
                        Vector3 lp = lastPos - ScalarFieldOrigin;
                        dir += nDir*BallRadius;

                        float t = 0.5f;
                        float size = 0.25f;
                        int searchIt = 0;
                        float lastD = 1.0f;
                        float lastT = t;
                        while(searchIt < 3 && math.abs(lastD) > 0.0005f)
                        {   
                            lastT = t;
                            lastD = DistanceFunction.Evaluate(lp + dir*t);
                            t += (lastD < 0.0f) ? -size : size;                        
                            size *= 0.5f;
                            searchIt++;
                        }

                        const float offset = 0.0001f;
                        Vector3 pos = lp + dir*lastT;

                        Vector3 normal = new Vector3(
                            DistanceFunction.Evaluate(pos + new Vector3(offset, 0.0f, 0.0f)) - lastD,
                            DistanceFunction.Evaluate(pos + new Vector3(0.0f, offset, 0.0f)) - lastD,
                            DistanceFunction.Evaluate(pos + new Vector3(0.0f, 0.0f, offset)) - lastD
                        ).normalized;

                        float dp = Vector3.Dot(normal, p.Position - pos - ScalarFieldOrigin) - BallRadius;
                        if(dp < 0)
                        {
                            p.Position += normal*(-(1.0f + BouncingCoeff)*dp);
                            p.Velocity += normal*(-(1.0f + BouncingCoeff - FrictionCoeff)*math.dot(normal, p.Velocity)) - FrictionCoeff*p.Velocity;
                            collision = true;
                        } 
                        else
                        {
                            collision = false;
                        }
                        
                    }
                }

                it++;
            }

            resultingTime -= deltaTime;
        }

        // Update Data
        Particles[particleId] = p;
        Output[particleId] = new Vector4(p.Position.x, p.Position.y, p.Position.z, BallRadius);
    }
}
