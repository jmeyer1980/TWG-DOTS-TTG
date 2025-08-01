Shader "Custom/URPCartoonToonShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Main Texture", 2D) = "white" {}
        _RampThreshold ("Shadow Threshold", Range(0,1)) = 0.5
        _RampSmooth ("Shadow Smoothness", Range(0.001,0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _RampThreshold;
                float _RampSmooth;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float3 ToonLighting(float3 normal, float3 lightDir, float3 lightColor, float rampThreshold, float rampSmooth)
            {
                float NdotL = saturate(dot(normal, lightDir));
                float ramp = smoothstep(rampThreshold - rampSmooth, rampThreshold + rampSmooth, NdotL);
                return lerp(0.2, 1.0, ramp) * lightColor;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 lightDir = normalize(_MainLightPosition.xyz);
                float3 lightColor = _MainLightColor.rgb;

                float3 toonLight = ToonLighting(normal, lightDir, lightColor, _RampThreshold, _RampSmooth);

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float3 color = tex.rgb * _Color.rgb * toonLight;
                return float4(color, _Color.a * tex.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Forward"
}