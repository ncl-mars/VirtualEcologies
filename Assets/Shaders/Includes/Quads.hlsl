float2 QuadCoords(in float2 uv, in float2 quadScl, in float2 texDim, in float4 texST){
        
    float2 pos = (uv - 0.5) * 2.0;
    pos *= quadScl.xy/min(quadScl.x,quadScl.y); // quad ratio
    
    pos = pos * texST.xy - texST.zw;
    pos /= texDim / max(texDim.x, texDim.y); // image ratio
    
    return pos;
}

float2 MirrorUV(in float2 uv, in bool3 mir)
{
    float2 qm = abs(uv);
    qm = abs(floor(qm % 2.0) - clamp(frac(qm), 0.001, 0.999));
    qm = lerp(uv, qm, mir.xy);
    return lerp(qm, clamp(uv, 0,1) , mir.z);
}


// float4 objectOrigin = unity_ObjectToWorld[3];
float4 BillboardObjectToClipPos(float3 vertex){

    // billboard mesh towards camera
    float3 wldpos = mul((float3x3)unity_ObjectToWorld, vertex.xyz);
    float4 wldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
    wldCoord.xyz += mul((float3x3)unity_CameraToWorld, wldpos);
    return mul(UNITY_MATRIX_VP, wldCoord);
}

float4 BillboardWorldToClipPos(float3 vertex){

    // billboard mesh towards camera
    float3 wldpos = mul((float3x3)unity_ObjectToWorld, vertex.xyz);
    float4 wldCoord = float4(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23, 1);
    wldCoord.xyz += mul((float3x3)unity_CameraToWorld, wldpos);

    return wldCoord;
    // return mul(UNITY_MATRIX_VP, wldCoord);
}