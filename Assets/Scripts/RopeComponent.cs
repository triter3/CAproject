using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeComponent : MonoBehaviour
{
    public ParticleSimulator Simulator;
    public ParticleComponent FirstParticle = null;
    public ParticleComponent LastParticle = null;
    public float Length;
    public int NumParticles;
    public float Ke;
    public float Kd;
    public int LinkLevel = 1;
    public bool CompensateKe;

    void Awake()
    {
        CreateRope(Simulator, transform.position, -transform.up,
                   Length, NumParticles, Ke, Kd, LinkLevel,
                   (FirstParticle) ? FirstParticle.GetId() : -1, 
                   (LastParticle) ? LastParticle.GetId() : -1, 
                   CompensateKe);
    }

    public static void CreateRope(ParticleSimulator Simulator, Vector3 pos, Vector3 dir,
                                  float Length, int NumParticles, float Ke, float Kd, int LinkLevel, 
                                  int FirstParticleId = -1, int LastParticleId = -1, bool CompensateKe = false)
    {
        float step = Length / NumParticles;
        int[] particleId = new int[NumParticles];
        if(FirstParticleId >= 0) particleId[0] = FirstParticleId;
        if(LastParticleId >= 0) particleId[NumParticles-1] = LastParticleId;
        for(int p=(FirstParticleId >= 0) ? 1 : 0; p < ((LastParticleId >= 0) ? NumParticles - 1 : NumParticles); p++)
        {
            particleId[p] = Simulator.AddParticle(pos + dir * (step * p), 1.0f);
        }

        for(int p=1; p < NumParticles; p++)
        {
            for(int n=1; n <= LinkLevel; n++)
            {
                if(p - n >= 0)
                {
                    float inc = CompensateKe ? NumParticles - p : 1; 
                    Simulator.AddSpring(particleId[p], particleId[p-n], n*step, Ke*inc, Kd, n == 1);        
                }
            }
        }
    }
}
