/*
    RAY TRACING FUNTIONS
    Intersection functions used for optimization (raymarch bouding box, sphere...)
    https://iquilezles.org/articles/boxfunctions

    plane intersection : https://www.shadertoy.com/view/lsfGDB

    siggraph implementation at :
    https://github.com/gillesferrand/Unity-RayTracing/blob/master/Assets/RayCaster.shader

*/

// ------------------------------------------------------------------- Grid Cell Intersect
// Calcs intersection and exit distances, and normal at intersection.
// The ray must be in box/object space. If you have multiple boxes all
// aligned to the same axis, you can precompute 1/rd. If you have
// multiple boxes but they are not alligned to each other, use the 
// "Generic" box intersector bellow this one.
float2 IBox(in float3 ro, in float3 rd, in float3 rad, out float3 oN){

    float3 m = 1.0/rd;
    float3 n = m*ro;
    float3 k = abs(m)*rad;
    float3 t1 = -n - k;
    float3 t2 = -n + k;

    float tN = max( max( t1.x, t1.y ), t1.z );
    float tF = min( min( t2.x, t2.y ), t2.z );
	
    if( tN>tF || tF<0.0) return (float2)-1.0; // no intersection
    
    oN = -sign(rd)*step(t1.yzx,t1.xyz)*step(t1.zxy,t1.xyz);

    return float2( tN, tF );
}

// Calcs intersection and exit distances, NO normals
float2 IBox(in float3 ro, in float3 rd, in float3 rad){

    float3 m = 1.0/rd;
    float3 n = m*ro;
    float3 k = abs(m)*rad;
    float3 t1 = -n - k;
    float3 t2 = -n + k;
    float tN = max( max( t1.x, t1.y ), t1.z );
    float tF = min( min( t2.x, t2.y ), t2.z );
    if( tN > tF || tF < 0.0) return (float2)-1.0;
    return float2( tN, tF );
}

// ------------------------------------------------------------------- Generic Box 
// "Generic" box intersector working both for ray origin in|out the box
// https://www.shadertoy.com/view/ld23DV
float4 IBox(in float3 ro, in float3 rd, in float4x4 txx, in float4x4 txi, in float3 rad){

    // convert from ray to box space
	float3 rdd = mul(txx, float4(rd,0.0)).xyz;
	float3 roo = mul(txx, float4(ro,1.0)).xyz;

	// ray-box intersection in box space
    float3 m = 1.0/rdd;
    // more robust
    float3 k = float3(rdd.x>=0.0?rad.x:-rad.x, rdd.y>=0.0?rad.y:-rad.y, rdd.z>=0.0?rad.z:-rad.z);
    float3 t1 = (-roo - k)*m;
    float3 t2 = (-roo + k)*m;

    float tN = max(max(t1.x,t1.y),t1.z);
    float tF = min(min(t2.x,t2.y),t2.z);
    
    // no intersection
	if( tN>tF || tF<0.0 ) return (float4)-1.0;

    // use this instead if your rays origin can be inside the box
    float4 res = (tN>0.0) ? float4( tN, step((float3)tN,t1) ) :
                            float4( tF, step(t2,(float3)tF) );


    // add sign to normal and convert to ray space
	res.yzw = mul(txi, float4(-sign(rdd)*res.yzw,0.0)).xyz;

	return res;
}


// https://www.shadertoy.com/view/lsfGDB
// FAST = Ugly (but faster) way to intersect a planar coordinate system: plane + projection
// ELSE = Elegant way to intersect a planar coordinate system (3x3 linear system)
#define FAST

#ifdef FAST
float3 ICoordSys( in float3 ro, in float3 rd, in float3 dc, in float3 du, in float3 dv ){

    float3  oc = ro - dc;
    float3  no = cross(du,dv);
    float t  = -dot(no,oc)/dot(rd,no);
    float r  =  dot(du,oc + rd*t);
    float s  =  dot(dv,oc + rd*t);
    return float3(t,s,r);
}
#else
float3 ICoordSys( in float3 ro, in float3 rd, in float3 dc, in float3 du, in float3 dv ){

    float3 oc = ro - dc;
    return float3(
        dot( cross(du,dv), oc ),
        dot( cross(oc,du), rd ),
        dot( cross(dv,oc), rd ) ) / 
        dot( cross(dv,du), rd );
}
#endif


// Plane
// plane degined by p (p.xyz must be normalized)
float IPlane( in float3 ro, in float3 rd, in float4 p ){
    
    return -(dot(ro,p.xyz)+p.w)/dot(rd,p.xyz);
}
