Shader "Pulse/ConductorBlackout"
{
    Properties { _MainTex ("Screen", 2D) = "white" {} _Darkness ("Darkness", Range(0,1)) = 1 }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Darkness;
            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                return fixed4(color.rgb * (1 - saturate(_Darkness)), color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
