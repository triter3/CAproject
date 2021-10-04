using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct PhysicsEngine : IJobParallelFor
{
    public struct Particle
    {
        public Vector3 Position;
        public Vector3 LastPosition;
        public Vector3 Velocity;
    }

    public NativeArray<Particle> Particles;
    public NativeArray<Matrix4x4> Output;

    public float DeltaTime;

    public Vector3 ConstantForce;

    private float BallRadius;
    
    public void Execute(int particleId)
    {
        // Update position and velocity
        Particle p = Particles[particleId];
        Vector3 pos = p.Position;
        p.Position += (p.Position - p.LastPosition) + ConstantForce * DeltaTime * DeltaTime;
        p.LastPosition = pos;

        // Check for collisions


        // Update Data
        Particles[particleId] = p;
        Output[particleId] = Matrix4x4.Translate(p.Position);
    }
}
