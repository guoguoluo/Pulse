Shader "PulseMachine/ConductorMycelium"
{
    Properties
    {
        _MainTex ("Mushroom Atlas", 2D) = "white" {}
        _BaseColor ("Dormant Tint", Color) = (0.58,0.67,0.65,1)
        _GlowColor ("Awake Glow", Color) = (0.13,0.68,1,1)
        _Glow ("Glow", Range(0,5)) = 0
        _GlowPower ("Emission Strength", Range(0,8)) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _BaseColor;
        fixed4 _GlowColor;
        half _Glow;
        half _GlowPower;
        struct Input { float2 uv_MainTex; float3 worldNormal; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed3 textureColor = tex2D(_MainTex, IN.uv_MainTex).rgb;
            half reveal = saturate(_Glow);
            half cap = smoothstep(0.05, 0.55, IN.worldNormal.y);
            o.Albedo = textureColor * _BaseColor.rgb;
            o.Emission = _GlowColor.rgb * _Glow * _GlowPower * lerp(0.045, 1.0, cap);
            o.Alpha = 1;
        }
        ENDCG
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "MYCELIUM_GLOW"
            Tags { "LightMode"="MyceliumGlow" }
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; half cap : TEXCOORD0; };
            fixed4 _GlowColor;
            half _Glow;
            half _GlowPower;
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.cap = smoothstep(0.05, 0.55, UnityObjectToWorldNormal(v.normal).y);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(_GlowColor.rgb * _Glow * _GlowPower * lerp(0.045, 1.0, i.cap), 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
