/*
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
    // https://gist.github.com/kaiware007/8ebad2d28638ff83b6b74970a4f70c9a
    // https://forum.unity.com/threads/shadow-caster-billboard-to-camera.1214808/
*/

Shader "Quads/Billboard"{
    
Properties
{
    [NoScaleOffset] _ColorTex ("Texture", 2D) = "black" {}
    _Tex_ST("[Scale.xy, Translate.zw] Texture", Vector) = (1,1,0,0)
    [Toggle] _MirrorU("Mirror U", Int) = 0
    [Toggle] _MirrorV("Mirror V", Int) = 0
    [Toggle] _Clamp("Clamp Texture", Int) = 0
}

SubShader{
Tags {
    "RenderType"="Opaque"
    // "LightMode" = "ShadowCaster"
}

// Cull Back
ZWrite On 
ZTest LEqual
Cull Back

Pass{
CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag

    // #pragma multi_compile_shadowcaster

    #include "UnityCG.cginc"
    #include "../Includes/Quads.hlsl"
    #include "../Includes/Utils.hlsl"

    // props
    sampler2D _ColorTex;
    float4 _ColorTex_TexelSize;

    float4 _Tex_ST;
    bool _MirrorU, _MirrorV, _Clamp;

    struct v2f // vertex shader output
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 worldPos : TEXCOORD1;
    };


    v2f vert(appdata_base v) // vertex program with basic input
    {
        v2f o;
        o.uv = v.texcoord;
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.pos = BillboardObjectToClipPos(v.vertex.xyz);

        return o;
    }


    float4 frag(v2f i) : SV_TARGET // fragment program
    {
        float4 col;
        float2 pos = QuadCoords(i.uv, GetScale().xy, _ColorTex_TexelSize.zw, _Tex_ST);
        float2 uv = MirrorUV(pos + 0.5, bool3(_MirrorU, _MirrorV, _Clamp));
        // float2 uv = pos +0.5;

        col = tex2D(_ColorTex, uv);
        return col;
    }

ENDCG
}


// Pass to render object as a shadow caster
// Pass
// {
// Name "ShadowCaster"
// Tags { "LightMode" = "ShadowCaster" }

// ZWrite On ZTest LEqual Cull Off

// CGPROGRAM
//     #pragma vertex vert
//     #pragma fragment frag
//     #pragma multi_compile_shadowcaster
//     #include "UnityCG.cginc"

//     struct v2f {
//         // float4 pos : SV_POSITION;
//         V2F_SHADOW_CASTER;
//     };

//     v2f vert( appdata_base v )
//     {
//         v2f o;

//         TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
//         return o;
//     }

//     float4 frag( v2f i ) : SV_Target
//     {
//         // return float4(1.0, 1.0, 1.0, 1.0);
//         SHADOW_CASTER_FRAGMENT(i)
//     }
// ENDCG
// }

}
}
