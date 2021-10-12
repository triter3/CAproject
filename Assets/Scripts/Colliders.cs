using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public bool SolveCollision(ref PhysicsEngine.Particle p, ref Vector3 lastPos, float deltaTime, float ballRadius, float bCoeff, float fCoeff)
        {
            Vector3 v = p.Position - lastPos;
            float a = Vector3.Dot(v, v);
            float b = 2.0f * Vector3.Dot(v, lastPos - Position);
            float c = DotPosition + 
                      Vector3.Dot(lastPos, lastPos) - 
                      2.0f * Vector3.Dot(lastPos, Position) - 
                      (Radius + ballRadius) * (Radius + ballRadius);

            float inSqrt = b*b - 4.0f*a*c;
            if(inSqrt >= 0)
            {
                float outSqrt = Mathf.Sqrt(inSqrt);
                float invA = 1.0f / (2.0f*a);
                float t = Mathf.Min((-b + outSqrt) * invA, (-b - outSqrt) * invA);
                if(t >= 0.0f && t <= 1.0f)
                {
                    Vector3 normal = (lastPos + v * t - Position).normalized;
                    float dp = Vector3.Dot(normal, p.Position - Position) - ballRadius;
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
}
