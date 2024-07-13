// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Compositing.shader

Shader "Image/Compositing"
{
Properties
{
    [NoScaleOffset] _MainTex ("_MainTex", 2D) = "clear" {}
    [NoScaleOffset] _SecondTex ("_SecondTex", 2D) = "clear" {}

    [PowerSlidder, 2] _AlphaParam ("_Alpha Param", Range (0.0, 1.0)) = 0.5
    [PowerSlidder, 2] _SecondAlphaParam ("_Second Alpha Param", Range (0.0, 1.0)) = 0.5
}

SubShader{
Pass{

Cull Off 
ZWrite On 
Blend Off
// Blend SrcAlpha OneMinusSrcAlpha
    
CGPROGRAM
    #pragma vertex vertexDirect
    #pragma fragment fragmentMix
    
    #include "UnityCG.cginc"
    #include "../Includes/ColorFunctions.hlsl"

    sampler2D _MainTex;
    sampler2D _SecondTex;

    float  _AlphaParam;
    float  _SecondAlphaParam;

    struct appdata_t {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
    };

    struct v2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    v2f vertexDirect(appdata_t v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.texcoord.xy;
        return o;
    }

    fixed4 fragmentMix(v2f i) : SV_Target
    {
        fixed4 colA = tex2D(_MainTex, i.uv);
        fixed4 colB = tex2D(_SecondTex, i.uv);

        return fixed4(_AlphaParam * colA.a * colA.rgb + _SecondAlphaParam * colB.a * colB.rgb, 1.0);
    }
    
ENDCG
}
}

FallBack Off
}