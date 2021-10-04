using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

public class ParticleSimulator : MonoBehaviour
{
    public Mesh ParticleMesh;
    public Material ParticleMaterial;
    public int NumParticles = 1;

    private NativeArray<PhysicsEngine.Particle> ParticleProperties;
    private NativeArray<Matrix4x4> ParticleTransforms;
    private PhysicsEngine PhysicsEngine;

    void Awake() 
    {
        ParticleProperties = new NativeArray<PhysicsEngine.Particle>(NumParticles, Allocator.Persistent);
        ParticleTransforms = new NativeArray<Matrix4x4>(NumParticles, Allocator.Persistent);
        for(int i=0; i < NumParticles; i++)
        {
            PhysicsEngine.Particle p;
            p.Position = Vector3.zero;
            p.LastPosition = Vector3.zero;
            p.Velocity = Vector3.zero;
            ParticleProperties[i] = p;
        }

        PhysicsEngine = new PhysicsEngine();
        PhysicsEngine.Particles = ParticleProperties;
        PhysicsEngine.Output = ParticleTransforms;
        PhysicsEngine.ConstantForce = new Vector3(0.0f, -9.8f, 0.0f);
        PhysicsEngine.DeltaTime = 0.01f;
    }

    void OnEnable() 
    {
        
    }

    void OnDisable() 
    {
        ParticleProperties.Dispose();
        ParticleTransforms.Dispose();    
    }

    void Update()
    {
        JobHandle handle = PhysicsEngine.Schedule(NumParticles, 1);
        
        handle.Complete();

        Graphics.DrawMeshInstanced(ParticleMesh, 0, ParticleMaterial, ParticleTransforms.ToArray(), ParticleTransforms.Length);
    }
}
