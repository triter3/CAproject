using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpawnFunction
{
    void InitParticle(ref PhysicsEngine.Particle p);
}
