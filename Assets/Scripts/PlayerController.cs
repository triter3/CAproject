using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float MaxVelocity = 10.0f;
    public float MaxAngularVelocity = 5.0f;

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
            AngularVelocity = Mathf.Lerp(AngularVelocity, 0.0f, Time.deltaTime * 3.0f);
        }

        transform.position += -transform.right * (Velocity * Time.deltaTime);
        transform.eulerAngles += new Vector3(0.0f, AngularVelocity*Time.deltaTime, 0.0f);
    }
}
