/*
    Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
*/

Shader "Image/FrameBuffer"{
Properties{
    _MainTex ("_MainTex (A)", 2D) = "clear"
}
SubShader{
// Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
ZWrite Off
ZTest Off

Pass{

CGPROGRAM
    #pragma vertex vert_img
    #pragma fragment frag

    #include "UnityCG.cginc"
    #include "../Includes/ColorFunctions.hlsl"

    sampler2D _MainTex;

    
    fixed4 frag(v2f_img i) : SV_Target
    {
        fixed4 col = tex2D(_MainTex, i.uv) * 0.95;
        return col;
    }

ENDCG
}
}
}