/*
THIS IS A DEBUGG SHADER, meant to control the output of the distShp generator
*/

Shader "Particles/NetCanvas"
{
Properties
{
[NoScaleOffset] _NetMap ("NetMap Data Texture", 2D) = "black" {}

[Header(SDF visualizer)][Space(10)]
[Toggle] _Isolines("Show Isolines", Float) = 0
[Toggle] _Animate("Animate circle", Float) = 0

[PowerSlidder, 2]_OffsetLine("line offset", Range (-1.0, 1.0)) = 0.0
[PowerSlidder, 3]_LineWidth("line thickness", Range (0.0, 1.0)) = 0.01
[PowerSlidder, 4]_LineSharp("line sharpness", Range (0.00, 16.0)) = 1.0
}

SubShader
{
Tags { "Queue"="Transparent" "RenderType"="Transparent"}


Cull Front
Blend SrcAlpha OneMinusSrcAlpha

Pass
{
CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    // #pragma target 5.0
    
    #include "UnityCG.cginc"


    //======================================================= Struct and Uniforms
    struct v2f{

        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    sampler2D _NetMap;
    float4 _NetMap_TexelSize;
    
    float _Isolines, _OffsetLine, _LineWidth, _LineSharp, _Animate;

    //======================================================= Methods


    //======================================================= Vertex
    v2f vert(appdata_base v){

        v2f o;
        o.uv = v.texcoord;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
    }

    //======================================================= Fragment
    fixed4 frag(v2f i) : SV_TARGET // fragment program
    {
        fixed4 col = 0.0;
        // float2 pos = uvToPos(i.uv);

        float4 netData = tex2D(_NetMap, i.uv);

        float distShp   = length(netData.rg) * 2.0 - 1.0;
        float2 norShp  = normalize(netData.rg);

        float distPath = length(netData.ba) * 2.0 - 1.0;
        float2 dirPath = normalize(netData.ba);

        col = fixed4(norShp, dirPath * (1 - abs(distPath * 2)));
        col.a = max(col.a, 0.5);
        col.b =0.0;

        return col;

        float outline = smoothstep(0.0,0.01, abs(distShp - 0.01));

        // float3 oCol = float3(0.95, 0.25, 0.01);
        // col.rgb = lerp(0, oCol, 1.0-outline);
        // col.a = 1;

        float curveSide = sign(distPath);
        curveSide = max(curveSide, 0);


        float iso = sin(distShp * 500) * 0.5 + 0.5;
        col.rgb = iso;
        col.a = 1.0;

        return col;
    }

ENDCG
}
}
}


