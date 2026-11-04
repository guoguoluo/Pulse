Shader "PulseMachine/ConductorFilament"
{
    Properties
    {
        _GlowColor ("Filament Color", Color) = (0.12,0.9,0.75,1)
        _Glow ("Glow", Range(0,5)) = 0
        _GlowPower ("Emission Strength", Range(0,4)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _GlowColor;
            half _Glow;
            half _GlowPower;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                half power = saturate(_Glow * _GlowPower);
                return fixed4(_GlowColor.rgb * (0.2 + power * 0.8), power * 0.65);
            }
            ENDCG
        }
    }
    Fallback Off
}
