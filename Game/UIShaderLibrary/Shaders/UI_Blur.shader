Shader "UI/Effects/Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 10)) = 2
        _Alpha ("Alpha", Range(0, 1)) = 1

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
            float _BlurSize;
            float _Alpha;


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
                float2 o = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 col = 0;
                col += tex2D(_MainTex, IN.texcoord) * 4.0;
                col += tex2D(_MainTex, IN.texcoord + float2( o.x, 0)) * 2.0;
                col += tex2D(_MainTex, IN.texcoord + float2(-o.x, 0)) * 2.0;
                col += tex2D(_MainTex, IN.texcoord + float2(0,  o.y)) * 2.0;
                col += tex2D(_MainTex, IN.texcoord + float2(0, -o.y)) * 2.0;
                col += tex2D(_MainTex, IN.texcoord + float2( o.x,  o.y));
                col += tex2D(_MainTex, IN.texcoord + float2(-o.x,  o.y));
                col += tex2D(_MainTex, IN.texcoord + float2( o.x, -o.y));
                col += tex2D(_MainTex, IN.texcoord + float2(-o.x, -o.y));
                col /= 16.0;
                col = (col + _TextureSampleAdd) * IN.color;
                col.a *= _Alpha;
                return ApplyUIClip(col, IN);
            }
            ENDCG
        }
    }
}
