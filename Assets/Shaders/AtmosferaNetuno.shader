Shader "Custom/AtmosferaNetuno"
{
    Properties
    {
        _Color ("Atmosphere Color", Color) = (0.05, 0.35, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.2
        _Intensity ("Intensity", Range(0.0, 5.0)) = 1.2
        _Alpha ("Alpha", Range(0.0, 1.0)) = 0.38
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float3 normalWorld : TEXCOORD0;
                float3 viewDirection : TEXCOORD1;
            };

            fixed4 _Color;
            float _RimPower;
            float _Intensity;
            float _Alpha;

            v2f vert(appdata input)
            {
                v2f output;

                output.position = UnityObjectToClipPos(input.vertex);
                output.normalWorld = UnityObjectToWorldNormal(input.normal);

                float3 worldPosition =
                    mul(unity_ObjectToWorld, input.vertex).xyz;

                output.viewDirection =
                    _WorldSpaceCameraPos.xyz - worldPosition;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 normalDirection = normalize(input.normalWorld);
                float3 viewDirection = normalize(input.viewDirection);

                float facing = saturate(
                    dot(normalDirection, viewDirection)
                );

                float rim = pow(
                    1.0 - facing,
                    _RimPower
                );

                fixed3 finalColor =
                    _Color.rgb * rim * _Intensity;

                float finalAlpha =
                    rim * _Alpha * _Color.a;

                return fixed4(finalColor, finalAlpha);
            }

            ENDCG
        }
    }

    Fallback Off
}