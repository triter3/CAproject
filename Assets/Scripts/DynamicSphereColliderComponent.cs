using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicSphereColliderComponent : SphereColliderComponent
{
    public ParticleSimulator Simulator;

    void Update() 
    {
        Simulator.SetSphereColliderPosition(ColliderId, transform.position);
    }
}
