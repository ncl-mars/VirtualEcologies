/*
Camera utilitary functions
*/

#ifndef CAM_UTILS_INCLUDED
#define CAM_UTILS_INCLUDED

void CreateCameraRay(in float2 uv, in float4x4 camToWorld, in float4x4 camInvProj, in float3 camWorldPos, 
                    out float3 ro, out float3 rd) 
{
    ro    = mul(camToWorld, float4(0,0,0,1)).xyz;
    ro    += camWorldPos.xyz; // add current cam position

    rd   = mul(camInvProj, float4(uv,0,1)).xyz;
    rd   = mul(camToWorld, float4(rd,0)).xyz;
    rd   = normalize(rd);
}


void CreateCameraRay(in float2 uv, in float4x4 camToWorld, in float4x4 camInvProj, 
                    out float3 ro, out float3 rd) 
{
    ro    = mul(camToWorld, float4(0,0,0,1)).xyz;

    rd   = mul(camInvProj, float4(uv,0,1)).xyz;
    rd   = mul(camToWorld, float4(rd,0)).xyz;
    rd   = normalize(rd);
}

//
// https://forum.unity.com/threads/having-trouble-converting-a-world-space-position-to-screen-space-in-a-shader.1343048/
// World position to read for distances, z = layer depth
float2 WorldToScreen(float3 worldPos, in float4x4 worldToCam, in float4x4 camProj){

    float4 camPos = mul(worldToCam, float4(worldPos, 1.0));
    // camPos.z = -camPos.z;

    float4 clipPos = mul(camProj, camPos);
    return(clipPos.xy / clipPos.w) ; // -1,1

    // float2 screenPos = (clipPos.xy / clipPos.w) ; // 0,1
    // return screenPos * 0.5 + 0.5;
}

float2 WorldToScreenUv(float3 worldPos, in float4x4 worldToCam, in float4x4 camProj){

    return WorldToScreen(worldPos, worldToCam, camProj) * 0.5 + 0.5;
}

#endif