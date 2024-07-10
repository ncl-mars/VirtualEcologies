// Operations taken directly from https://iquilezles.org/articles/distfunctions/



#ifndef SDF_OP_INCLUDED
    #define SDF_OP_INCLUDED
    ///////////////////////////////////////////////////////////////////
    // Primitive combinations
    //-- simple
    float OpUnion(in float d1, in float d2){        return min(d1,d2); }
    float OpSubtraction(in float d1, in float d2){  return max(-d1,d2); }
    float OpIntersection(in float d1, in float d2){ return max(d1,d2); }

    float OpSmoothUnion(in float d1, in float d2, in float k){
        float h = max(k-abs(d1-d2),0.0);
        return min(d1, d2) - h*h*0.25/k;
    }

    float OpSmoothSubtraction(in float d1, in float d2, in float k){    return -OpSmoothUnion(d1,-d2,k); }
    float OpSmoothIntersection(in float d1, in float d2, in float k){   return -OpSmoothUnion(-d1,-d2,k); }

    float2 OpSmooth2Union(in float d1, in float d2, in float k){
        float h = max(k-abs(d1-d2),0.0);
        return float2(min(d1, d2) - h*h*0.25/k, h);
    }

    float2 OpSmooth2Subtraction(in float d1, in float d2, in float k){    return -OpSmooth2Union(d1,-d2,k); }
    float2 OpSmooth2Intersection(in float d1, in float d2, in float k){   return -OpSmooth2Union(-d1,-d2,k); }

    float OpXOr(in float a, in float b){ return max(min(a, b), -max(a, b)); }


    //-- with id
    float2 OpUnion(in float2 d1, in float2 d2){         return d1.x<d2.x ? d1:d2 ; }
    float2 OpSubtraction(in float2 d1, in float2 d2){   return -d1.x>d2.x ? d1:d2; }
    float2 OpIntersection(in float2 d1, in float2 d2){  return d1.x>d2.x ? d1:d2; }


    ///////////////////////////////////////////////////////////////////
    // Smooth Minimums => https://iquilezles.org/articles/smin/
    // Cubic Polynomial Smooth-minimum
    float2 OpCubicSMin2(in float d1, in float d2, in float k ){
        k *= 6.0;
        float h = max( k-abs(d1-d2), 0.0 )/k;
        float m = h*h*h*0.5;
        float s = m*k*(1.0/3.0); 
        return d1<d2 ? float2(d1-s,m):float2(d2-s,1.0-m);
    }

    // quadratic polynomial
    float OpQuadSMin( float d1, float d2, float k ){
        k *= 4.0;
        float h = max( k-abs(d1-d2), 0.0 )/k;
        return min(d1,d2) - h*h*k*(1.0/4.0);
    }

    float2 OpQuadSMin2( float d1, float d2, float k ){
        k *= 4.0;
        float h = max( k-abs(d1-d2), 0.0 )/k;
        return float2(min(d1,d2) - h*h*k*(1.0/4.0), h);
    }

    ///////////////////////////////////////////////////////////////////
    // Shape operation
    float4 OpElongate(in float3 p, in float3 h){
        float3 q = abs(p)-h;
        return float4( max(q,0.0), min(max(q.x,max(q.y,q.z)),0.0) );
    }

    float OpExtrusion(in float3 p, in float d, in float h){
        float2 w = float2( d, abs(p.z) - h );
        return min(max(w.x,w.y),0.0) + length(max(w,0.0));
    }

    float2 OpRevolution(in float3 p, in float w){   return float2( length(p.xz) - w, p.y );}

    float OpOnion(in float d, in float h){  return abs(d)-h;}

    ///////////////////////////////////////////////////////////////////
    // Domain operation
    // https://iquilezles.org/articles/sdfrepetition/ for repetition with ids, check article !

    // 2D
    float2 OpRepLim(in float2 p, in float s, in float2 lima, in float2 limb){
        return p-s*clamp(round(p/s),lima,limb);
    }

    float2 OpRepLim(in float2 p, in float s, in float2 lim){
        return p-s*clamp(round(p/s),-lim,lim);
    }

    float2 OpRep(in float2 p, in float s){
        return p-s*round(p/s);
    }

    // 3D
    float3 OpRepLim(in float3 p, in float s, in float3 lim){
        return p-s*clamp(round(p/s),-lim,lim);
    }

    float3 OpRepLim(in float3 p, in float s, in float3 lima, in float3 limb){
        return p-s*clamp(round(p/s),lima,limb);
    }

    float3 OpRep(in float3 p, in float s){
        return p-s*round(p/s);
    }

    ///////////////////////////////////////////////////////////////////
    // Distortions
    float3 OpTwist(in float3 p){

        const float k = 10.0; // or some other amount
        float c = cos(k*p.y);
        float s = sin(k*p.y);

        float2x2  m = float2x2(c,-s,s,c);

        return float3(mul(m, p.xz),p.y);
    }

    float3 OpCheapBend(in float3 p){
        
        const float k = 10.0; // or some other amount
        float c = cos(k*p.x);
        float s = sin(k*p.x);
        float2x2  m = float2x2(c,-s,s,c);
        
        return float3(mul(m,p.xy),p.z);
    }
    
#endif























// __________________________________________________________________________________FOOT
// Quadratic polynomial = older / slower
// float2 OpQuadSMin(in float d1, in float d2, in float k ){
//     float h = clamp( 0.5+0.5*(d2-d1)/k, 0.0, 1.0 );
//     return float2( lerp(d2, d1, h) - k*h*(1.0-h), h);
// }

//---------------------------------------------------------- makes sens ?
// float2 OpSmoothUnion(in float2 d1, in float2 d2, in float k){
//     float h = max(k-abs(d1.x-d2.x),0.0);
//     h = h*h*0.25/k;
//     return d1.x<d2.x ? float2(d1.x - h, d1.y):float2(d2.x - h, d2.y);
// }

// float2 OpSmoothSubtraction(in float2 d1, in float2 d2, in float k){     return -OpSmoothUnion(d1,-d2,k); }
// float2 OpSmoothIntersection(in float2 d1, in float2 d2, in float k){    return -OpSmoothUnion(-d1,-d2,k); }

// float3 OpSmooth2Union(in float2 d1, in float2 d2, in float k){
//     float h = max(k-abs(d1.x-d2.x),0.0);
//     h = h*h*0.25/k;
//     return d1.x<d2.x ? float3(d1.x-h, h, d1.y):float3(d2.x-h, h, d2.y);
// }

// float3 OpSmooth2Subtraction(in float2 d1, in float2 d2, in float k){     return -OpSmooth2Union(d1, float2(-d2.x, d2.y), k); }
// float3 OpSmooth2Intersection(in float2 d1, in float2 d2, in float k){    return -OpSmooth2Union(float2(-d1.x, d1.y), float2(-d2.x, d2.y),k); }
