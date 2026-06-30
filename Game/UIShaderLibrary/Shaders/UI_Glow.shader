Shader "UI/Effects/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (1,0.8,0.25,1)
        _GlowSize ("Glow Size", Range(0, 20)) = 4
        _GlowStrength ("Glow Strength", Range(0, 5)) = 1.2

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            fixed4 _GlowColor;
            float _GlowSize;
            float _GlowStrength;


            struct appdata_t
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 ApplyUIClip(fixed4 col, v2f IN)
            {
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseCol = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                float2 o = _MainTex_TexelSize.xy * _GlowSize;
                float glowA = 0;
                glowA += tex2D(_MainTex, IN.texcoord + float2( o.x, 0)).a;
                glowA += tex2D(_MainTex, IN.texcoord + float2(-o.x, 0)).a;
                glowA += tex2D(_MainTex, IN.texcoord + float2(0,  o.y)).a;
                glowA += tex2D(_MainTex, IN.texcoord + float2(0, -o.y)).a;
                glowA += tex2D(_MainTex, IN.texcoord + float2( o.x,  o.y)).a;
                glowA += tex2D(_MainTex, IN.texcoord + float2(-o.x,  o.y)).a;
                glowA += tex2D(_MainTex, IN.texcoord + float2( o.x, -o.y)).a;
                glowA += tex2D(_MainTex, IN.texcoord + float2(-o.x, -o.y)).a;
                glowA = saturate(glowA / 8.0 - baseCol.a);
                fixed4 glowCol = fixed4(_GlowColor.rgb * _GlowStrength, glowA * _GlowColor.a * IN.color.a);
                fixed4 col = lerp(glowCol, baseCol, baseCol.a);
                return ApplyUIClip(col, IN);
            }
            ENDCG
        }
    }
}
