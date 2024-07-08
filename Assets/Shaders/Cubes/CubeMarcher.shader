/*
A cube shader that implements a raymarcher in local space, within the bound of a cube


// https://docs.unity3d.com/Manual/SL-BuiltinFunctions.html
*/
Shader "Cubes/CubeMarcher"
{
Properties
{
[Header(Input Textures)][Space(10)]
[NoScaleOffset] _ColTex ("Color Texture", 2D) = "" {}
[NoScaleOffset] _CtrTex ("Contour Texture", 2D) = "" {}
[NoScaleOffset] _SdfTex ("SDF Texture", 2D) = "" {}

[Header(Domain Extents)][Space(10)]
_ScaleUV("Scale uv",  Range (0.0, 2.0)) = 1

[Header(SDF Params)][Space(10)]
[Toggle]_Operation("0:Extrusion 1:Revolution", Int) = 0
[PowerSlidder, 3]_Off2dDist("Offset 2d sdf", Range (-0.5, 0.5)) = 0.0
[PowerSlidder, 3]_ExDepth("Extrusion depth", Range ( 0.0, 1.0)) = 0.25


//-- BASE MATE COLOR
[Header(Base Mate Color)][Space(10)]
_HueMate("Hue", Range (0.0, 1.0)) = 0.5
_SatMate("Saturation", Range (0.0, 1.0)) = 0.5
_ValMate("Value", Range (0.0, 1.0)) = 0.5

//-- SUN SETTINGS
[Header(Sun Lighting)][Space(10)]
_SunDir("Sun direction", Vector) = (0.85,0.75,0.25,1.0)
_SunStr("Sun Intensity",  Range (0.0, 1.0)) = 1


}

SubShader
{
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
    #include "../Includes/Interpolators.hlsl"

    #include "../Includes/Utils.hlsl"


    Texture2D _SdfTex, _ColTex, _CtrTex;
    SamplerState sampler_SdfTex;
    float4 _SdfTex_TexelSize;

    float4 _Extents;

    float _Off2dDist; //_OffExDist
    float _ExDepth;
    bool _Operation;

    fixed4 _SunDir;
    fixed _SunStr;

    float _HueMate, _SatMate, _ValMate;
    float _ScaleUV;

    struct appdata{
        float4 pos : POSITION;
    };

    struct v2f{
        float4 pos : SV_POSITION;
        float3 eye : TEXCOORD1;
        float3 view : TEXCOORD2;
    };

    v2f vert (appdata v){
        
        v2f o;
        o.pos = UnityObjectToClipPos(v.pos );

        float3 scl = GetScale();
        o.view = -ObjSpaceViewDir(v.pos) * scl;
        o.eye = v.pos.xyz * scl - o.view; // calculate eye ray in object space
        
        return o;
    }

    //---------------------------------------------------------- maps
    float4 Map(in float3 pos){

        float2 uv = (_Operation>0) ? OpRevolution(pos, 0.0) : pos.xy;
        uv *= _ScaleUV;
        uv += 0.5;

        float d = _SdfTex.Sample(sampler_SdfTex, uv).r;
        float m = 1;

        d += _Off2dDist;
        d = (_Operation>0) ? d : OpExtrusion(pos, d, _ExDepth*0.5);

        return float4(d, m, uv);
    }

    #define _MAP Map
    #include "../Includes/CubeMarcher.hlsl"

    // https://www.shadertoy.com/view/Xds3zN
    fixed4 ShadeSurf(in float3 ro, in float3 rd, in float4 res)
    {
        float3 pos = ro + rd * res.x;
        float3 nor = CalcNormal(pos);

        fixed3 col = _ColTex.Sample(sampler_SdfTex, res.zw).rgb;
        col = HueShift(col, _HueMate);

        float3 ctr = _CtrTex.Sample(sampler_SdfTex, pos.xy * _ScaleUV + 0.5).rgb;
        col = Saturation(col + ctr, _SatMate);
        col *= _ValMate;

        float3 lig = normalize(_SunDir);
        float sha = CalcSoftShadow( pos, lig.xyz, 0.01, 1.0, 8.0 ) * _SunStr;
        float ks = 1;

        float3 lin = 0;

        {   //----------------------------------------- SUN
            float3 hal = normalize( lig-rd );
            float dif = clamp( dot(nor, lig.xyz), 0.0, 1.0);
            dif *= sha;

            float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0);
            spe *= dif;
            spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);
            lin += col*2.20*dif*float3(1.30,1.00,0.70);
            lin +=     5.00*spe*float3(1.30,1.00,0.70)*ks;
        }

        {   //----------------------------------------- SKY
            float dif = sqrt(clamp( 0.5+0.5*nor.y, 0.0, 1.0 ));
            lin += col*0.60*dif*float3(0.40,0.60,1.15);
        }

        col = lin;
        col = lerp( col, float3(0.7,0.7,0.9), 1.0-exp( -0.0001*res.x*res.x*res.x ) );
        
        return fixed4(col, 1);
    }
    

    //_________________________________________________________________________________
    ///////////////////////////////////////////////////////////////////////////////////
    fixed4 frag(v2f i) : SV_Target
    {
        float3 rd = normalize(i.view);
        float3 ro = i.eye;
        float4 res = March(ro, rd, Map(ro+rd*0.01).x, length(i.view));
        
        if(res.x > 0.0){
            return ShadeSurf(ro, rd, res);
        }

        return 0;
    }
ENDCG
}
}
}






//________________________________________________________________________________________________
