/*
A cube shader that implements a raymarcher in local space, within the bound of a cube
// https://docs.unity3d.com/Manual/SL-BuiltinFunctions.html
*/
Shader "Cubes/ExtrudeSubSurf"
{
Properties
{
[Header(Input Textures)][Space(10)]
[NoScaleOffset] _ColTex ("Color Texture", 2D) = "" {}
[NoScaleOffset] _CtrTex ("Contour Texture", 2D) = "" {}
[NoScaleOffset] _SdfTex ("SDF Texture", 2D) = "" {}
_TexST("Scale offset textures",  Vector) = (1,1,0,0)

[NoScaleOffset] _NoiseTex ("Noise Texture", 3D) = "" {}

[Header(SDF Params)][Space(10)]
// [Toggle]_Operation("0:Extrusion 1:Revolution", Int) = 0
[PowerSlidder, 3]_Off2dDist("Offset 2d sdf", Range (-0.5, 0.5)) = 0.0
[PowerSlidder, 3]_ExDepth("Extrusion depth", Range ( 0.0, 1.0)) = 0.25
[PowerSlidder, 3]_Round("round 3d shap", Range (0, 0.25)) = 0.0

[PowerSlidder, 2]_FDisp("displace freq", Range (0.0001, 2)) = 1
[PowerSlidder, 3]_ADisp("displace amp", Range (0.000, 1)) = 0.1

//-- BASE MATE COLOR
[Header(Base Mate Color)][Space(10)]
_HueMate("Hue", Range (0.0, 1.0)) = 0.5
_SatMate("Saturation", Range (0.0, 1.0)) = 0.5
_ValMate("Value", Range (0.0, 1.0)) = 0.5
_Alpha("Alpha", Range (0.0, 1.0)) = 0.5

//-- SUN SETTINGS
[Header(Sun Lighting)][Space(10)]
_SunDir("Sun direction", Vector) = (0.85,0.75,0.25,1.0)

_SunDifCol("Diffuse Color", Color) = (0.85,0.75,0.25,1.0)
[PowerSlidder, 3]_SunDifStr("Diffuse Strength", Range (0.0, 1.0)) = 0.1

_SunSpeCol("Specular Color", Color) = (0.85,0.75,0.25,1.0)
[PowerSlidder, 3]_SunSpeStr("Specular Strength", Range (0.0, 1.0)) = 0.1

_SunStr("Sun Intensity",  Range (0.0, 1.0)) = 1

//-- SKY SETTINGS
[Header(Sky Lighting)][Space(10)]
_SkyDifCol("Diffuse color", Color) = (0.85,0.75,0.25,1.0)
[PowerSlidder, 3]_SkyDifStr("Diffuse Strength", Range (0.0, 1.0)) = 0.1

_SkySpeCol("Specular Color", Color) = (0.85,0.75,0.25,1.0)
[PowerSlidder, 3]_SkySpeStr("Specular Strength", Range (0.0, 1.0)) = 0.1

_SkyBouCol("Bounce Color", Color) = (0.85,0.75,0.25,1.0)
[PowerSlidder, 3]_SkyBouStr("Bounce Strength", Range (0.0, 1.0)) = 0.1

_SkyShaMix("Sun Shadows Mix",  Range (0.0, 1.0)) = 1.0
}

SubShader
{
Tags { 
    "Queue" = "Transparent" 
    // "RenderType" = "Transparent" 
    "RenderType" = "TransparentCutout" 
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

    sampler3D _NoiseTex;

    SamplerState sampler_SdfTex;
    float4 _SdfTex_TexelSize;
    float4 _TexST;

    float _Off2dDist; //_OffExDist
    float _ExDepth;
    float _Round;

    fixed4 _SunDir;
    fixed4 _SunDifCol, _SkyDifCol, _SkyBouCol, _SunSpeCol, _SkySpeCol;
    fixed _SunDifStr, _SkyDifStr, _SkyBouStr, _SunSpeStr, _SkySpeStr, _SunStr, _SkyShaMix;

    float _HueMate, _SatMate, _ValMate, _Alpha;
    float _FDisp, _ADisp;

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
        float3 scl = GetScale(); // REPLACE THIS BY UNIFORM

        o.view = -ObjSpaceViewDir(v.pos) * scl;
        o.eye = v.pos.xyz * scl - o.view; // calculate eye ray in object space
        
        return o;
    }

    //---------------------------------------------------------- ray marcher block
    // @block _MAP
    float4 Map(in float3 pos){

        float2 uv = pos.xy*_TexST.xy + 0.5 + _TexST.zw;

        float m = 1; // material
        float d = _SdfTex.Sample(sampler_SdfTex, uv).r;

        d += _Off2dDist;
        d = OpExtrusion(pos, d, _ExDepth*0.5) - _Round;

        float no = tex3D(_NoiseTex, (pos * 0.5 + 0.5)  * _FDisp + _Time.y * 0.00125) * _ADisp;
        d += no;

        return float4(d, m, uv);
    }

    #define _MAP Map
    #define _MAX_STEP_SURF 32
    #define _MAX_STEP_SHA 4
    #define _EPS_SURF 0.001
    // #define _IS_INSIDE // _THROUGH
    #include "../Includes/CubeMarcher.hlsl"

    //----------------------------------------------------------
    fixed4 ShadeSurf(in float3 ro, in float3 rd, in float4 res){

        fixed3 col = 0;

        float3 pos = ro + rd*res.x;
        float3 nor = CalcNormal(pos);

        float2 uv = pos.xy*_TexST.xy + 0.5 + _TexST.zw;

        col = _ColTex.Sample(sampler_SdfTex, uv).rgb;
        float3 ctr = _CtrTex.Sample(sampler_SdfTex, uv).rgb;

        uv = (pos + rd * _ExDepth * 2).xy * _TexST.xy + 0.5 + _TexST.zw;

        fixed3 sub = _ColTex.Sample(sampler_SdfTex, uv).rgb * 0.75;
        col = lerp(col, sub, 0.25);

        col = HueShift(col, _HueMate);
        col = Saturation(col + ctr, _SatMate);
        col *= _ValMate;

        float3 ref = reflect(rd, nor);
        float3 lig = normalize(_SunDir);
        float sha = CalcSoftShadow( pos, lig.xyz, 0.01, 1.0, 8.0 ) * _SunStr;
        // sha = 1;

        float3 lin = 0;

        {   //----------------------------------------- SUN
            float3 hal = normalize( lig-rd );
            float fre = clamp(1.0+dot(nor,rd), 0.0, 1.0 );

            float dif = clamp( dot(nor, lig.xyz), 0.0, 1.0);
            dif *= sha;

            float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0);
            spe *= dif;
            spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);

            float amp = 10;
            lin += col * _SunDifStr * _SunDifCol.rgb * dif * amp * fre;
            lin +=       _SunSpeStr * _SunSpeCol.rgb * spe * amp * 5;
        }

        {   //----------------------------------------- SKY
            sha = lerp(1,sha, _SkyShaMix);

            float dif = sqrt(clamp( 0.5+0.5*nor.y, 0.0, 1.0 ));
                dif *= sha;
            float spe = smoothstep( -0.2, 0.2, ref.y );
                spe *= dif;
                spe *= 0.04+0.96*pow(clamp(1.0+dot(nor,rd),0.0,1.0), 5.0 );
            float bou = clamp( 0.3-0.7*nor.y, 0.0, 1.0 );
                bou *= sha;

            float amp = 5;
            lin += col * _SkyDifStr * _SkyDifCol.rgb * dif * amp;
            lin +=       _SkySpeStr * _SkySpeCol.rgb * spe * amp * 2;
            lin += ctr * _SkyBouStr * _SkyBouCol.rgb * bou * amp * 0.1;
        }
        col = lin;
        col = lerp( col, float3(0.7,0.7,0.9), 1.0-exp( -0.0001*res.x*res.x*res.x ) );

        return fixed4(col, _Alpha);
    }

    //_________________________________________________________________________________
    ///////////////////////////////////////////////////////////////////////////////////
    fixed4 frag(v2f i) : SV_Target{

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
    
    // {   // TRANSPARENT
    //     fixed3 oriCol = col;
    //     float dz = length(view) - res.x;
    //     float ao = clamp(dz*50.0,0.0,1.0);

    //     float fre = clamp( 1.0 + dot( rd, nor ), 0.0, 1.0 );
    //     float3  hal = normalize( lig-rd );
    //     float spe1 = clamp( dot(nor,hal), 0.0, 1.0 );
    //     float spe2 = clamp( dot(ref,lig), 0.0, 1.0 );
    
    //     float ds = 1.6 - col.y;
        
    //     col *= lerp( float3(0.0,0.0,0.0), float3(0.4,0.6,0.4), ao );
    
    //     col += ds*1.5*float3(1.0,0.9,0.8)*pow( spe1, 80.0 );
    //     col += ds*0.2*float3(0.9,1.0,1.0)*smoothstep(0.4,0.8,fre);
    //     col += ds*0.9*float3(0.6,0.7,1.0)*smoothstep( -0.5, 0.5, -reflect( rd, nor ).y )*smoothstep(0.2,0.4,fre);    
    //     col += ds*0.5*float3(1.0,0.9,0.8)*pow( spe2, 80.0 );
    //     col += ds*0.5*float3(1.0,0.9,0.8)*pow( spe2, 16.0 );
    
    //     col += float3(0.8,1.0,0.8)*0.5*smoothstep(0.3,0.6, _ColTex.Sample(sampler_SdfTex, 0.8*nor.xy).rgb )*(0.1+0.9*fre*fre);
    //     // col += float3(0.8,1.0,0.8)*0.5*smoothstep(0.3,0.6,text1( 0.8*nor.xy ))*(0.1+0.9*fre*fre);

    //     // hide aliasing a bit
    //     col = lerp( col, oriCol, smoothstep(0.6,1.0,fre) );
    //     alpha = _Alpha;
    // }
