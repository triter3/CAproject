using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereColliderComponent : MonoBehaviour
{
    void Awake() {}
    
    protected int ColliderId;

    public Colliders.Sphere GetCollider(int colliderId)
    {
        ColliderId = colliderId;
        Colliders.Sphere s = new Colliders.Sphere();
        s.Position = transform.position;
        s.DotPosition = Vector3.Dot(s.Position, s.Position);
        s.Radius = transform.localScale.x * 0.5f;
        s.DisVector = Vector3.zero;
        return s;
        
    }
}
