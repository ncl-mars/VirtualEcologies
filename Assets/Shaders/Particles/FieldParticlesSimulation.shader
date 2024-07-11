Shader "FieldParticles/Simulation"
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

    #include "./FieldParticlesSimulation.hlsl"
ENDCG

Pass{
    Name "VelocityPass"
    CGPROGRAM

    #pragma multi_compile __ _ALTI_PATH _GRAV_TOPO _PLATEFORM

    float4 frag (v2f_img i) : SV_Target
    {
        float4 vel;
        uint3 ids = GetIds(i.uv);

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
        return PSPosition(GetIds(i.uv));
    }
    ENDCG
}
}
}
