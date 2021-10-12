using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float Speed = 5.0f;
    public float MouseSpeed = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            if(Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
            } 
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        float h = MouseSpeed * Input.GetAxis("Mouse X");
        float v = MouseSpeed * Input.GetAxis("Mouse Y");
        transform.eulerAngles += new Vector3(-v, h, 0.0f);


        Vector3 dir = Vector3.zero;
        if(Input.GetKey(KeyCode.A))
        {
            dir.x += -1;
        }
        if(Input.GetKey(KeyCode.D))
        {
            dir.x += 1;
        }

        if(Input.GetKey(KeyCode.W))
        {
            dir.z += 1;
        }
        if(Input.GetKey(KeyCode.S))
        {
            dir.z += -1;
        }

        if(Input.GetKey(KeyCode.LeftShift))
        {
            dir.y -= 1;
        }
        if(Input.GetKey(KeyCode.Space))
        {
            dir.y += 1;
        }
        dir = dir.normalized;

        dir = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f) * dir;
        transform.position += dir * (Time.deltaTime*Speed);
    }
}
