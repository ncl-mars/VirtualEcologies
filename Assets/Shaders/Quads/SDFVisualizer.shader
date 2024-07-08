/*
THIS IS A DEBUGG SHADER, meant to control the output of the sdf generator
*/

Shader "Quads/SDFVisualizer"
{
Properties
{
_Dimen("Quad Dimensions", Vector) = (1.0,1.0,1.0,1.0)

[NoScaleOffset] _ColorTex ("Color Texture", 2D) = "black" {}
[NoScaleOffset] _SDFTex ("SDF Texture", 2D) = "black" {}
[NoScaleOffset] _NormalTex ("Normal Texture", 2D) = "black" {}

_OffScaleTex("[Offset.xy, Scale.zw] Texture", Vector) = (0.0,0.0,1.0,1.0)
[Toggle] _MirrorU("Mirror U", Float) = 0
[Toggle] _MirrorV("Mirror V", Float) = 0
[Toggle] _FrameCut("Frame Cut", Float) = 0
_OffScaleFrame("[Offset.xy, Scale.zw] Frame", Vector) = (0.0,0.0,1.0,1.0)

[Header(SDF visualizer)][Space(10)]
[Toggle] _Isolines("Show Isolines", Float) = 0
[Toggle] _ShowSkin("Show Skin", Float) = 1
[Toggle] _BlackShape("Blacken Shape", Float) = 0
[Toggle] _AlphaTest("Test Alpha", Float) = 0
[Toggle] _Animate("Animate circle", Float) = 0

[PowerSlidder, 2]_OffsetLine("line offset", Range (-1.0, 1.0)) = 0.0
[PowerSlidder, 3]_LineWidth("line thickness", Range (0.0, 1.0)) = 0.01
[PowerSlidder, 4]_LineSharp("line sharpness", Range (0.00, 16.0)) = 1.0
}

SubShader
{
Tags { "Queue"="Transparent" "RenderType"="Transparent"}

// LOD 0
Cull Off
// ZWrite On
// ZTest Always
Blend SrcAlpha OneMinusSrcAlpha

Pass
{
CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    // #pragma target 5.0
    
    #include "UnityCG.cginc"
    #include "../Includes/Quads.hlsl"
    #include "../Includes/Utils.hlsl"

    // props
    sampler2D _ColorTex, _SDFTex, _NormalTex; // should be one sampler for the 3 texture

    float4 _Dimen, _ColorTex_TexelSize;    // sizes
    float4 _OffScaleTex, _OffScaleFrame;    // offscales
    float _MirrorU, _MirrorV, _FrameCut;    // sampling

    float _Isolines, _OffsetLine, _LineWidth, _LineSharp, _ShowSkin, _AlphaTest, _BlackShape, _Animate;


    struct v2f // vertex shader output
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };
    
    v2f vert(appdata_base v) // vertex program with basic input
    {
        v2f o;
        o.uv = v.texcoord;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
    }


    fixed4 frag(v2f i) : SV_TARGET // fragment program
    {
        fixed4 col = 0.0;

        // float2 pos = uvToPos(i.uv);
        float2 pos = QuadCoords(i.uv, GetScale().xy, _ColorTex_TexelSize.zw, float4(1,1,0,0));

        // float4 skin = tex2D(_ColorTex, i.uv);
        float4 skin = tex2D(_ColorTex, pos.xy * 0.5 + 0.5);
        skin.b = 1 * 0.5;
        skin.a = 1;

        return skin;

        // float sdf = tex2D(_SDFTex, ts.uv).r;
        // float2 nor = tex2D(_NormalTex, ts.uv).rg;
        
        float sdf = length(skin.rg) * 2.0 - 1.0;
        float2 nor = normalize(skin.rg);

        float pathDist = length(skin.ba) * 2.0 - 1.0;
        float2 pathDir = normalize(skin.ba);

        col = fixed4(nor, pathDir * (1 - abs(pathDist * 2)));
        col.a = max(col.a, 0.5);

        float outline = smoothstep(0.0,0.01, abs(sdf - 0.01));

        // float3 oCol = float3(0.95, 0.25, 0.01);
        // col.rgb = lerp(0, oCol, 1.0-outline);
        // col.a = 1;

        float curveSide = sign(pathDist);
        curveSide = max(curveSide, 0);

        
        return col;
    }

ENDCG
}
}
}










// // Basic noise
// float bnoise( in float x )
// {
//     // setup    
//     float i = floor(x);
//     float f = frac(x);
//     float s = sign(frac(x/2.0)-0.5);
    
//     float k = frac(i*.1731);

//     // quartic polynomial
//     return s*f*(f-1.0)*((16.0*k-4.0)*f*(f-1.0)-1.0);
// }

// float2 uvToPos(float2 uv)
// {
//     float2 pos = (uv - 0.5) * 2.0;
//     pos *= _Dimen/min(_Dimen.x,_Dimen.y); // quad ratio
//     return pos;
// }

// struct texSpace
// {
//     float2 uv;
//     float frame;
//     float2 flip;
// };


// texSpace posToTexSpace(float2 pos)
// {
//     texSpace ts; 

//     ts.uv = pos * _OffScaleTex.zw * 0.5 - _OffScaleTex.xy;
//     ts.uv /= _ColorTex_TexelSize.zw / max(_ColorTex_TexelSize.z, _ColorTex_TexelSize.w); // image ratio

//     // frame cut fac, kind of useless finally...
//     float2 qc = ts.uv;
//     qc = qc * _OffScaleFrame.zw - _OffScaleFrame.xy;
//     qc = abs(qc);
//     ts.frame = lerp(1.0, step(qc.x, 0.5) * step(qc.y, 0.5), _FrameCut); // use lerp instead of "if", no branching

//     ts.uv += 0.5; // convert back pos to uv
    
//     float2 mirror = float2(_MirrorU, _MirrorV);

//     float2 flip = floor((ts.uv % 2.0) + (1-sign(ts.uv)));
//     ts.flip = lerp(1, -(2*flip-1), mirror);

//     float2 qm = abs(ts.uv);
//     qm = floor(qm%2) - clamp(frac(qm), 0.001, 0.999);

//     qm = abs(qm);
//     ts.uv = lerp(ts.uv, qm, mirror);

//     return ts;
// }

