/*
    https://bgolus.medium.com/anti-aliased-alpha-test-the-esoteric-alpha-to-coverage-8b177335ae4f

    => are these optional ??
    // AlphaToMask On // This could behave oddly without MSAA
    // AlphaTest Greater [_Cutoff]
*/

Shader "Particles/FieldParticles"{
Properties
{
    [Header(Input Textures)][Space(10)]
    [NoScaleOffset] _NoiseTex ("Noise Texture", 2D) = "" {}
}

SubShader{
Tags { 
    "Queue"="AlphaTest"
    "RenderType"="TransparentCutout" 
}

Cull Off
Blend SrcAlpha OneMinusSrcAlpha

Pass
{
    AlphaTest Greater 0.1
    CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag
    
    #pragma multi_compile _ALTI_PATH _GRAV_TOPO _PLATEFORM

    #include "UnityCG.cginc"
    #include "../Includes/Quads.hlsl"
    #include "../Includes/Particles.hlsl"
    #include "../Includes/Noises.hlsl"
    #include "../Includes/ColorFunctions.hlsl"
    #include "../Includes/Utils.hlsl"
    #include "../Includes/TransformUtils.hlsl"

    #define PI 3.14159265359

    //======================================================= Struct
    // https://docs.unity3d.com/Manual/SL-VertexFragmentShaderExamples.html
    struct appdata{

        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float3 nor : NORMAL;
    };

    struct v2f{

        float2 uv       : TEXCOORD0;
        float4 pos      : SV_POSITION;
        uint id         : SV_InstanceID;
        float4 color    : COLOR0;

        // float3 nor      : NORMAL;

        // hold a 3x3 rotation matrix that transforms from tangent to world space
        half3 tspace0 : TEXCOORD1; // tangent.x, bitangent.x, normal.x
        half3 tspace1 : TEXCOORD2; // tangent.y, bitangent.y, normal.y
        half3 tspace2 : TEXCOORD3; // tangent.z, bitangent.z, normal.z
    };

    #define MeshNormals(i)          float3(i.tspace0.z, i.tspace1.z, i.tspace2.z)
    #define TangentToWorld(i, v)    float3(dot(i.tspace0,v), dot(i.tspace1,v), dot(i.tspace2,v))


    UNITY_DECLARE_TEX2DARRAY(_Sprites);
    // Texture2DArray _Sprites;

    Texture2D _NoiseTex;
    Texture2D _BumpMap;
    SamplerState SamplerLinearRepeat;

    float4 _GV;
    #define _NumParticles   (int)_GV.x
    #define _NumSprites     (int)_GV.y

    #define UVB_Length 16

    // unpack uniform buffer
    float4 _UVB[UVB_Length];
    #define _Settings   _UVB[0] // x cutoff
    #define _Origin     _UVB[1] //
    #define _Extents    _UVB[2] // xyz, w = system scale 

    #define _Col0       _UVB[3]
    #define _Col1       _UVB[4]
    #define _Col2       _UVB[5]
    #define _Col3       _UVB[6]
    #define _Palette    _UVB[7]

    #define _Trs0       _UVB[8] // x : particle size

    #define REND_SCALE _Extents[3] * 0.1 * _Origin[3]

    //======================================================= Vertex
    v2f vert (appdata v, uint id : SV_InstanceID){

        v2f o;
        float3 pos = v.vertex.xyz; // vertex is world pos
        
        uint2 id2 = IdtToPtc(id, _Positions_TexelSize.zw);
        ParticleData data = UnpackBuffers(id2); 

        //--------------------------------------------- apply S,R,T
        pos *= _Trs0[1] * REND_SCALE;
        pos *= saturate(ceil(Dot2(data.vel))); // hide if no velocity

        if(_Trs0[0] < 3.5){
            pos = -mul((float3x3)unity_CameraToWorld, pos);

            float3 nor = mul((float3x3)unity_CameraToWorld, v.nor);
            float3 xa = mul((float3x3)unity_CameraToWorld, float3(1,0,0));
            float3 ya = cross(xa, nor);

            o.tspace0 = half3(xa.x, ya.x, nor.x);
            o.tspace1 = half3(xa.y, ya.y, nor.y);
            o.tspace2 = half3(xa.z, ya.z, nor.z);
        }

        else if(_Trs0[0] < 4.5){
            float3 view = normalize(data.pos - _WorldSpaceCameraPos);
            float3 xa = normalize(data.vel);
            float3 ya = cross(xa, view);
            float3 za = cross(xa, ya);

            float3x3 look = (float3x3)AxisMatrix(xa, ya, za);
            pos = mul(look, pos);

            o.tspace0 = half3(xa.x, ya.x, za.x);
            o.tspace1 = half3(xa.y, ya.y, za.y);
            o.tspace2 = half3(xa.z, ya.z, za.z);
        }

        else{
            float3 xa = normalize(data.vel);
            float3 ya = float3(0,1,0);
            float3 za = cross(xa, ya);

            float3x3 look = (float3x3)AxisMatrix(xa, ya, za);
            pos = mul(look, pos);
            // o.nor = mul(look, v.nor);

            o.tspace0 = half3(xa.x, ya.x, za.x);
            o.tspace1 = half3(xa.y, ya.y, za.y);
            o.tspace2 = half3(xa.z, ya.z, za.z);
        }

        
        pos += data.pos;
        //---------------------------------------------
        float zone = data.zone;

        #if _ALTI_PATH | _GRAV_TOPO
            zone = frac(zone);
        #endif
        uint dim = sqrt(_NumParticles);
        float no = _NoiseTex[IdtToPtc(id, (uint2)dim)].r * 2 - 1;

        zone *= _Palette[0];
        zone += _Palette[1];
        zone += no *_Palette[2] * _Palette[0];

        o.color.rgb = Palette(
            zone,
            _Col0.rgb,
            _Col1.rgb,
            _Col2.rgb,
            _Col3.rgb);

        o.color.a = 1;

        //--------------------------------------------- write out
        // o.view = pos - _WorldSpaceCameraPos;
        o.pos = mul(UNITY_MATRIX_VP, float4(pos, 1));
        o.id = id;
        o.uv = v.uv;
        return o;
    }


    //======================================================= Fragment
    float3 GetLights(v2f i, ParticleData P){
        
        half3 view = normalize(P.pos - _WorldSpaceCameraPos);
        float4 bump = _BumpMap.Sample(SamplerLinearRepeat, i.uv);
        
        float3 bnor = TangentToWorld(i, bump.xyz);
        float3 nor = MeshNormals(i);

        nor = normalize(lerp(nor, bnor, 2.0)); // bump strength

        float3 rd = normalize(view);
        float3 ref = reflect(rd, nor);
        fixed3 mate = i.color.rgb;

        float3 lin = 0;
        {   // Sun
            float3 lig = normalize( float3(-0.5, 0.4, -.75) );

            float3 hal = normalize( lig-rd );
            float dif = clamp( dot( nor, lig ), 0.0, 1.0 );

            float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0);
            spe *= dif;
            spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);

            lin += mate * dif;
        }
        {   // Sky
            float dif = sqrt(clamp( 0.5+0.5*nor.y, 0.0, 1.0 ));
            float spe = smoothstep( -0.2, 0.2, ref.y );
            spe *= dif;
            spe *= 0.04+0.96*pow(clamp(1.0+dot(nor,rd),0.0,1.0), 5.0 );

            lin += mate*0.60*dif*float3(0.40,0.60,1.15);
            lin +=      2.00*spe*float3(0.40,0.60,1.30); 
        }
       return lin;
    }

    fixed4 frag (v2f i, uint id : SV_InstanceID) : SV_Target{

        // return 1;

        //---------------------------------------------
        uint2 dim = pow((float)_NumParticles, 0.5);
        float2 posBoid = PtcToNtc(IdtToPtc(id, dim), dim);

        float noise = _NoiseTex.Sample(SamplerLinearRepeat, posBoid * 0.5 + 0.5).r;
        float slice = (noise * 20.0) % _NumSprites;
        slice = floor(slice);

        fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_Sprites, float3(i.uv, slice) );
        clip(col.a - _Settings[0]); // discard on alpha


        //---------------------------------------------
        uint2 id2 = IdtToPtc(id, _Positions_TexelSize.zw);
        ParticleData P = UnpackBuffers(id2);

        col = i.color;

        col.rgb = GetLights(i, P);


        return col;
    }
    ENDCG
}
}
}



// //======================================================= Methods
// float CalcMipLevel(float2 texel){

//     float2 dx = ddx(texel);
//     float2 dy = ddy(texel);
//     float deltaMaxSqr = max(dot(dx, dx), dot(dy, dy));
    
//     return max(0.0, 0.5 * log2(deltaMaxSqr));
// }

// float RecomputeAlpha(float2 uv, float alpha){

//     float mipScale = 4;
//     float2 spriteRes = 32.0;
//     float cutoff = 0.99;
//     alpha *= 1 + max(0, CalcMipLevel(uv * spriteRes)) * mipScale;
//     alpha = (alpha - cutoff) / max(fwidth(alpha), 0.0001) + 0.5; // rescale alpha by partial derivative
//     return alpha;
// }