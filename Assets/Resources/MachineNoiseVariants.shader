Shader "Hidden/KO/MachineNoiseVariants"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Style, _Strength, _Tick;
            float hash21(float2 p)
            {
                // Keep intermediate values small enough for long-running sessions on the GPU.
                float3 p3 = frac(float3(p.x, p.y, p.x) * .1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            float2 safeUV(float2 uv)
            {
                float2 border = abs(_MainTex_TexelSize.xy) * .5;
                return clamp(uv, border, 1 - border);
            }
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;
                float amount = _Strength;
                float2 shifted = uv;
                float active;
                if (_Style < 1.5)
                {
                    // A few horizontal bands tear sideways; RGB edges separate only in those bands.
                    float row = floor(uv.y * 38);
                    float gate = hash21(float2(row, _Tick));
                    active = step(1 - .075 * saturate(amount), gate);
                    float jitter = hash21(float2(row + 51, _Tick + 9)) * 2 - 1;
                    shifted.x += jitter * .009 * amount * active;
                    fixed4 color = tex2D(_MainTex, safeUV(shifted));
                    float split = .0018 * amount * active;
                    color.r = tex2D(_MainTex, safeUV(shifted + float2(split, 0))).r;
                    color.b = tex2D(_MainTex, safeUV(shifted - float2(split, 0))).b;
                    float scan = step(.7, frac(uv.y * abs(_MainTex_TexelSize.w) * .5));
                    color.rgb *= 1 - scan * .018 * amount;
                    color.rgb += active * jitter * .025 * amount;
                    return color;
                }
                // Vertical signal faults: narrow columns jump up/down with vertical RGB separation.
                float column = floor(uv.x * 60);
                float gate = hash21(float2(column + 113, _Tick));
                active = step(1 - .075 * saturate(amount), gate);
                float jitter = hash21(float2(column + 164, _Tick + 9)) * 2 - 1;
                shifted.y += jitter * .012 * amount * active;
                fixed4 color = tex2D(_MainTex, safeUV(shifted));
                float split = .0024 * amount * active;
                color.r = tex2D(_MainTex, safeUV(shifted + float2(0, split))).r;
                color.b = tex2D(_MainTex, safeUV(shifted - float2(0, split))).b;
                float scan = step(.7, frac(uv.x * abs(_MainTex_TexelSize.z) * .5));
                color.rgb *= 1 - scan * .018 * amount;
                color.rgb += active * jitter * .025 * amount;
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
