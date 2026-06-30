Shader "UI/FlowLight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlowColor ("Flow Color", Color) = (1,1,1,1)
        _FlowSpeed ("Flow Speed", Float) = 1
        _FlowWidth ("Flow Width", Range(0.01,1)) = 0.18
        _FlowSoftness ("Flow Softness", Range(0.001,1)) = 0.08
        _FlowAngle ("Flow Angle", Range(-3.14,3.14)) = 0.6
        _FlowIntensity ("Flow Intensity", Range(0,5)) = 1.2

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
            Name "FlowLight"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _FlowColor;
            float _FlowSpeed;
            float _FlowWidth;
            float _FlowSoftness;
            float _FlowAngle;
            float _FlowIntensity;
            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                float2 uv = i.texcoord - 0.5;
                float s = sin(_FlowAngle);
                float c = cos(_FlowAngle);
                float rotatedX = uv.x * c - uv.y * s;

                float pos = frac(_Time.y * _FlowSpeed);
                float center = lerp(-0.75, 0.75, pos);

                float dist = abs(rotatedX - center);
                float band = 1.0 - smoothstep(_FlowWidth, _FlowWidth + _FlowSoftness, dist);

                fixed3 flowRgb = _FlowColor.rgb * band * _FlowIntensity;
                col.rgb += flowRgb * col.a;
                col.a = saturate(col.a);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
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