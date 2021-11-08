using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public ParticleSimulator Simulator;
    public float MaxVelocity = 10.0f;
    public float MaxAngularVelocity = 5.0f;

    private CreateHair CreateHair = null;
    private int ColliderId;
    private void Awake()
    {
        CreateHair = GetComponent<CreateHair>();
        ColliderId = GetComponent<SphereColliderComponent>().ColliderId;
    }

    float Velocity = 0.0f;
    float AngularVelocity = 0.0f;
    void Update()
    {
        if(Input.GetKey(KeyCode.UpArrow)) 
        {
            Velocity = Mathf.Lerp(Velocity, MaxVelocity, Time.deltaTime * 3.0f);
        }
        else if(Input.GetKey(KeyCode.DownArrow)) 
        {
            Velocity = Mathf.Lerp(Velocity, -MaxVelocity, Time.deltaTime * 3.0f);
        }
        else
        {
            Velocity = Mathf.Lerp(Velocity, 0.0f, Time.deltaTime * 6.0f);
        }

        if(Input.GetKey(KeyCode.RightArrow)) 
        {
            AngularVelocity = Mathf.Lerp(AngularVelocity, MaxAngularVelocity, Time.deltaTime * 3.0f);
        }
        else if(Input.GetKey(KeyCode.LeftArrow))
        {
            AngularVelocity = Mathf.Lerp(AngularVelocity, -MaxAngularVelocity, Time.deltaTime * 3.0f);
        }
        else
        {
            AngularVelocity = Mathf.Lerp(AngularVelocity, 0.0f, Time.deltaTime * 6.0f);
        }

        transform.position += -transform.forward * (Velocity * Time.deltaTime);
        transform.eulerAngles += new Vector3(0.0f, AngularVelocity*Time.deltaTime, 0.0f);

        CreateHair.Transform();
        Simulator.SetSphereColliderPosition(ColliderId, transform.position);
    }
}
