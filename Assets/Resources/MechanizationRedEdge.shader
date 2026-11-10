Shader "UI/MechanizationRedEdge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Mechanization", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            float _Progress;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=abs(i.uv*2-1);
                float edge=max(p.x,p.y);
                float inner=.95-1.2*saturate(_Progress);
                float mask=smoothstep(inner,inner+.25,edge);
                fixed4 result=i.color;
                result.a*=mask*smoothstep(0,.2,_Progress);
                return result;
            }
            ENDCG
        }
    }
}
