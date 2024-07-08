/*
    A cube shader that implements a raymarcher in local space, within the bound of a cube

    // https://docs.unity3d.com/Manual/SL-BuiltinFunctions.html
*/
Shader "SdfCompositor/BlendMap"{
Properties{

    [Header(Input Textures)][Space(10)]
    [NoScaleOffset] _NoiseTex ("Noise Texture", 3D) = "" {}

    // Displacement
    [PowerSlidder, 2]_FDisp("displace freq", Range (0.0001, 5)) = 1
    [PowerSlidder, 3]_ADisp("displace amp", Range (0.000, 1)) = 0.1

    // TEMP
    [PowerSlidder, 3]_BoxLerp("Box mapping lerp", Range (0.000, 10)) = 0.1

    //-- SUN SETTINGS
    [Header(Sun Lighting)][Space(10)]
    _SunDir("Sun position", Vector) = (0.85,0.75,0.25,1.0)

    [PowerSlidder, 3]_SunStr("Sun Intensity",  Range (0.0, 1.0)) = 1
    _SunCol("Sun Color",Color) = (.25, .5, .5, 1)

    [PowerSlidder, 3] _SkyStr("Sky Strength",  Range (0.0, 1.0)) = 0.1
    _SkyCol("Sky Color",Color) = (.25, .25, .75, 1.0)
}

SubShader{
Tags { 
    "Queue" = "Transparent" 
    "RenderType" = "Transparent"
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

    //======================================================= Struct and Uniforms
    struct appdata{
        float4 pos : POSITION;
    };

    struct v2f{
        float4 pos : SV_POSITION;
        float3 eye : TEXCOORD1;
        float3 view : TEXCOORD2;
    };

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

    struct Model{
        uint id;
        float3 coord;
        float4 color;
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
    fixed4 _SunCol;
    fixed _SunStr;
    fixed3 _SkyCol;
    float _SkyStr;


    //======================================================= Methods
    // https://iquilezles.org/articles/smin
    float2 SMin( in float a, in float b, in float k ){

        float h = 1.0 - min( abs(a-b)/(4.0*k), 1.0 );
        float w = h*h;
        float m = w*0.5;
        float s = w*k;
        return (a<b) ? float2(a-s,m) : float2(b-s,1.0-m);

        // float f1 = exp2( -k*a );
        // float f2 = exp2( -k*b );
        // return float2(-log2(f1+f2)/k,f2);
    }

    //---------------------------------------- @block func
    float Map(in float3 pos, inout Model model){

        float an = _Time.y * 0.01;
        half no = Tex3DTricubicFast1(_NoiseTex, (pos * 0.5 * _FDisp + 0.5 + an), _NoiseTex_TexelSize.w);
        // model.disp = no * _ADisp;

        float d = 1e6;
        float near = d; // 2d closest distance used to id neighboor
        const uint n = _NumShapes;
        
        float4 col = 0;
        model.coord = 0;
        model.id = 0;

        [loop]
        for(uint i = 0; i < n; i++)
        {
            float3 coord = mul(_Shapes[i].txi, float4(pos, 1)).xyz;
            float2 uv = (coord.xy / _Shapes[i].scale);

            float clipBox = SdBox(uv, 0.499) * _Shapes[i].scale; // used to fill empty spaces left by the texture
            uv += 0.5;

            // element shape
            half nd = (UNITY_SAMPLE_TEX2DARRAY(_SdfArray, float3(uv, _Shapes[i].idSdf)).r - _Shapes[i].off2d) * _Shapes[i].scale;
            nd = max(nd, clipBox); // intersect clipbox

            // 3d, extrusion & round
            half ext = max( _Shapes[i].depth * (1 - _Shapes[i].round) * 0.5, 0.0001);
            half rnd = _Shapes[i].round * _Shapes[i].depth;
            
            nd += rnd;
            nd = OpExtrusion(coord, nd, ext) - rnd;
            // coord /= _Shapes[i].scale;

            float2 smooth = SMin( d, nd, max(_Shapes[i].blend, 0.00001) * 0.5 );
            d = smooth.x;
            
            float4 ncol = _ColArray.Sample(sampler_ColArray, float3(uv, _Shapes[i].idCol));
            model.color = lerp(model.color, ncol, smooth.y);

        }

        d -= no * _ADisp;

        return d;
    }

    //---------------------------------------- Import raymarching
    #define Map Map // define block map func
    #define Model Model

    #define MAX_STEP_SURF 64
    #define EPS_SURF 0.0005
    #define SCALE_MARCH 0.75
    #define MAX_STEP_SHA 4

    #include "../Includes/RayMarching.hlsl"

    //---------------------------------------- Shade
    fixed4 ShadeSurf(in Ray r, in Model m){

        float3 pos = r.or + r.dir * r.len;
        float3 nor = CalcNormal(pos,0.025);

        float3 col = m.color.rgb;
       
        float3 lin = 0;

        float ks = 1;
        float3 lig = normalize(_SunDir);
    
        float3 hal = normalize( lig-r.dir );
        float dif = clamp( dot(nor, lig.xyz), 0.0, 1.0);
        float sha = dif > 0.001 ? CalcSoftShadow( pos, lig.xyz, 0.01, 1.0, 32 ) * _SunStr : 1;
    
        dif *= sha;
    
        float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0);
        spe *= dif;
        spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);

        lin += col*2.20*dif*_SunCol;
        lin +=     5.00*spe*float3(1.30,1.00,0.70)*ks;

        // ----------------------------------------- SKY
        dif = sqrt(clamp( 0.5+0.5*nor.y, 0.0, 1.0 ));
        lin += col*0.6*dif*_SkyCol.rgb*_SkyStr;

        col = lin;
        col = lerp( col, float3(0.7,0.7,0.9), 1.0-exp( -0.0001*r.len*r.len*r.len ) );
        return fixed4(col, m.color.a);
    }

    //======================================================= Vertex
    v2f vert (appdata v){

        v2f o;
        o.pos = UnityObjectToClipPos(v.pos );

        float3 scl = GetScale(); // should be set as uniform !!
        
        o.view = -ObjSpaceViewDir(v.pos) * scl;
        o.eye = v.pos.xyz * scl - o.view;        
        return o;
    }

    //======================================================= Fragment
    fixed4 frag(v2f i) : SV_Target{

        Model m; Ray r = {i.eye, normalize(i.view), Map(i.eye, m)};
        
        if(March(r, length(i.view), m)){ return ShadeSurf(r, m);}

        return 0;
    }
ENDCG
}
}
}

