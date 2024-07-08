Shader "Quads/PostPro"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            #define AA 4

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 sum = 0;

                for(int x = 0; x < AA; ++x)
                for(int y = 0; y < AA; ++y)
                {
                    float2 o = float2(float(x),float(y)) / float(AA) - 0.5;
                    float2 uv = i.uv + (o/_MainTex_TexelSize.w);
                    
                    sum +=  tex2D(_MainTex, uv);
                }

                sum /= float(AA*AA);



                // fixed4 col = tex2D(_MainTex, i.uv);
                // just invert the colors
                // col.rgb = 1 - col.rgb;
                return sum;
            }
            ENDCG
        }
    }
}
