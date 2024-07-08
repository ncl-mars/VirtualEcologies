/*
    A cube shader that implements a raymarcher in local space, within the bound of a cube

    // https://docs.unity3d.com/Manual/SL-BuiltinFunctions.html
*/
Shader "SdfCompositor/BaseCompositor"{
Properties{

    [Header(Input Textures)][Space(10)]
    [NoScaleOffset] _NoiseTex ("Noise Texture", 3D) = "" {}

    // Displacement
    [PowerSlidder, 2]_FDisp("displace freq", Range (0.0001, 5)) = 1
    [PowerSlidder, 3]_ADisp("displace amp", Range (0.000, 1)) = 0.1

    // TEMP
    [PowerSlidder, 3]_BoxLerp("Box mapping lerp", Range (0.000, 10)) = 0.1

    
    //-- BASE MATE COLOR
    [Header(Base Mate Color)][Space(10)]
    _HueMate("Hue", Range (0.0, 1.0)) = 0.5
    _SatMate("Saturation", Range (0.0, 1.0)) = 0.5
    _ValMate("Value", Range (0.0, 1.0)) = 0.5

    //-- SUN SETTINGS
    [Header(Sun Lighting)][Space(10)]
    _SunDir("Sun position", Vector) = (0.85,0.75,0.25,1.0)
    _SunStr("Sun Intensity",  Range (0.0, 1.0)) = 1
}

SubShader{
Tags { 
    "Queue" = "Transparent" 
    "RenderType" = "Transparent"
    // "RenderType" = "TransparentCutout"
}
Blend SrcAlpha OneMinusSrcAlpha
ZWrite On
ZTest Always
LOD 0
Cull Front

Pass
{
CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag

    #include "UnityCG.cginc"

    #include "../Includes/ColorFunctions.hlsl"
    #include "../Includes/SDFOperations.hlsl"
    #include "../Includes/SDFPrimitivesBase.hlsl"

    #include "../Includes/Interpolators.hlsl"
    #include "../Includes/Utils.hlsl"

    struct Shape{

        float4x4 txi;
        float scale;
        float depth;

        int idSdf;
        int idCol;

        float off2d;
        float round;
        float blend;
    };

    sampler3D _NoiseTex;
    float4 _NoiseTex_TexelSize;

    UNITY_DECLARE_TEX2DARRAY(_SdfArray); // faster than using col sampler

    Texture2DArray _ColArray;
    SamplerState sampler_ColArray;

    StructuredBuffer<Shape> _Shapes;
    int _NumShapes;

    float _FDisp, _ADisp;

    fixed4 _SunDir;
    fixed _SunStr;

    float _HueMate, _SatMate, _ValMate;
    float _BoxLerp; //

    //_________________________________________________________________________________
    ///////////////////////////////////////////////////////////////////////////////////
    struct appdata{
        float4 pos : POSITION;
    };

    struct v2f{
        float4 pos : SV_POSITION;
        float3 eye : TEXCOORD1;
        float3 view : TEXCOORD2;
    };

    // TODO: set scale as uniform !
    v2f vert (appdata v){

        v2f o;
        o.pos = UnityObjectToClipPos(v.pos );

        float3 scl = GetScale(); // should be set as uniform !!
        
        o.view = -ObjSpaceViewDir(v.pos) * scl;
        o.eye = v.pos.xyz * scl - o.view;        
        return o;
    }

    //_________________________________________________________________________________
    ///////////////////////////////////////////////////////////////////////////////////
    //---------------------------------------------------------- Structs
    struct BlendData{
        uint id;
        float fac;
        float3 coord;
    };

    struct Model{
        uint id;
        float3 coord;
        BlendData blend;
    };

    //---------------------------------------------------------- @block
    // blend color factor to material
    float BlendSDF(float nd, float cd, float k, in BlendData data, inout Model model, inout float near){

        k *= 6.0;
        float h = max( k-abs(nd-cd), 0.0 )/k;
        float m = h*h*h*0.5;
        float s = m*k*(1.0/3.0);

        if(nd < cd)
        {
            near = cd; // near = prev dist

            model.blend.fac = m;
            model.blend.id = model.id;
            model.blend.coord = model.coord;

            model.id = data.id;
            model.coord = data.coord;

            return nd-s;
        }
        else
        {
            if(nd < near)
            {
                near = nd; // new near dist
                model.blend = data;
                model.blend.fac = m;
            }

            return cd-s;
        }
    }

    float Map(in float3 pos, inout Model model){

        float an = _Time.y * 0.01;
        half no = Tex3DTricubicFast1(_NoiseTex, (pos * 0.5 * _FDisp + 0.5 + an), _NoiseTex_TexelSize.w);
        // model.disp = no * _ADisp;

        float d = 1e6;
        float near = d; // 2d closest distance used to id neighboor
        
        // const uint n = (uint)_PropsTex_TexelSize.z; // hum... slows down a lot, should be known at compile time
        const uint n = _NumShapes;
        // const uint n = 4;
        
        [loop]
        for(uint i = 0; i < n; i++){

            // to element space
            BlendData data = {i, 0.0, mul(_Shapes[i].txi, float4(pos, 1)).xyz};

            float2 uv = (data.coord.xy / _Shapes[i].scale);

            float clipBox = SdBox(uv, 0.499) * _Shapes[i].scale; // used to fill empty spaces left by the texture
            uv += 0.5;

            // element shape
            half nd = (UNITY_SAMPLE_TEX2DARRAY(_SdfArray, float3(uv, _Shapes[i].idSdf)).r - _Shapes[i].off2d) * _Shapes[i].scale;
            nd = max(nd, clipBox); // intersect clipbox

            // 3d, extrusion & round
            half ext = max( _Shapes[i].depth * (1 - _Shapes[i].round) * 0.5, 0.0001);
            half rnd = _Shapes[i].round * _Shapes[i].depth;
            nd += rnd;
            nd = OpExtrusion(data.coord, nd, ext) - rnd;

            data.coord /= _Shapes[i].scale;
            d = BlendSDF(nd, d, max(_Shapes[i].blend * _Shapes[i].scale, 0.00001), data, model, near);
        }

        d -= no * _ADisp;

        return d;
    }

    //---------------------------------------------------------- Import raymarching
    #define Map Map
    #define Model Model

    #define MAX_STEP_SURF 64
    #define EPS_SURF 0.0005
    #define SCALE_MARCH 0.75
    #define MAX_STEP_SHA 8

    #include "../Includes/RayMarching.hlsl"

    //---------------------------------------------------------- Shade
    fixed4 ShadeSurf(in Ray r, in Model m){

        float3 pos = r.or + r.dir * r.len;
        float3 nor = CalcNormal(pos,0.025);

        fixed3 col = fixed3(0.5,0.25,0.1);

        // float noc = tex3D(_NoiseTex, float3(m.id       * 5.1, 0.5,0.5)) * 0.5 + 0.5;
        // float non = tex3D(_NoiseTex, float3(m.blend.id * 5.1, 0.5,0.5)) * 0.5 + 0.5;
        // fixed3 colc = Palette( noc * 10, fixed3(0.8,0.5,0.4),fixed3(0.2,0.4,0.2),fixed3(2.0,1.0,1.0),fixed3(0.0,0.25,0.25) );
        // fixed3 coln = Palette( non * 10, fixed3(0.8,0.5,0.4),fixed3(0.2,0.4,0.2),fixed3(2.0,1.0,1.0),fixed3(0.0,0.25,0.25) );
        fixed3 colc = BoxMap(_ColArray, sampler_ColArray, m.coord * 0.5 + 0.5      , nor, _BoxLerp, m.id).rgb;
        fixed3 coln = BoxMap(_ColArray, sampler_ColArray, m.blend.coord * 0.5 + 0.5, nor, _BoxLerp, m.blend.id).rgb;
        col = lerp(colc, coln, saturate(m.blend.fac * 1.0));

        // col = BoxMap(_ColArray, sampler_ColArray, m.coord + 0.5, nor, _BoxLerp, _Shapes[m.id].idCol).rgb;
       
        float3 lin = 0;
        lin += SunLight(col, pos, r.dir, nor, _SunDir, _SunStr, 32);

        //----------------------------------------- SKY
        float dif = sqrt(clamp( 0.5+0.5*nor.y, 0.0, 1.0 ));
        lin += col*0.6*dif*float3(0.40,0.60,1.15) * 2;

        col = lin;
        // col = lerp( col, float3(0.7,0.7,0.9), 1.0-exp( -0.0001*res.x*res.x*res.x ) );
        return fixed4(col, 1.0);
    }
    
    //_________________________________________________________________________________
    ///////////////////////////////////////////////////////////////////////////////////
    fixed4 frag(v2f i) : SV_Target{

        Model m; Ray r = {i.eye, normalize(i.view), Map(i.eye, m)};
        
        if(March(r, length(i.view), m)){ return ShadeSurf(r, m);}

        return 0;
    }
ENDCG
}
}
}




//________________________________________________________________________________________________
    // UNITY_SAMPLE_TEX2DARRAY faster than :
    // half nd = (_SdfArray.Sample(sampler_ColArray, float3(uv, _Shapes[i].slice)).r - _Shapes[i].off2d) * _Shapes[i].scale;

    // [loop]
    // for(uint i = 0; i < n; i++){

    //     //-- get props from texture
    //     half4x4 txi = half4x4(_PropsTex[uint2(i, 0)],_PropsTex[uint2(i, 1)], _PropsTex[uint2(i, 2)], _PropsTex[uint2(i, 3)]);
    //     half4 scales = _PropsTex[uint2(i, 4)];
    //     half4 params = _PropsTex[uint2(i, 5)];
    //     scales = abs(scales);

    //     // to element space
    //     BlendData data;
    //     data.id = i;
    //     data.coord = mul( txi, float4(pos, 1));

    //     float2 uv = (data.coord.xy / scales.w);

    //     float clipBox = SdBox(uv, 0.49) * scales.w; // used to fill empty spaces left by the texture
    //     uv += 0.5;

    //     // element shape
    //     half nd = (UNITY_SAMPLE_TEX2DARRAY(_SdfArray, float3(uv, params.x)).r - params.y) * scales.w;
    //     nd = max(nd, clipBox); // intersect clipbox

    //     // 3d, extrusion & round
    //     half ext = max(scales.z * (1 - params.z) * 0.5, 0.0001);
    //     half rnd = params.z * scales.z;
    //     nd += rnd;
    //     nd = OpExtrusion(data.coord, nd, ext) - rnd;

    //     data.coord /= scales.w;
    //     d = BlendSDF(nd, d, max(params.w * scales.w, 0.00001), data, model, near);
    // }





    // colc = BoxMap(_ColTex, m.coord * 0.5 + 0.5, nor, 1).rgb;
    // coln = BoxMap(_ColTex, m.blend.coord * 0.5 + 0.5, nor, 1).rgb;
    

        // float a = 1.62;
        // float2x2 rot = float2x2(cos(a), -sin(a), sin(a), cos(a));
        // float3 q = pos * 0.25;
        // q.xz = mul(q.xz, rot);
        // col = BoxMap(_ColTex, q + 0.5, nor, 1).rgb;
        // colc = UNITY_SAMPLE_TEX2DARRAY(_ColArray, res.zwy ).rgb;





    // blend color factor to material

    // float h = clamp( 0.5+0.5*(nres.x-cres.x)/k, 0.0, 1.0 );
    // float s = k*h*(1.0-h);
    // float m = h;


    // // Cubic Polynomial Smooth-minimum, https://www.shadertoy.com/view/MXfXzM
    // float2 SMin( float a, float b, float k ){

    //     k *= 6.0;
    //     float h = max( k-abs(a-b), 0.0 )/k;
    //     float m = h*h*h*0.5;
    //     float s = m*k*(1.0/3.0); 
    //     return (a<b) ? float2(a-s,m) : float2(b-s,1.0-m);
    // }

    
    // float4 EvalSdElement(float3 pos, int i)
    // {
    //     //-- generate sdShape from tex
    //     half4x4 txi = float4x4(_PropsTex[uint2(i, 0)],_PropsTex[uint2(i, 1)], _PropsTex[uint2(i, 2)], _PropsTex[uint2(i, 3)]);
    //     float3 q = mul( txi, float4(pos, 1));
        
    //     half4 scales = _PropsTex[uint2(i, 4)];
    //     half4 params = _PropsTex[uint2(i, 5)];
        
    //     float2 uv = (q.xy / scales.w) + 0.5;

    //     float d = (UNITY_SAMPLE_TEX2DARRAY(_SdfArray, float3(uv, params.x)).r - params.y) * scales.w ;

    //     half depth = abs(scales.z);
    //     half ext = max(depth * (1 - params.z) * 0.5, 0.0001);
    //     half rnd = params.z * depth;
        
        
    //     d += rnd;
    //     d = OpExtrusion(q, d, ext) - rnd;

    //     return float4(d, uv, params.w);
    // }
