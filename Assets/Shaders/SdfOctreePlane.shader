Shader "Unlit/SdfOctreePlane"
{
    Properties
    {
        //_MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 tPos : TEXCOORD0;
            };

            StructuredBuffer<uint> octreeData;
            float4x4 octreeTransform;
            float3 startGridSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.tPos = mul(octreeTransform, mul(unity_ObjectToWorld, v.vertex));
                //o.tPos = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            // const uint isLeafMask = 1 << 31;
            // const uint childrenIndexMask = ~(1 << 31);

            uint roundFloat(float a)
            {
                return (a >= 0.5) ? 1 : 0;
            }

            float getDistance(float3 p)
            {
                float3 fracPart = p * startGridSize;
                int3 arrayPos = int3(floor(fracPart));
                fracPart = frac(fracPart);

                if(arrayPos.x < 0 || arrayPos.y < 0 || arrayPos.z < 0 ||
                   arrayPos.x >= startGridSize.x || arrayPos.y >= startGridSize.y || arrayPos.z >= startGridSize.z)
                {
                    float3 q = abs(p - 0.5) - 0.5;
                    return length(max(q, float3(0.0, 0.0, 0.0))) + 0.2;
                }

                int index = arrayPos.z * int(startGridSize.y * startGridSize.x) +
                            arrayPos.y * int(startGridSize.x) +
                            arrayPos.x;
                uint currentNode = octreeData[index];

                while((currentNode & (1 << 31)) == 0)
                {
                    uint childIdx = (roundFloat(fracPart.z) << 2) + 
                                    (roundFloat(fracPart.y) << 1) + 
                                     roundFloat(fracPart.x);

                    currentNode = octreeData[(currentNode & (~(1 << 31))) + childIdx];
                    fracPart = frac(2.0 * fracPart);
                }

                uint vIndex = currentNode & (~(1 << 31));

                return 0.0
                    + asfloat(octreeData[vIndex + 0]) + asfloat(octreeData[vIndex + 1]) * fracPart[0] + asfloat(octreeData[vIndex + 2]) * fracPart[0] * fracPart[0] + asfloat(octreeData[vIndex + 3]) * fracPart[0] * fracPart[0] * fracPart[0] + asfloat(octreeData[vIndex + 4]) * fracPart[1] + asfloat(octreeData[vIndex + 5]) * fracPart[0] * fracPart[1] + asfloat(octreeData[vIndex + 6]) * fracPart[0] * fracPart[0] * fracPart[1] + asfloat(octreeData[vIndex + 7]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] + asfloat(octreeData[vIndex + 8]) * fracPart[1] * fracPart[1] + asfloat(octreeData[vIndex + 9]) * fracPart[0] * fracPart[1] * fracPart[1] + asfloat(octreeData[vIndex + 10]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] + asfloat(octreeData[vIndex + 11]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] + asfloat(octreeData[vIndex + 12]) * fracPart[1] * fracPart[1] * fracPart[1] + asfloat(octreeData[vIndex + 13]) * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] + asfloat(octreeData[vIndex + 14]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] + asfloat(octreeData[vIndex + 15]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1]
                    + asfloat(octreeData[vIndex + 16]) * fracPart[2] + asfloat(octreeData[vIndex + 17]) * fracPart[0] * fracPart[2] + asfloat(octreeData[vIndex + 18]) * fracPart[0] * fracPart[0] * fracPart[2] + asfloat(octreeData[vIndex + 19]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[2] + asfloat(octreeData[vIndex + 20]) * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 21]) * fracPart[0] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 22]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 23]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 24]) * fracPart[1] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 25]) * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 26]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 27]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 28]) * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 29]) * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 30]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] + asfloat(octreeData[vIndex + 31]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2]
                    + asfloat(octreeData[vIndex + 32]) * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 33]) * fracPart[0] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 34]) * fracPart[0] * fracPart[0] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 35]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 36]) * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 37]) * fracPart[0] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 38]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 39]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 40]) * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 41]) * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 42]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 43]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 44]) * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 45]) * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 46]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 47]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2]
                    + asfloat(octreeData[vIndex + 48]) * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 49]) * fracPart[0] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 50]) * fracPart[0] * fracPart[0] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 51]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 52]) * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 53]) * fracPart[0] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 54]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 55]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 56]) * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 57]) * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 58]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 59]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 60]) * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 61]) * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 62]) * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2] + asfloat(octreeData[vIndex + 63]) * fracPart[0] * fracPart[0] * fracPart[0] * fracPart[1] * fracPart[1] * fracPart[1] * fracPart[2] * fracPart[2] * fracPart[2];
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float dist = getDistance(i.tPos);
                float val = (dist < 0.0) ? 0.0 : 1.0;
                fixed4 col = fixed4(val, val, val, 1.0);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
