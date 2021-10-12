using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneColliderComponent : MonoBehaviour
{
    void Awake() {}
    
    public Colliders.Plane GetCollider()
    {
        Colliders.Plane p = new Colliders.Plane();
        p.Normal = transform.up;
        p.D = -Vector3.Dot(p.Normal, transform.position);
        return p;
    }
}
