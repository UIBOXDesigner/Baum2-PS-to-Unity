Shader "UI/Equipment/Outer Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _GlowColor ("Glow Color", Color) = (1,0.75,0.15,1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.6
        _GlowSize ("Glow Size", Range(0, 16)) = 4
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 1
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.5
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.35
        _EdgeBoost ("Edge Boost", Range(0, 4)) = 1.2

        [Header(Unity UI Stencil)]
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
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowSize;
            float _BaseAlpha;
            float _PulseSpeed;
            float _PulseAmount;
            float _EdgeBoost;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 SampleSprite(float2 uv)
            {
                return tex2D(_MainTex, uv) + _TextureSampleAdd;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseCol = SampleSprite(IN.texcoord) * IN.color;
                float baseA = saturate(baseCol.a * _BaseAlpha);

                float2 stepUV = _MainTex_TexelSize.xy * _GlowSize;
                float a = 0;
                a = max(a, SampleSprite(IN.texcoord + float2( stepUV.x, 0)).a);
                a = max(a, SampleSprite(IN.texcoord + float2(-stepUV.x, 0)).a);
                a = max(a, SampleSprite(IN.texcoord + float2(0,  stepUV.y)).a);
                a = max(a, SampleSprite(IN.texcoord + float2(0, -stepUV.y)).a);
                a = max(a, SampleSprite(IN.texcoord + float2( stepUV.x,  stepUV.y)).a);
                a = max(a, SampleSprite(IN.texcoord + float2(-stepUV.x,  stepUV.y)).a);
                a = max(a, SampleSprite(IN.texcoord + float2( stepUV.x, -stepUV.y)).a);
                a = max(a, SampleSprite(IN.texcoord + float2(-stepUV.x, -stepUV.y)).a);

                float edge = saturate((a - baseA) * _EdgeBoost + baseA * 0.35);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed * 6.28318) * _PulseAmount;
                float glowA = saturate(edge * _GlowColor.a * _GlowIntensity * pulse);

                fixed3 rgb = baseCol.rgb * baseA + _GlowColor.rgb * glowA;
                fixed alpha = saturate(baseA + glowA);
                fixed4 col = fixed4(rgb, alpha);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
