Shader "Particles/Simulation"
{
Properties{ // _MainTex ("Texture", 2D) = "white" {}
}
SubShader
{
Cull Off ZWrite Off ZTest Always

CGINCLUDE
    #include "UnityCG.cginc"

    #pragma vertex vert_img
    #pragma fragment frag

    Texture2D _MainTex;
    float4 _MainTex_TexelSize;


    #include "./FieldParticlesSimulation.hlsl"
ENDCG


Pass{

    Name "VelocityPass"
    CGPROGRAM

    #pragma multi_compile __ _ALTI_PATH _GRAV_TOPO _PLATEFORM

    float4 frag (v2f_img i) : SV_Target
    {
        uint2 id = i.uv * _MainTex_TexelSize.zw;
        float4 vel;

        #ifdef _ALTI_PATH
            vel = PSAltiPath(id);

        #elif _GRAV_TOPO
            vel = PSGravity(id);
        
        #else
            vel = PSPlateform(id);
        #endif

        return vel;
    }
    ENDCG
}


Pass{
    Name "PositionPass"
    CGPROGRAM

    Texture2D _VelocityTex;

    fixed4 frag (v2f_img i) : SV_Target
    {
        uint2 id = i.uv * _MainTex_TexelSize.zw;
        float4 pos = _MainTex[id];
        
        ApplyVelocity(pos.xyz, _VelocityTex[id].xyz);

        return pos;
    }
    ENDCG
}
}
}
