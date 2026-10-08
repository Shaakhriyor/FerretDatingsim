Shader "VN/Doctor Screen Blur"
{
    Properties { _MainTex ("Screen", 2D) = "white" {} }
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
            float4 _MainTex_TexelSize;
            float4 _BlurDirection;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 d = abs(_MainTex_TexelSize.xy) * _BlurDirection.xy;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.2270270270;
                c += tex2D(_MainTex, i.uv + d * 1.3846153846) * 0.3162162162;
                c += tex2D(_MainTex, i.uv - d * 1.3846153846) * 0.3162162162;
                c += tex2D(_MainTex, i.uv + d * 3.2307692308) * 0.0702702703;
                c += tex2D(_MainTex, i.uv - d * 3.2307692308) * 0.0702702703;
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
