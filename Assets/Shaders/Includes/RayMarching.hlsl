// #include "UnityCG.cginc"
// return distance, id mate, uv

#ifndef Ray
    struct _DefaultRay{
        float3 or;
        float3 dir;
        float len;
    };
    #define Ray _DefaultRay
#endif

#ifndef Model
    struct _DefaultModel{
        uint id;
        float3 coord;
    };
    #define Model _DefaultModel
#endif

#ifndef Map
    inline float DefaultMap(in float3 pos, inout Model model)
    {
        return length(pos)-0.01;
    }
    #define Map DefaultMap
#endif

float3 CalcNormal( in float3 pos, const float eps){

    Model kk;
    float2 e = float2(1.0,-1.0)*0.5773*eps;
    return normalize(   e.xyy * Map( pos + e.xyy, kk ) + 
                        e.yyx * Map( pos + e.yyx, kk ) + 
                        e.yxy * Map( pos + e.yxy, kk ) + 
                        e.xxx * Map( pos + e.xxx, kk ) );

}

#ifndef MAX_STEP_SHA
    #define MAX_STEP_SHA 4
#endif 
float CalcSoftShadow( in float3 ro, in float3 rd, float tmin, float tmax, const float k ){

    float res = 1.0;
    float t = tmin;
    Model kk;

    [loop]
    for( int i=0; i<MAX_STEP_SHA; i++ )
    {
        float h = Map( ro + rd*t, kk);
        res = min( res, k*h/t );
        t += clamp( h, 0.02, 0.20 );
        if( res<0.005 || t>tmax ) break;
    }
    return clamp( res, 0.0, 1.0 );
}

//---------------------------------------------------------- ray marcher
#ifndef MAX_STEP_SURF
    #define MAX_STEP_SURF 32
#endif
#ifndef EPS_SURF   
    #define EPS_SURF 0.0005
#endif
#ifndef SCALE_MARCH
    #define SCALE_MARCH 0.75
#endif

bool March(inout Ray r, float tmax, inout Model m){

    [loop]
    for (int s = 0; s < MAX_STEP_SURF; s++)
    {
        float h = Map(r.or + r.dir * r.len, m);

        if( abs(h) < EPS_SURF) return true;
        
        r.len += h * SCALE_MARCH;

        if(r.len > tmax) break;
    }
    return false;
}


// https://www.shadertoy.com/view/Xds3zN
fixed3 SunLight(in fixed3 col, in float3 pos, in float3 rd, in float3 nor, in float3 sun, in float str, float ksha){

    float ks = 1;
    float3 lig = normalize(sun);

    float3 hal = normalize( lig-rd );
    float dif = clamp( dot(nor, lig.xyz), 0.0, 1.0);
    float sha = dif > 0.001 ? CalcSoftShadow( pos, lig.xyz, 0.01, 1.0, ksha ) * str : 1;

    dif *= sha;

    fixed3 lin = 0;
    float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0);
    spe *= dif;
    spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);
    lin += col*2.20*dif*float3(1.30,1.00,0.70);
    lin +=     5.00*spe*float3(1.30,1.00,0.70)*ks;

    return lin;
}

fixed3 SunLightCol(in fixed3 col, in float3 pos, in float3 rd, in float3 nor, in float3 sun, in float str, float ksha){

    float ks = 1;
    float3 lig = normalize(sun);

    float3 hal = normalize( lig-rd );
    float dif = clamp( dot(nor, lig.xyz), 0.0, 1.0);
    float sha = dif > 0.001 ? CalcSoftShadow( pos, lig.xyz, 0.01, 1.0, ksha ) * str : 1;

    dif *= sha;

    fixed3 lin = 0;
    float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0);
    spe *= dif;
    spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);
    lin += col*2.20*dif*float3(1.30,1.00,0.70);
    lin +=     5.00*spe*float3(1.30,1.00,0.70)*ks;

    return lin;
}