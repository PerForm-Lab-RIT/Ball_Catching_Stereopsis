
Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            #include "UnityCG.cginc"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            //struct v2f
            //{
            //    float2 uv : TEXCOORD0;
            //    UNITY_FOG_COORDS(1)
            //    float4 vertex : SV_POSITION;
            //};

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR0;
            };

            //sampler2D _MainTex;
            //float4 _MainTex_ST;

            //v2f vert (appdata v)
            //{
            //    v2f o;
            //    o.vertex = UnityObjectToClipPos(v.vertex);
            //    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            //    UNITY_TRANSFER_FOG(o,o.vertex);
            //    return o;
            //}

            //fixed4 frag (v2f i) : SV_Target
            //{
            //    // sample the texture
            //    fixed4 col = float4(0.0, 1.0, 0.0, 1.0);
            //    // apply fog
            //    UNITY_APPLY_FOG(i.fogCoord, col);
            //    return col;
            //}

            StructuredBuffer<float4> objectBuffer;
            StructuredBuffer<float> SizeBuffer;
            StructuredBuffer<float> lifeTimeBuffer; // THe buffer which keeps the remainning lifeTime;


            v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);

                float4 object_pos = objectBuffer[instanceID];
                
                // Scaling
                v.vertex.xyz *= SizeBuffer[instanceID];

                // World Position
                float4 worldVertexPos = float4(v.vertex.xyz, 1.0) + object_pos;

                // Apply Camera's Inverse Transformation
                // float4 cameraRelativePos = mul(_CameraInverseMatrix, worldVertexPos);
                // cameraRelativePos.z *= -1;

                // LifeTime Test
                float lifeTime = lifeTimeBuffer[instanceID];

                float normalized_lt = lifeTime / 5.0f;

                // Project to clip space
                o.pos = UnityObjectToClipPos(worldVertexPos);
                
                o.color = float4(1.0f , 1.0f, 1.0f, 1.0f);

                

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return i.color;
            }

            ENDCG
        }
    }
}
