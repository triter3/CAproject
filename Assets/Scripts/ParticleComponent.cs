using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ParticleComponent : MonoBehaviour
{
    public ParticleSimulator Simulator;
    public float Mass;
    public bool Active;
    public bool SpringToNearParticleAtStart;
    public float Ke;
    public float Kd;

    private int Id = -1;

    void Awake() 
    {
        if(Id < 0)
        {
            Id = Simulator.AddParticle(transform.position, Mass, Active);
            if(SpringToNearParticleAtStart)
            {
                Simulator.AddSpringToNearParticle(Id, Ke, Kd);
            }
        }
    }

    public int GetId()
    {
        Awake();
        return Id;
    }

    protected virtual void Move() { }

    void Update()
    {
        if(Mass == float.PositiveInfinity)
        {
            Move();
            Simulator.SetParticlePosition(Id, transform.position);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ParticleComponent))]
public class LevelScriptEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        ParticleComponent myTarget = (ParticleComponent)target;

        //myTarget.Simulator = EditorGUILayout.ObjectField("Particle Simulator", myTarget.Simulator, typeof(ParticleSimulator)) as ParticleSimulator;
        myTarget.Mass = EditorGUILayout.FloatField("Mass", myTarget.Mass);
        bool newResult = EditorGUILayout.Toggle("Active", myTarget.Active);

        myTarget.Active = newResult;
        myTarget.SpringToNearParticleAtStart = EditorGUILayout.Toggle("Create spring at start", myTarget.SpringToNearParticleAtStart);
        if(GUILayout.Button("Add Spring to Near Particle"))
        {
            myTarget.Simulator.AddSpringToNearParticle(myTarget.GetId(), myTarget.Ke, myTarget.Kd);
        }

        myTarget.Ke = EditorGUILayout.FloatField("Ke", myTarget.Ke);
        myTarget.Kd = EditorGUILayout.FloatField("Kd", myTarget.Kd);
    }
}
#endif