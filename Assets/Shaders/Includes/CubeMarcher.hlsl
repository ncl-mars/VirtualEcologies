// #include "UnityCG.cginc"
// return distance, id mate, uv

#ifndef _MAP
    inline float4 DefaultMap(in float3 pos)
    {
        return float4(length(pos)-0.01, 1, pos.xy);
    }
#define _MAP DefaultMap
#endif


#ifndef _SHADE_SURF
    inline fixed4 DefaultShadeSurf(in float3 ro, in float3 rd, in float4 res)
    {
        return 1;
    }
#define _SHADE_SURF DefaultShadeSurf
#endif


#ifndef _EPS_NOR
#define _EPS_NOR 0.05
#endif 
inline float3 CalcNormal( in float3 pos ){

    float2 e = float2(1.0,-1.0)*0.5773*_EPS_NOR;
    return normalize( e.xyy*_MAP( pos + e.xyy ).x + 
                      e.yyx*_MAP( pos + e.yyx ).x + 
                      e.yxy*_MAP( pos + e.yxy ).x + 
                      e.xxx*_MAP( pos + e.xxx ).x );
}

inline float3 CalcNormal( in float3 pos, float eps){

    float2 e = float2(1.0,-1.0)*0.5773*eps;
    return normalize( e.xyy*_MAP( pos + e.xyy ).x + 
                      e.yyx*_MAP( pos + e.yyx ).x + 
                      e.yxy*_MAP( pos + e.yxy ).x + 
                      e.xxx*_MAP( pos + e.xxx ).x );

}


#ifndef _MAX_STEP_SHA
#define _MAX_STEP_SHA 4
#endif 
inline float CalcSoftShadow( in float3 ro, in float3 rd, float tmin, float tmax, const float k ){

    float res = 1.0;
    float t = tmin;

    [unroll(_MAX_STEP_SHA)]
    for( int i=0; i<_MAX_STEP_SHA; i++ )
    {
        float h = _MAP( ro + rd*t ).x;
        res = min( res, k*h/t );
        t += clamp( h, 0.02, 0.20 );
        if( res<0.005 || t>tmax ) break;
    }
    return clamp( res, 0.0, 1.0 );
}


//---------------------------------------------------------- ray marcher
#ifndef _MAX_STEP_SURF
#define _MAX_STEP_SURF 16
#endif
#ifndef _EPS_SURF
#define _EPS_SURF 0.001
#endif

#ifndef _SCALE_MARCH
#define _SCALE_MARCH 1.0
#endif

float4 March(float3 ro, float3 rd, float tmin, float tmax){

    float t = tmin;
    float4 res = -1;

    // [unroll(_MAX_STEP_SURF)]
    [loop]
    for (int s = 0; s < _MAX_STEP_SURF; s++)
    {
        float4 h = _MAP(ro + rd * t);

#if defined(_THROUGH)
        float d = abs(h.x);
        if(d < _EPS_SURF) {
            res = float4(t, h.yzw);
            break;
        }
        t += d;

#elif defined(_IS_INSIDE)
        float d = -h.x;
        if(d < _EPS_SURF) {
            res = float4(t, h.yzw);
            break;
        }
        t += d;
#else
        if( abs(h.x) < _EPS_SURF) {
            res = float4(t, h.yzw);
            break;
        }
        t += h.x * _SCALE_MARCH;
#endif

        if(t > tmax) break;
    }

    return res;
}


// https://www.shadertoy.com/view/Xds3zN
fixed3 SunLight(in fixed3 col, in float3 pos, in float3 rd, in float3 nor, in float3 sun, in float str, float ksha)
{
    float ks = 1;
    float3 lig = normalize(sun);
    float sha = CalcSoftShadow( pos, lig.xyz, 0.01, 1.0, ksha ) * str;

    float3 hal = normalize( lig-rd );
    float dif = clamp( dot(nor, lig.xyz), 0.0, 1.0);
    dif *= sha;

    fixed3 lin = 0;
    float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0);
    spe *= dif;
    spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);
    lin += col*2.20*dif*float3(1.30,1.00,0.70);
    lin +=     5.00*spe*float3(1.30,1.00,0.70)*ks;

    return lin;
}