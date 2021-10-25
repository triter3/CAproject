using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// public static class SpawnTest
// {
//     public static void InitParticle(ref PhysicsEngine.Particle p)
//     {
//         p.Position = new Vector3(0.0f, 6.0f, 0.0f);
//         p.Velocity = Vector3.zero;
//         p.Lifetime = 30.0f;
//     }
// }

public static class Spawn
{
    public enum Spawns
    {
        Stairs,
        Rain,
        Fall
    }

    public static void InitParticle(Spawns s, ref PhysicsEngine.Particle p)
    {
        switch(s)
        {
            case Spawns.Stairs:
                SpawnStairs.InitParticle(ref p);
                break;
            case Spawns.Rain:
                SpawnRain.InitParticle(ref p);
                break;
            case Spawns.Fall:
                SpawnFall.InitParticle(ref p);
                break;
        }
    }
}

public static class SpawnStairs
{
    private const float MaxValue = (float)uint.MaxValue;

    public static float GetRandom(ref uint seed)
    {
        seed = 214013 * seed + 2531011;
        return (float)seed / MaxValue;
    }

    public static void InitParticle(ref PhysicsEngine.Particle p)
    {
        p.Position = new Vector3(-4.921f, 5.5f, -4.4f);
        float a = 2.0f + GetRandom(ref p.Seed)*0.4f;
        float b = GetRandom(ref p.Seed)*0.3f;
        p.Velocity = new Vector3(-a, 0.0f, b);
        p.Lifetime = 10.0f;
    }
}

public static class SpawnRain
{
    private const float MaxValue = (float)uint.MaxValue;

    public static float GetRandom(ref uint seed)
    {
        seed = 214013 * seed + 2531011;
        return (float)seed / MaxValue;
    }

    public static void InitParticle(ref PhysicsEngine.Particle p)
    {
        p.Position = new Vector3(0.0f, 6.0f, 0.0f);
        float a = GetRandom(ref p.Seed)*Mathf.PI/8.0f;
        float b = GetRandom(ref p.Seed)*2.0f*Mathf.PI;
        p.Velocity = new Vector3(Mathf.Sin(a)*Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a)*Mathf.Sin(b)) * 7.0f;
        //Debug.Assert(Mathf.Abs(p.Velocity.magnitude-7.0f) < 0.001f, "not unitary vector");
        p.Lifetime = 30.0f;
    }
}

public static class SpawnFall
{
    private const float MaxValue = (float)uint.MaxValue;

    public static float GetRandom(ref uint seed)
    {
        seed = 214013 * seed + 2531011;
        return (float)seed / MaxValue;
    }

    public static void InitParticle(ref PhysicsEngine.Particle p)
    {
        p.Position = new Vector3(0.0f, 8.0f, 0.0f);
        float a = Mathf.PI/10.0f;
        float b = GetRandom(ref p.Seed)*2.0f*Mathf.PI;
        p.Velocity = new Vector3(Mathf.Sin(a)*Mathf.Cos(b), -Mathf.Cos(a), Mathf.Sin(a)*Mathf.Sin(b)) * 2.0f;
        //Debug.Assert(Mathf.Abs(p.Velocity.magnitude-7.0f) < 0.001f, "not unitary vector");
        p.Lifetime = 10.0f;
    }
}
