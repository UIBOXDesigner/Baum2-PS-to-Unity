Shader "UI/Effects/ProgressMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 1
        _FillOrigin ("Fill Origin", Range(0, 3)) = 0
        _Softness ("Softness", Range(0.001, 0.2)) = 0.01
        _Radial ("Radial Mode", Range(0, 1)) = 0

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
            float _FillAmount;
            float _FillOrigin;
            float _Softness;
            float _Radial;


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
                fixed4 col = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                float2 uv = IN.texcoord;
                float linearValue = uv.x;
                if (_FillOrigin > 0.5 && _FillOrigin < 1.5) linearValue = 1.0 - uv.x;
                if (_FillOrigin > 1.5 && _FillOrigin < 2.5) linearValue = uv.y;
                if (_FillOrigin > 2.5) linearValue = 1.0 - uv.y;
                float linearMask = 1.0 - smoothstep(_FillAmount, _FillAmount + _Softness, linearValue);
                float2 centered = uv - 0.5;
                float angle = atan2(centered.y, centered.x) / 6.2831853 + 0.5;
                float radialMask = 1.0 - smoothstep(_FillAmount, _FillAmount + _Softness, angle);
                float mask = lerp(linearMask, radialMask, step(0.5, _Radial));
                col.a *= mask;
                return ApplyUIClip(col, IN);
            }
            ENDCG
        }
    }
}
