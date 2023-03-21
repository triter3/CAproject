Shader "Unlit/CylinderShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        //_MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members normal)
#pragma exclude_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 normal;
            };

            // sampler2D _MainTex;
            // float4 _MainTex_ST;
            fixed4 _Color;

            struct VertexData
            {
                float4 position;
                float3 normal;
            }

            StructuredBuffer<VertexData> Vertices;

            v2f vert (uint vertex_id : SV_VertexID)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(Vertices[vertex_id].position);
                o.normal = UnityObjectToWorldNormal(Vertices[vertex_id].normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
