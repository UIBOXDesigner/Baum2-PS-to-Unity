Shader "UI/Equipment/Frame Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ShineColor ("Shine Color", Color) = (1,0.95,0.55,1)
        _ShineIntensity ("Shine Intensity", Range(0, 5)) = 1.8
        _ShineWidth ("Shine Width", Range(0.01, 0.5)) = 0.12
        _ShineSoftness ("Shine Softness", Range(0.001, 0.5)) = 0.08
        _ShineSpeed ("Shine Speed", Range(-5, 5)) = 0.65
        _ShineAngle ("Shine Angle", Range(-3.1416, 3.1416)) = 0.785
        _MinAlpha ("Min Mask Alpha", Range(0, 1)) = 0.05

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
        Blend SrcAlpha One
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
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _ShineColor;
            float _ShineIntensity;
            float _ShineWidth;
            float _ShineSoftness;
            float _ShineSpeed;
            float _ShineAngle;
            float _MinAlpha;

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

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 mask = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                float maskA = saturate(mask.a);

                float2 p = IN.texcoord - 0.5;
                float c = cos(_ShineAngle);
                float s = sin(_ShineAngle);
                float bandCoord = p.x * c + p.y * s + 0.5;
                float center = frac(_Time.y * _ShineSpeed);
                float d = abs(bandCoord - center);
                d = min(d, 1.0 - d);

                float band = smoothstep(_ShineWidth + _ShineSoftness, _ShineWidth, d);
                band *= step(_MinAlpha, maskA);

                fixed4 col;
                col.rgb = _ShineColor.rgb * _ShineIntensity * band;
                col.a = saturate(_ShineColor.a * band * maskA);

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
