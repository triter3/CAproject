using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

public class PhysicsEngine
{
    public PhysicsSprings MyPhysicsSprings = new PhysicsSprings();
    public PhysicsCollisions MyPhysicsCollisions = new PhysicsCollisions();
    public UpdatePositions MyUpdatePositions = new UpdatePositions();

    public struct Particle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Force; // Particle Forces to apply
        public float InvMass;
        public float Lifetime;
        public uint Seed;
        public float RadiusMultiplier;
    }

    public struct Spring
    {
        public int ParticleId1;
        public int ParticleId2;
        public float TargetDistance;
        public float Ke;
        public float Kd;
        public float RadiusMultiplier;
    }

    public struct ParticleSpringsInfo
    {
        public int startIndex;
        public int NumSprings;
    }

    public struct PhysicsSprings : IJob
    {
        public NativeList<Particle> Particles;

        [ReadOnly]
        public NativeList<Spring> Springs;

        public void Execute()
        {
            for(int s=0; s < Springs.Length; s++)
            {
                Particle p1 = Particles[Springs[s].ParticleId1];
                Particle p2 = Particles[Springs[s].ParticleId2];

                Vector3 dir = p2.Position - p1.Position;
                float mag = dir.magnitude;
                dir = dir/mag;
                float totalForce = Springs[s].Ke * (mag - Springs[s].TargetDistance) + 
                                   Springs[s].Kd * Vector3.Dot(p2.Velocity - p1.Velocity, dir);

                p1.Force += totalForce * dir;
                p2.Force += (-totalForce) * dir;                

                Particles[Springs[s].ParticleId1] = p1;
                Particles[Springs[s].ParticleId2] = p2;
            }
        }

        public void Dispose()
        {
            Springs.Dispose();
        }
    }

    public struct UpdatePositions : IJob
    {
        // Input Data
        [ReadOnly]
        public NativeList<Particle> Particles;
        [ReadOnly]
        public NativeList<Spring> Springs;

        [WriteOnly]
        public NativeArray<Vector4> ParticlesOutput;
        [WriteOnly]
        public NativeArray<ParticleSimulator.CylinderData> SpringsOutput;

        public float BallRadius;

        public void Execute()
        {
            for(int i=0; i < Particles.Length; i++)
            {
                Particle p = Particles[i];
                if(p.Lifetime > 0.0f)
                {
                    ParticlesOutput[i] = new Vector4(p.Position.x, p.Position.y, p.Position.z, BallRadius*p.RadiusMultiplier);
                }
                else
                {
                    ParticlesOutput[i] = Vector4.zero;
                }
            }

            for(int i=0; i < Springs.Length; i++)
            {
                Spring s = Springs[i];
                Vector3 pos = Particles[s.ParticleId1].Position;
                Vector3 dir = Particles[s.ParticleId2].Position - pos;
                float len = dir.magnitude;
                dir = dir/len;
                ParticleSimulator.CylinderData data;
                data.position = new Vector4(pos.x, pos.y, pos.z, BallRadius*s.RadiusMultiplier);
                data.direction = new Vector4(dir.x, dir.y, dir.z, len);
                SpringsOutput[i] = data;
            }
        }
    }

    [BurstCompile]
    public struct PhysicsCollisions : IJobParallelFor
    {
        // Particles Info
        [ReadOnly]
        public NativeArray<Particle> InputParticles;
        [WriteOnly]
        public NativeArray<Particle> OutputParticles;

        // Springs affecting particles
        [ReadOnly]
        public NativeArray<ParticleSpringsInfo> ParticlesSpringsInfo;
        [ReadOnly]
        public NativeArray<Spring> ParticlesSprings;
        
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
        public Vector3 ConstantAcceleration;
        public float Drag;

        public NativeCounter.Concurrent FreeParticlesCounter;
        public NativeCounter.Concurrent SpawnCounter;

        private Vector3 GetForce(ref Particle p)
        {
            return (p.InvMass == 0 ? 0.0f : 1.0f) * ConstantAcceleration + 
                   (ConstantForce - Drag*p.Velocity + p.Force) * p.InvMass;
        }  
        
        public void Execute(int particleId)
        {
            Particle p = InputParticles[particleId];
            // Calculate spings forces
            ParticleSpringsInfo info = ParticlesSpringsInfo[particleId];
            for(int s=0; s < info.NumSprings; s++)
            {
                int id2 = ParticlesSprings[info.startIndex + s].ParticleId2;
                Vector3 dir = InputParticles[id2].Position - p.Position;
                float mag = dir.magnitude;
                dir = dir/mag;
                float totalForce = ParticlesSprings[info.startIndex + s].Ke * (mag - ParticlesSprings[info.startIndex + s].TargetDistance) + 
                                   ParticlesSprings[info.startIndex + s].Kd * Vector3.Dot(InputParticles[id2].Velocity - p.Velocity, dir);

                p.Force += totalForce * dir;
            }

            // Update position and velocity
            if(p.Lifetime < 0.0f)
            {
                float spawnIndex = SpawnCounter.Decrement();
                if(spawnIndex < 0) 
                {
                    OutputParticles[particleId] = p;
                    return;
                }
                Spawn.InitParticle(SpawnType, ref p);
                p.Force = Vector3.zero;
            }

            // Time Loop
            float resultingTime = DeltaTime;
            while(resultingTime > MaxDeltaTime*0.99f)
            {
                float deltaTime = Mathf.Min(resultingTime, MaxDeltaTime);
                
                Vector3 lastPos = p.Position;
                // Debug.Log(particleId + ": " + p.Force);
                p.Position += p.Velocity * deltaTime + GetForce(ref p) * deltaTime * deltaTime;
                p.Velocity = (p.Position - lastPos) / deltaTime;
                p.Force = Vector3.zero;

                // Check for plane collisions
                bool collision = true;
                int it = 0;
                while(it < 3 && collision && p.InvMass > 0)
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

                    // if(ScalarFieldCollision)
                    // {
                    //     Vector3 dir = p.Position - lastPos;
                    //     Vector3 nDir = dir.normalized;
                    //     float d = DistanceFunction.Evaluate(p.Position + nDir*BallRadius - ScalarFieldOrigin);

                    //     if(d <= 0.0f)
                    //     {
                    //         Vector3 lp = lastPos - ScalarFieldOrigin;
                    //         dir += nDir*BallRadius;

                    //         float t = 0.5f;
                    //         float size = 0.25f;
                    //         int searchIt = 0;
                    //         float lastD = 1.0f;
                    //         float lastT = t;
                    //         while(searchIt < 3 && math.abs(lastD) > 0.0005f)
                    //         {   
                    //             lastT = t;
                    //             lastD = DistanceFunction.Evaluate(lp + dir*t);
                    //             t += (lastD < 0.0f) ? -size : size;                        
                    //             size *= 0.5f;
                    //             searchIt++;
                    //         }

                    //         const float offset = 0.0001f;
                    //         Vector3 pos = lp + dir*lastT;

                    //         Vector3 normal = new Vector3(
                    //             DistanceFunction.Evaluate(pos + new Vector3(offset, 0.0f, 0.0f)) - lastD,
                    //             DistanceFunction.Evaluate(pos + new Vector3(0.0f, offset, 0.0f)) - lastD,
                    //             DistanceFunction.Evaluate(pos + new Vector3(0.0f, 0.0f, offset)) - lastD
                    //         ).normalized;

                    //         float dp = Vector3.Dot(normal, p.Position - pos - ScalarFieldOrigin) - BallRadius;
                    //         if(dp < 0)
                    //         {
                    //             p.Position += normal*(-(1.0f + BouncingCoeff)*dp);
                    //             p.Velocity += normal*(-(1.0f + BouncingCoeff - FrictionCoeff)*math.dot(normal, p.Velocity)) - FrictionCoeff*p.Velocity;
                    //             collision = true;
                    //         } 
                    //         else
                    //         {
                    //             collision = false;
                    //         }
                            
                    //     }
                    // }

                    it++;
                }

                resultingTime -= deltaTime;
                p.Lifetime -= deltaTime;
            }

            // Update Data
            OutputParticles[particleId] = p;
        }

        public void Dispose()
        {
            Planes.Dispose();
            Spheres.Dispose();
            Triangles.Dispose();

        }
    }
}
