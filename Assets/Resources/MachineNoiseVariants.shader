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
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
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
                // Sparse rectangular data faults: displaced/mosaicked patches with cyan/magenta fragments.
                float2 cell = floor(uv * float2(34, 24));
                float gate = hash21(cell + float2(_Tick * 7, _Tick * 3));
                active = step(1 - .095 * saturate(amount), gate);
                float seed = hash21(cell + float2(_Tick + 13, 71));
                float2 offset = float2(seed * 2 - 1, hash21(cell + _Tick + 31) * 2 - 1);
                shifted += offset * float2(.014, .005) * amount * active;
                float2 grid = float2(240, 135);
                shifted = lerp(shifted, (floor(shifted * grid) + .5) / grid, active * saturate(amount) * .65);
                fixed4 color = tex2D(_MainTex, safeUV(shifted));
                float3 tint = seed > .5 ? float3(.14, .8, .85) : float3(.65, .18, .55);
                color.rgb = lerp(color.rgb, tint, active * amount * .10);
                float edge = step(.86, frac(uv.y * 24));
                color.rgb += active * edge * .04 * amount;
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
