Shader "PulseMachine/DissolveLit"
{
    Properties
    {
        // URP Lit 标准属性
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_NORMALMAP)] _NormalMapToggle("Normal Map", Float) = 0
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0
        _EmissionColor("Emission Color", Color) = (0, 0, 0, 0)
        [Toggle(_EMISSION)] _EmissionToggle("Emission", Float) = 0
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0,1)

        // 溶解参数
        [Header(Dissolve)]
        _DissolveProgress("Dissolve Progress", Range(0, 1)) = 0
        [HDR] _DissolveEdgeColor("Dissolve Edge Color", Color) = (1, 0.5, 0, 1)
        _DissolveEdgeWidth("Dissolve Edge Width", Range(0.001, 0.1)) = 0.02
        _DissolveNoiseScale("Noise Scale", Range(1, 100)) = 10

        // 渲染状态
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 1
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment DissolveFragment
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // 标准 URP 属性
            TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Smoothness;
            float _Metallic;
            float4 _EmissionColor;

            // 溶解属性
            float _DissolveProgress;
            float4 _DissolveEdgeColor;
            float _DissolveEdgeWidth;
            float _DissolveNoiseScale;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 posCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
            };

            // 简易 3D 噪声（基于世界坐标）
            float SimpleNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n = i.x + i.y * 57.0 + i.z * 131.0;
                return frac(sin(n) * 43758.5453);
            }

            float FBM(float3 p, int octaves)
            {
                float value = 0;
                float amplitude = 0.5;
                float frequency = 1.0;
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * SimpleNoise3D(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.posCS = vpi.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = vpi.positionWS;
                output.normalWS = vni.normalWS;
                output.tangentWS = float4(vni.tangentWS, input.tangentOS.w);
                return output;
            }

            float4 DissolveFragment(Varyings input) : SV_Target
            {
                // === 常规 URP Lit 采样 ===
                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 albedo = baseMap.rgb * _BaseColor.rgb;

                float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 posWS = input.positionWS;

                // === 溶解计算 ===
                float noiseVal = FBM(posWS * _DissolveNoiseScale, 3);
                float dissolve = noiseVal - _DissolveProgress;

                // 溶解边缘发光
                float edgeMask = 1.0 - smoothstep(0, _DissolveEdgeWidth, abs(dissolve));
                float alpha = smoothstep(0, 0.01, dissolve);

                // Alpha 剔除
                clip(alpha - 0.01);

                // === 简易光照 ===
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float NdotL = saturate(dot(normalWS, lightDir));

                float3 ambient = SampleSH(normalWS) * albedo;
                float3 diffuse = mainLight.color * albedo * NdotL;

                float3 specular = 0;
                if (NdotL > 0)
                {
                    float3 viewDir = GetWorldSpaceViewDir(posWS);
                    float3 halfVec = SafeNormalize(lightDir + viewDir);
                    float NdotH = saturate(dot(normalWS, halfVec));
                    float spec = pow(NdotH, exp2(10.0 * _Smoothness + 1.0));
                    specular = mainLight.color * _Metallic * spec;
                }

                float3 finalColor = ambient + diffuse + specular;

                // 边缘发光叠加
                finalColor = lerp(finalColor, _DissolveEdgeColor.rgb, edgeMask * _DissolveEdgeColor.a);
                finalColor += _EmissionColor.rgb;

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}