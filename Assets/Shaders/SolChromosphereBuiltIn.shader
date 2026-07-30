Shader "Custom/Sun/ChromosphereBuiltIn"
{
    Properties
    {
        [HDR] _InnerColor ("Cor interna", Color) = (1.5, 0.08, 0.01, 1)
        [HDR] _RimColor ("Cor da borda", Color) = (6.0, 0.8, 0.08, 1)

        _Intensity ("Intensidade", Range(0, 10)) = 2.0
        _BaseAlpha ("Opacidade do disco", Range(0, 1)) = 0.035
        _RimAlpha ("Opacidade da borda", Range(0, 2)) = 0.85
        _RimPower ("Espessura da borda", Range(0.5, 10)) = 3.2

        _NoiseScale ("Escala do ruido", Range(1, 30)) = 9.0
        _NoiseStrength ("Forca do ruido", Range(0, 1)) = 0.55
        _Speed ("Velocidade", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 worldPosition : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 localPosition : TEXCOORD2;
            };

            fixed4 _InnerColor;
            fixed4 _RimColor;

            float _Intensity;
            float _BaseAlpha;
            float _RimAlpha;
            float _RimPower;

            float _NoiseScale;
            float _NoiseStrength;
            float _Speed;

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3D(float3 p)
            {
                float3 cell = floor(p);
                float3 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);

                float n000 = hash31(cell + float3(0, 0, 0));
                float n100 = hash31(cell + float3(1, 0, 0));
                float n010 = hash31(cell + float3(0, 1, 0));
                float n110 = hash31(cell + float3(1, 1, 0));
                float n001 = hash31(cell + float3(0, 0, 1));
                float n101 = hash31(cell + float3(1, 0, 1));
                float n011 = hash31(cell + float3(0, 1, 1));
                float n111 = hash31(cell + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, local.x);
                float nx10 = lerp(n010, n110, local.x);
                float nx01 = lerp(n001, n101, local.x);
                float nx11 = lerp(n011, n111, local.x);

                float nxy0 = lerp(nx00, nx10, local.y);
                float nxy1 = lerp(nx01, nx11, local.y);

                return lerp(nxy0, nxy1, local.z);
            }

            float fbm(float3 p)
            {
                float value = 0.0;

                value += noise3D(p) * 0.5000;
                p = p * 2.03 + float3(11.7, 17.3, 5.9);

                value += noise3D(p) * 0.2500;
                p = p * 2.01 + float3(7.1, 13.5, 19.2);

                value += noise3D(p) * 0.1250;
                p = p * 2.04 + float3(23.4, 3.8, 9.6);

                value += noise3D(p) * 0.0625;

                return value / 0.9375;
            }

            v2f vert(appdata input)
            {
                v2f output;

                output.positionCS = UnityObjectToClipPos(input.vertex);
                output.worldPosition =
                    mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal =
                    UnityObjectToWorldNormal(input.normal);
                output.localPosition = input.vertex.xyz;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float3 normalWS = normalize(input.worldNormal);
                float3 viewDirection =
                    normalize(_WorldSpaceCameraPos.xyz - input.worldPosition);

                float facing = saturate(dot(normalWS, viewDirection));
                float rim = pow(1.0 - facing, _RimPower);

                float3 spherePosition =
                    normalize(input.localPosition) * _NoiseScale;

                float timeValue = _Time.y * _Speed;
                float3 movement =
                    float3(timeValue, timeValue * 0.37, -timeValue * 0.21);

                float noiseValue = fbm(spherePosition + movement);
                float filaments = smoothstep(0.28, 0.82, noiseValue);

                float variation = lerp(
                    1.0 - _NoiseStrength,
                    1.0 + _NoiseStrength,
                    noiseValue
                );

                float alpha = saturate(
                    (_BaseAlpha + rim * _RimAlpha) * variation
                );

                float colorMix = saturate(
                    rim * 1.30 + filaments * 0.15
                );

                float3 color = lerp(
                    _InnerColor.rgb,
                    _RimColor.rgb,
                    colorMix
                );

                color *= _Intensity * lerp(
                    0.75,
                    1.35,
                    filaments
                );

                return fixed4(color, alpha);
            }

            ENDCG
        }
    }

    FallBack Off
}
