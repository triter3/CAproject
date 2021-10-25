using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateHair : MonoBehaviour
{
    public ParticleSimulator Simulator;
    public int NumHairs = 0;
    public float Length;
    public int NumParticles;
    public float Ke;
    public float Kd;
    public int LinkLevel = 1;

    List<KeyValuePair<int, Vector3>> HairCapilars = new List<KeyValuePair<int, Vector3>>();

    // Start is called before the first frame update
    void Awake()
    {
        System.Random r = new System.Random();
        float radius = transform.localScale.x * 0.5f;
        for(int h=0; h < NumHairs; h++)
        {
            float a = Mathf.PI/16.0f + ((float) r.NextDouble()) * Mathf.PI/3.0f;
            float b = Mathf.PI/2.0f + (((float) r.NextDouble()) * 2.0f - 1.0f) * Mathf.PI/1.8f;
            Vector3 dis = new Vector3(Mathf.Sin(a)*Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a)*Mathf.Sin(b));
            int pId = Simulator.AddParticle(transform.position + dis * radius, float.PositiveInfinity);
            HairCapilars.Add(new KeyValuePair<int, Vector3>(pId, dis * radius));
            RopeComponent.CreateRope(Simulator, transform.position + dis * radius, dis, Length, NumParticles, Ke, Kd, LinkLevel, pId);
        }
    }

    public void Transform()
    {
        for(int p=0; p < HairCapilars.Count; p++)
        {
            Simulator.SetParticlePosition(HairCapilars[p].Key, transform.position + transform.rotation * HairCapilars[p].Value);
        }
    }
}
