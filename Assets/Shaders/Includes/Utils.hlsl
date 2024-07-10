/*
*/

#ifndef UTILS_INCLUDED
#define UTILS_INCLUDED

    #ifndef Rot2
        #define Rot2(a) float2x2( cos(a), -sin(a), sin(a), cos(a) )
    #endif
    #ifndef PI
        #define PI 3.14159265359
    #endif
    #ifndef Dot2
        #define Dot2(x) dot(x,x)
    #endif

    #ifndef MinComp
        #define MinComp(v) min(min(v.x, v.y), v.z)
    #endif
    #ifndef MaxComp
        #define MaxComp(v) max(max(v.x, v.y), v.z)
    #endif

    // WARNING GENERATOR REDEFINITION !!!
    uint2 IdtToPtc(uint id, uint2 dim){

        uint xQ = id / dim.x;
        uint x 	= id % dim.x;
        uint y 	= xQ % dim.y;
        return uint2(x, y);
    }

    float2 PtcToNtc(uint2 pixel, float2 dim){
        return (2.0 * (pixel + 0.5) - dim) / min(dim.x, dim.y); // [-1, 1]
    }

    // PTC for pixel target coordinate, convert 2d pixel to 1d index 
    uint PtcToIdt(uint2 pixel, uint2 dim){

        pixel = clamp(pixel, 0, dim - 1);
        return pixel.x + pixel.y * dim.x;
    }


    float2 ClampMag( float2 v, float minmag, float maxmag ){

        float mag = clamp( length( v ), minmag, maxmag );
        v = normalize( v ) * mag;
        return v;
    }

    float3 ClampMag( float3 v, float minmag, float maxmag ){

        float mag = clamp( length( v ), minmag, maxmag );
        v = normalize( v ) * mag;
        return v;
    }

    // http://mathworld.wolfram.com/Quaternion.html
    float4 QMul(float4 q1, float4 q2){

        return float4(
            q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
            q1.w * q2.w - dot(q1.xyz, q2.xyz)
        );
    }
    float3 RotateVector(float3 v, float4 r){

        float4 r_c = r * float4(-1, -1, -1, 1);
        return QMul(r, QMul(float4(v, 0), r_c)).xyz;
    }

    float ExpStep( float x, float n ){

        return exp2( -exp2(n)*pow(x,n) );
    }

    float Parabola( float x, float k ){

        return pow( 4.0*x*(1.0-x), k );
    }

    float Falloff( float x, float m ){

        x /= m;
        return (x-2.0)*x+1.0;
    }

    // ================================================================== Object Utils
    #ifdef UNITY_CG_INCLUDED
        inline float3 GetScale(){
            return float3(
                length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)),
                length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)),
                length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z)));
        }

        inline float3 ToLocal(float3 pos){
            return mul(unity_WorldToObject, float4(pos, 1.0)).xyz;
        }

        inline float3 ToWorld(float3 pos){
            return mul(unity_ObjectToWorld, float4(pos, 1.0)).xyz;
        }
    #endif

#endif