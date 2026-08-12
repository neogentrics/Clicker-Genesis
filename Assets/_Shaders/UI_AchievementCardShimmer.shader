// Custom UI shader for achievement card frames (2026-08-12).
//
// Why this exists: Unity's CanvasRenderer always binds an Image's *sprite* texture to
// whatever property is tagged [PerRendererData] (conventionally _MainTex), completely
// ignoring any texture baked into the assigned Material. That made an earlier attempt at
// putting a custom "swirl" material directly on these cards a no-op - the material was
// assigned correctly in code, but the diamond frame's own tier-rank sprite always won at
// render time. This shader routes around that: _MainTex still receives the sprite (used
// ONLY as an alpha/shape mask, via [PerRendererData] as usual), and the real swirl artwork
// lives under _SwirlTex - a property CanvasRenderer never touches - so the material's own
// texture actually reaches the screen. _SwirlTex scrolls over time for a living shimmer.
//
// Otherwise mirrors Unity's real UI/Default shader exactly (stencil block, _ClipRect
// soft-clipping, blend/cull/zwrite state) so it stays fully compatible with RectMask2D and
// Mask - a custom shader that skips _ClipRect support is exactly what broke card clipping
// during scrolling the first time around; not making that mistake twice.
Shader "UI/AchievementCardShimmer"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask Sprite (tier-rank frame shape)", 2D) = "white" {}
        _SwirlTex ("Swirl / Energy Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SwirlColor ("Swirl Tint", Color) = (1,1,1,1)
        _ScrollSpeed ("Scroll Speed (u,v)", Vector) = (0.05, 0.03, 0, 0)
        _PulseSpeed ("Pulse Speed", Float) = 1.2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.25

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _SwirlTex;
            fixed4 _Color;
            fixed4 _SwirlColor;
            float4 _ClipRect;
            float2 _ScrollSpeed;
            float _PulseSpeed;
            float _PulseAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Shape/silhouette only - the tier-rank sprite's own texture, forced here by
                // CanvasRenderer regardless of what we assign in the material.
                half maskAlpha = tex2D(_MainTex, IN.texcoord).a;

                // Real color comes from here instead - scrolled for a slow, living shimmer,
                // plus a gentle sine pulse so it doesn't read as a static, flat scroll.
                float2 swirlUv = IN.texcoord + _ScrollSpeed * _Time.y;
                half4 swirl = tex2D(_SwirlTex, swirlUv) * _SwirlColor;
                half pulse = 1 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                swirl.rgb *= pulse;

                half4 col;
                col.rgb = swirl.rgb * IN.color.rgb;
                col.a = maskAlpha * IN.color.a;

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
