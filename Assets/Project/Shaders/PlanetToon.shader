Shader "PulseMachine/PlanetToon"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _MainTex ("Base Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Tint", Color) = (0.76,0.81,0.91,1)
        _LightCutoff ("Light Cutoff", Range(0,1)) = 0.48
        _LightSmoothness ("Band Softness", Range(0.001,0.25)) = 0.045
        _AmbientStrength ("Ambient Strength", Range(0,1)) = 0.35
        _LightStrength ("Light Strength", Range(0,1)) = 0.65
        _OutlineColor ("Outer Contour", Color) = (0.39,0.43,0.55,0.7)
        _OutlineWidth ("Outer Width Pixels", Range(0,5)) = 1.5
        _InteriorColor ("Interior Line Tint", Color) = (0.4,0.46,0.59,1)
        _InteriorStrength ("Interior Line Strength", Range(0,0.3)) = 0.08
        _InteriorCutoff ("Interior Band Position", Range(0,1)) = 0.44
        _InteriorWidth ("Interior Band Width", Range(0.005,0.2)) = 0.06
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _ShadowColor;
        fixed4 _InteriorColor;
        half _LightCutoff;
        half _LightSmoothness;
        half _AmbientStrength;
        half _LightStrength;
        half _InteriorStrength;
        half _InteriorCutoff;
        half _InteriorWidth;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = albedo.rgb;
            o.Alpha = albedo.a;
        }
        half4 LightingToon(SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = saturate(dot(s.Normal, lightDir));
            half lit = ndl * atten;
            half band = smoothstep(_LightCutoff - _LightSmoothness, _LightCutoff + _LightSmoothness, lit);
            half3 shade = lerp(_ShadowColor.rgb, _LightColor0.rgb * _LightStrength, band);
            half interior = (1 - smoothstep(0, _InteriorWidth, abs(lit - _InteriorCutoff))) * _InteriorStrength;
            half3 baseColor = s.Albedo * (shade + UNITY_LIGHTMODEL_AMBIENT.rgb * _AmbientStrength);
            half4 result;
            result.rgb = lerp(baseColor, baseColor * _InteriorColor.rgb, interior);
            result.a = s.Alpha;
            return result;
        }
        ENDCG
        Pass
        {
            Name "OuterContour"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            fixed4 _OutlineColor;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v)
            {
                v2f o;
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 dir = normalize(mul((float2x2)UNITY_MATRIX_P, viewNormal.xy) + float2(0.0001, 0.0001));
                clip.xy += dir * (_OutlineWidth * 2.0 / _ScreenParams.xy) * clip.w;
                o.pos = clip;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
