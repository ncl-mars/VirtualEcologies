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

    // Texture2D _MainTex;
    // float4 _MainTex_TexelSize;

    #include "./FieldParticlesSimulation.hlsl"
ENDCG


Pass{

    Name "VelocityPass"
    CGPROGRAM

    #pragma multi_compile __ _ALTI_PATH _GRAV_TOPO _PLATEFORM

    float4 frag (v2f_img i) : SV_Target
    {
        uint3 ids;
        ids.xy = floor(i.uv * _Positions_TexelSize.zw + 0.5);
        ids.z = PtcToIdt(ids.xy, _Positions_TexelSize.zw);

        // if(ids.z > _Settings[0]) return 0;

        float4 vel;

        #ifdef _ALTI_PATH
            vel = PSAltiPath(ids);

        #elif _GRAV_TOPO
            vel = PSGravity(ids);
        
        #else
            vel = PSPlateform(ids);
        #endif

        return vel;
    }
    ENDCG
}

Pass{
    Name "PositionPass"
    CGPROGRAM

    fixed4 frag (v2f_img i) : SV_Target
    {
        uint2 id = floor(i.uv * _Positions_TexelSize.zw + 0.5);
        float4 pos = _Positions[id];
        
        ApplyVelocity(pos.xyz, _Velocities[id].xyz);

        return pos;
    }
    ENDCG
}
}
}
