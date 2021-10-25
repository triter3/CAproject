using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OcillatePosition : MonoBehaviour
{
    public float Frequency;
    public float Amplitude;

    private Vector3 InitPos;
    void Start()
    {
        InitPos = transform.position;
    }

    // Update is called once per frame
    float AccTime = 0.0f;
    void Update()
    {
        AccTime += Time.deltaTime;
        Vector3 pos = transform.position;
        pos.y = InitPos.y + Amplitude*Mathf.Sin(Frequency*AccTime);
        transform.position = pos;
    }
}

