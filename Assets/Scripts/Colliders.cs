using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Colliders
{
    public struct Plane
    {
        public Vector3 Normal;
        public float D;

        public readonly bool SolveCollision(ref PhysicsEngine.Particle p, float ballRadius, float bCoeff, float fCoeff)
        {
            float dp = Vector3.Dot(Normal, p.Position) - ballRadius;
            if(dp + D < 0)
            {
                p.Position += Normal*(-(1.0f + bCoeff)*(dp + D));
                p.Velocity += Normal*(-(1.0f + bCoeff - fCoeff)*Vector3.Dot(Normal, p.Velocity)) - fCoeff*p.Velocity;
                return true;
            }
            return false;
        }
    }
    
    public struct Sphere
    {
        public Vector3 Position;
        public float DotPosition;
        public float Radius;
        public Vector3 DisVector;

        public bool SolveCollision(ref PhysicsEngine.Particle p, ref Vector3 lp, float deltaTime, float ballRadius, float bCoeff, float fCoeff)
        {
            float sqRadius = (Radius + ballRadius) * (Radius + ballRadius);
            float currentRadius = Vector3.Dot(p.Position - Position, p.Position - Position);
            if(currentRadius > sqRadius)
            {
                return false;
            }

            Vector3 pos = p.Position;
            Vector3 lastPos = lp + DisVector;
            Vector3 v = pos - lastPos;
            float a = Vector3.Dot(v, v);
            if(a > 1e-7)
            {
                float b = 2.0f * Vector3.Dot(v, lastPos - Position);
                float c = DotPosition + 
                        Vector3.Dot(lastPos, lastPos) - 
                        2.0f * Vector3.Dot(lastPos, Position) - 
                        sqRadius;

                float inSqrt = b*b - 4.0f*a*c;
                if(inSqrt >= 0)
                {
                    float outSqrt = Mathf.Sqrt(inSqrt);
                    float invA = 1.0f / (2.0f*a);
                    float t = Mathf.Min((-b + outSqrt) * invA, (-b - outSqrt) * invA);
                    if(t <= 1.0f)
                    {
                        Vector3 normal = (lastPos + v * t - Position).normalized;
                        float dp = Vector3.Dot(normal, pos - Position) - ballRadius;
                        p.Position += normal*(-(1.0f + bCoeff)*(dp - Radius));
                        p.Velocity += normal*(-(1.0f + bCoeff - fCoeff)*Vector3.Dot(normal, p.Velocity)) - fCoeff*p.Velocity;
                        return true;
                    }
                }
            }
            else
            {
                float t = currentRadius / sqRadius;

                if(t <= 1.0f)
                {
                    Vector3 normal = (pos - Position) / Mathf.Sqrt(currentRadius);
                    float dp = Vector3.Dot(normal, pos - Position) - ballRadius;
                    p.Position += normal*(-(1.0f + bCoeff)*(dp - Radius));
                    p.Velocity += normal*(-(1.0f + bCoeff - fCoeff)*Vector3.Dot(normal, p.Velocity)) - fCoeff*p.Velocity;
                    return true;
                }
            }
            return false;
        }
    }

    public struct Triangle
    {
        // Triangle defined by points A, B, C
        public Vector3 A;
        public Vector3 V; // B-A
        public Vector3 W; // C-A
        public Vector3 N; // cross(V, W)
        public float Nlength;

        // Barycenter offset
        public float minUOffset;
        public float minVOffset;
        public float uOffset;
        public float vOffset;
        public float uvOffset;


        public Vector3 Normal; // triangle normal
        public float D; // plane component D

        public bool SolveCollision(ref PhysicsEngine.Particle p, ref Vector3 lastPos, float deltaTime, float ballRadius, float bCoeff, float fCoeff)
        {
            Vector3 dir = p.Position - lastPos;
            Vector3 OA = lastPos - A - Normal*ballRadius;
            // Vector3 OA = lastPos - A;
            float depthLP = Vector3.Dot(OA, N);
            float depthP = Vector3.Dot(OA + dir, N);
            
            if(depthLP * depthP < 1e-6)
            {
                
                float det = Vector3.Dot(dir, N);
                float invDet = 1.0f / det;
                Vector3 DOA = Vector3.Cross(OA, dir);

                float u = -Vector3.Dot(W, DOA) * invDet;
                float v = Vector3.Dot(V, DOA) * invDet;
                float t = -depthLP * invDet;

                if(det <= 1e-6 && u >= -minUOffset && v >= -minVOffset && 
                   (u*vOffset + v*uOffset) <= uvOffset &&
                   t >= 0.0f && t <= 1.0f)
                {
                    float dp = Vector3.Dot(Normal, p.Position) - ballRadius;
                    // float dp = Vector3.Dot(Normal, p.Position);
                    p.Position += Normal*(-(1.0f + bCoeff)*(dp + D));
                    p.Velocity += Normal*(-(1.0f + bCoeff - fCoeff)*Vector3.Dot(Normal, p.Velocity)) - fCoeff*p.Velocity;
                    return true;
                }
            }

            return false;
        }
    }

    public static bool SolveSdfCollision(SdfFunction sdfFunction, ref PhysicsEngine.Particle p, ref Vector3 lastPos, 
                                         float deltaTime, float ballRadius, float bCoeff, float fCoeff)
    {
        Vector3 dir = p.Position - lastPos;
        Vector3 nDir = dir.normalized;
        // float d = DistanceFunction.Evaluate(p.Position + nDir*BallRadius - ScalarFieldOrigin);
        float d = sdfFunction.GetDistance(p.Position);

        if(d >= ballRadius) return false;

        // Vector3 lp = lastPos;

        // float t = 0.5f;
        // float size = 0.25f;
        // int searchIt = 0;
        // float lastD = float.PositiveInfinity;
        // float lastT = t;
        // while(searchIt < 10 && math.abs(lastD - ballRadius) > 5e-4f)
        // {   
        //     lastT = t;
        //     // lastD = DistanceFunction.Evaluate(lp + dir*t);
        //     lastD = sdfFunction.GetDistance(lp + dir * t);
        //     t += (lastD < ballRadius) ? -size : size;                        
        //     size *= 0.5f;
        //     searchIt++;
        // }

        Vector3 pos = p.Position;
        Vector3 gradient = Vector3.zero;
        float lastD = d;
        int searchIt = 0;
        while(searchIt < 10 && lastD - ballRadius < 0.0f)
        {
            pos -= nDir * (ballRadius - lastD);
            lastD = sdfFunction.GetDistance(pos, out gradient);
            searchIt++;
        }

        const float offset = 0.0001f;
        // Vector3 pos = lp + dir*lastT;

        // Vector3 normal = new Vector3(
        //     DistanceFunction.Evaluate(pos + new Vector3(offset, 0.0f, 0.0f)) - lastD,
        //     DistanceFunction.Evaluate(pos + new Vector3(0.0f, offset, 0.0f)) - lastD,
        //     DistanceFunction.Evaluate(pos + new Vector3(0.0f, 0.0f, offset)) - lastD
        // ).normalized;

        Vector3 normal = gradient;
        // Vector3 normal = new Vector3(
        //     sdfFunction.GetDistance(pos + new Vector3(offset, 0.0f, 0.0f)) - lastD,
        //     sdfFunction.GetDistance(pos + new Vector3(0.0f, offset, 0.0f)) - lastD,
        //     sdfFunction.GetDistance(pos + new Vector3(0.0f, 0.0f, offset)) - lastD
        // ).normalized;

        // if((gradient - normal).magnitude > 1e-5)
        // {
        //     Debug.Log("Error with the gradient");
        //     Debug.Log(normal.x + " // " + normal.y + " // " + normal.z);
        //     Debug.Log(gradient.x + " // " + gradient.y + " // " + gradient.z);
        // }

        float dp = math.dot(normal, p.Position - pos);
        if(dp < 0)
        {
            p.Position += normal*(-(1.0f + bCoeff) * dp);
            p.Velocity += normal*(-(1.0f + bCoeff - fCoeff) * math.dot(normal, p.Velocity)) - fCoeff * p.Velocity;
            return true;
        } 
        else
        {
            return false;
        }
    }
}
