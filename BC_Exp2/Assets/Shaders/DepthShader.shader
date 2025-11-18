Shader "Unlit/DepthShader"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }


            sampler2D _CameraDepthTexture;
            float _CameraFarClipPlane;
            float _CameraNearClipPlane;

            float4 frag (v2f i) : SV_Target
            {
                //float depthValue = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r) / _CameraFarClipPlane;
                float depth = tex2D(_CameraDepthTexture, i.uv).r;
                // float linearDepth = LinearEyeDepth(depth) * 1.0;
                // float normalizedDepth = (linearDepth) / (_CameraFarClipPlane * 1.0);
                

                return float4(depth, depth, depth, 1.0);
            }
            ENDCG
        }
    }
}
