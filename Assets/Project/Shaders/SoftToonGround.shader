Shader "PulseMachine/SoftToonGround"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.46,0.61,0.67,1)
        _MainTex ("Base Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Tint", Color) = (0.78,0.84,0.9,1)
        _LightCutoff ("Light Cutoff", Range(0,1)) = 0.46
        _LightSmoothness ("Light Transition", Range(0.01,1)) = 0.65
        _AmbientStrength ("Ambient Strength", Range(0,1)) = 0.38
        _LightStrength ("Light Strength", Range(0,1)) = 0.59
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf SoftGround fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _ShadowColor;
        half _LightCutoff;
        half _LightSmoothness;
        half _AmbientStrength;
        half _LightStrength;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = albedo.rgb;
            o.Alpha = albedo.a;
        }
        half4 LightingSoftGround(SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = saturate(dot(s.Normal, lightDir));
            half band = smoothstep(_LightCutoff - _LightSmoothness,
                _LightCutoff + _LightSmoothness, ndl);
            half3 shade = lerp(_ShadowColor.rgb, _LightColor0.rgb * _LightStrength, band) * atten;
            half4 result;
            result.rgb = s.Albedo * (shade + UNITY_LIGHTMODEL_AMBIENT.rgb * _AmbientStrength);
            result.a = s.Alpha;
            return result;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
