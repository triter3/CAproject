Shader "Custom/InstancingCylinderShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };


        struct CylinderData
        {
            float4 position; // position + radius
            float4 direction; // direction + length
        };

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<CylinderData> _InstancesData;
        #endif

        // float4x4 inverse(float4x4 input)
        // {
        // #define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
        
        //     float4x4 cofactors = float4x4(
        //         minor(_22_23_24, _32_33_34, _42_43_44),
        //         -minor(_21_23_24, _31_33_34, _41_43_44),
        //         minor(_21_22_24, _31_32_34, _41_42_44),
        //         -minor(_21_22_23, _31_32_33, _41_42_43),
        
        //         -minor(_12_13_14, _32_33_34, _42_43_44),
        //         minor(_11_13_14, _31_33_34, _41_43_44),
        //         -minor(_11_12_14, _31_32_34, _41_42_44),
        //         minor(_11_12_13, _31_32_33, _41_42_43),
        
        //         minor(_12_13_14, _22_23_24, _42_43_44),
        //         -minor(_11_13_14, _21_23_24, _41_43_44),
        //         minor(_11_12_14, _21_22_24, _41_42_44),
        //         -minor(_11_12_13, _21_22_23, _41_42_43),
        
        //         -minor(_12_13_14, _22_23_24, _32_33_34),
        //         minor(_11_13_14, _21_23_24, _31_33_34),
        //         -minor(_11_12_14, _21_22_24, _31_32_34),
        //         minor(_11_12_13, _21_22_23, _31_32_33)
        //         );
        // #undef minor
        //     return transpose(cofactors) / determinant(input);
        // }

        void setup()
        {
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            CylinderData data = _InstancesData[unity_InstanceID];

            unity_ObjectToWorld._11_21_31_41 = float4(data.position.w, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, data.position.w, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.direction.w, 0);
            unity_ObjectToWorld._14_24_34_44 = float4(0, 0, 0, 1);

            float3 tangent;
            if(abs(abs(data.direction.x)-1) < 0.00001f)
            {
                tangent = normalize(cross(data.direction.xyz, float3(0, 1, 0)));
            }
            else
            {
                tangent = normalize(cross(data.direction.xyz, float3(1, 0, 0)));
            }

            unity_ObjectToWorld = mul(transpose(float4x4(float4(tangent, 0), 
                                                float4(cross(data.direction.xyz, tangent), 0), 
                                                float4(data.direction.xyz, 0), 
                                                float4(data.position.xyz, 1))), 
                                      unity_ObjectToWorld);

        
            unity_WorldToObject._11_21_31_41 = float4(1.0f/data.position.w, 0, 0, 0);
            unity_WorldToObject._12_22_32_42 = float4(0, 1.0f/data.position.w, 0, 0);
            unity_WorldToObject._13_23_33_43 = float4(0, 0, 1.0f/data.direction.w, 0);
            unity_WorldToObject._14_24_34_44 = float4(0, 0, 0, 1);

            unity_WorldToObject = mul(unity_WorldToObject, float4x4(float4(tangent, -data.position.x), 
                                                            float4(cross(data.direction.xyz, tangent), -data.position.y), 
                                                            float4(data.direction.xyz, -data.position.z), 
                                                            float4(0, 0, 0, 1)));

        #endif
        }

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
