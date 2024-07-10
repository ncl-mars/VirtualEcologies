//Primitives taken directly from https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

#ifndef SDF_PRIM_BASE_INCLUDED
    #define SDF_PRIM_BASE_INCLUDED
    // Sphere - exact
    float SdSphere(in float3 p, in float r){    return length(p) - r;}

    // Box - exact
    float SdBox(in float3 p, in float3 b){
        float3 q = abs(p) - b;
        return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0);
    }

    float SdBox( in float2 p, in float2 b ){
        float2 d = abs(p)-b;
        return length(max(d,0.0)) + min(max(d.x,d.y),0.0);
    }

    // Box distances + normals
    float4 SdNorBox(in float3 p, in float3 b){
        float3 d = abs(p) - b;
        float x = min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
        float3 n = step(d.yzx,d.xyz)*step(d.zxy,d.xyz)*sign(p);
        return float4( x, n );
    }

    // Round Box - exact
    float SdRoundBox(in float3 p, in float3 b, in float r){
        float3 q = abs(p) - b;
        return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0) - r;
    }

    // Bounding Box - exact
    float SdBoundingBox(in float3 p, in float3 b, in float e){
        p = abs(p  )-b;
        float3 q = abs(p+e)-e;
        return min(min(
            length(max(float3(p.x,q.y,q.z),0.0))+min(max(p.x,max(q.y,q.z)),0.0),
            length(max(float3(q.x,p.y,q.z),0.0))+min(max(q.x,max(p.y,q.z)),0.0)),
            length(max(float3(q.x,q.y,p.z),0.0))+min(max(q.x,max(q.y,p.z)),0.0));
    }

    // Plane - exact
    float SdPlane(in float3 p, in float3 n, in float h){
        // n must be normalized
        return dot(p,n) + h;
    }
    
#endif
