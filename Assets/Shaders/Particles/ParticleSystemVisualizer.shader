/*
    // https://docs.unity3d.com/ScriptReference/Graphics.RenderPrimitives.html
    // https://codepen.io/marco_fugaro/pen/xxZWPWJ
    // https://forum.unity.com/threads/antialiased-grid-lines-fwidth.1010668/
*/

Shader "Helpers/PlaneFieldSystemVisualizer"{
Properties {}
SubShader {
Tags { 
    "RenderQueue"="AlphaTest"
    "RenderType"="TransparentCutout" 
}

Cull Off
// Cull Back
Blend SrcAlpha OneMinusSrcAlpha


Pass
{
    // AlphaTest Greater 0.1
    CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag

    #pragma multi_compile __ _1xF _2xF _3xF _4xF
    #pragma multi_compile __ _FIELD_TEXARRAY

    #pragma target 3.5

    
    #ifdef _1xF
        #define NUM_FIELDS 1
    #elif _2xF
        #define NUM_FIELDS 2
    #elif _3xF
        #define NUM_FIELDS 3
    #elif _4xF
        #define NUM_FIELDS 4
    #else
        #define NUM_FIELDS 1
    #endif // NUM_FIELDS max = 4

    #include "UnityCG.cginc"

    
    struct v2f {

        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 view : TEXCOORD1;
        float3 nor : NORMAL;
        uint inst : SV_InstanceID;
        float4 col : COLOR0; // used for field data
    };

    #define FIELDS_INDEX 3

    // Unpack vector buffer
    float4 _UVB[FIELDS_INDEX + NUM_FIELDS];
    #define _Settings   _UVB[0] // _NumFields + FieldTexSize.xy
    #define _Grid       _UVB[1]
    #define _Modes      _UVB[2] // x sim mode, y: wireframe

    // Unpack Field Data
    #define FieldData(id)  _UVB[FIELDS_INDEX + id] // x : linear 2D scale, y : depth scale, z = fieldTexId

    // Unpack Matrices
    float4x4 _UMB[NUM_FIELDS * 2];
    #define WorldToField(id) _UMB[id * 2]
    #define FieldToWorld(id) _UMB[id * 2 + 1]


    #ifdef _FIELD_TEXARRAY 
        Texture2DArray _FieldTex;
        #define FetchField(pixel, idField) _FieldTex[uint3(pixel.xy, FieldData(idField)[2])]
    #else
        Texture2D _FieldTex;
        #define FetchField(pixel, idField) _FieldTex[pixel]
    #endif

    #include "../Includes/Utils.hlsl"
    #include "../Includes/ColorFunctions.hlsl"
    #include "../Includes/SDFOperations.hlsl"

    
 
    static int trisOffset [6] = {
        0,
        _Grid.x,
        1,
        1,
        _Grid.x,
        _Grid.x + 1,
    };
    
    // static int trisOffset [6] = {
    //     _Grid.x,
    //     _Grid.x + 1,
    //     0,
    //     0,
    //     _Grid.x + 1,
    //     1
    // };
    

    #define TrisIdsToQuadIds(vertexID) (floor(vertexID/6) + trisOffset[vertexID%6])

    bool GetTrisPlanarCoords(uint id, uint2 grid, out float3 pos){

        uint posId = TrisIdsToQuadIds(id);
        uint2 pixel = IdtToPtc(posId, grid);

        if(_Modes[0] != 2){
            if(pixel.x<1) return false;
        }
        
        pos = float3(PtcToNtc(pixel, grid), 0);
        return true;
    }

    void PlateformVert(inout v2f o) {

        float3 pos = o.pos;
        pos.xy = pos.xy * 0.5 + 0.5;
        pos.x *= 2.0;
        
        float2 uv = float2(saturate( floor(pos.x)), pos.y);
        float sgn = -(uv.x * 2 - 1); // 1 > -1
        
        uv.x = abs(uv.x * 2 - pos.x); // triangle gradient 0>1>0
        pos.xy = uv.xy * 2 - 1;

        uint2 pixel = uv * _Settings.yz;
        float4 field = FetchField(pixel, o.inst);
        float dist = length(field.rgb) * 2 - 1;

        float3 nor = normalize(field.rgb);
        nor.z *= sgn;

        float height = field.a * sgn;
        o.nor = normalize( mul( transpose(WorldToField(o.inst)), float4(nor, 0)).xyz) ;
        
        pos.z = height;
        o.pos.xyz = pos;

        fixed3 mate = height * 0.5 + 0.5;
        o.col.rgb = lerp(mate, o.nor, 1);
        o.col.a = saturate(ceil(abs(height)));

        o.col.a = (dist < 0.0125) ? 1 : 0;
    }

    void GravityVert(inout v2f o) {
        
        uint2 pixel = o.uv * _Settings.yz;
        float4 field = FetchField(pixel, o.inst);

        o.nor = normalize( mul( transpose(WorldToField(o.inst)), float4(field.xyz, 0)).xyz);
        float height = field.a;

        fixed3 mate = height * 0.5 + 0.5;
        o.col.rgb = lerp(mate, o.nor, 1);
        o.col.a = 1;

        o.pos.z = height; 
    }

    void AltiVert(inout v2f o) {
        
        uint2 pixel = o.uv * _Settings.yz;
        float4 field = FetchField(pixel, o.inst);

        float height = length(field.ba) * 2 - 1; 

        const int3 off = int3(-1,0,1);
        const float eps = 0.005;
        const float2 size = float2(eps,0.0);

        float s01 = length(FetchField(pixel + off.xy, o.inst).ba);
        float s21 = length(FetchField(pixel + off.zy, o.inst).ba);
        float s10 = length(FetchField(pixel + off.yx, o.inst).ba);
        float s12 = length(FetchField(pixel + off.yz, o.inst).ba);

        float3 va = normalize(float3(size.xy,2*(s21-s01)));
        float3 vb = normalize(float3(size.yx,2*(s12-s10)));

        o.nor = cross(va,vb);
        o.nor = normalize( mul( transpose(WorldToField(o.inst)), float4(o.nor, 0)).xyz);

        float2 dir2 = -normalize(field.ba); 
        float dist = abs(length(field.rg) * 2 - 1); 
        dist -= 0.05;

        fixed3 mate = height * 0.5 + 0.5;
        o.col.rgb = lerp(mate, fixed3(dir2, 0.0), 1-smoothstep(0.0,0.1,dist));
        o.col.a = 1.0;

        // o.col.rgb = o.nor;
        o.col.a = 1;
        o.pos.z = height;
    }

    v2f vert(uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID)
    {
        uint2 grid = (uint2)_Grid;

        v2f o; 
        o.inst = instanceID; 
        o.col = 0; o.nor = 0;

        float3 pos;
        if(!GetTrisPlanarCoords(vertexID, grid, pos))return o;

        o.pos = float4(pos,0);
        o.uv = o.pos * 0.5 + 0.5;
        o.view = 0;

        // o.pos *=2;
        
        if      (_Modes[0] == 0) AltiVert(o);
        else if (_Modes[0] == 1) GravityVert(o);
        else if (_Modes[0] == 2) PlateformVert(o);
        

        float4 wpos = mul(FieldToWorld(instanceID), float4( o.pos.xyz * 0.5, 1.0));
        o.view = wpos - _WorldSpaceCameraPos;
        o.pos = mul(UNITY_MATRIX_VP, wpos);


        return o;
    }

    float4 frag(v2f i) : SV_Target {


        // return 1;
        // // if(_FieldHelpers[i.inst].state == 0) return 0;
        
        float3 rd = normalize(i.view);
        float3 ref = reflect(rd, i.nor);
        fixed3 mate = i.col.rgb;

        float4 col = 0;
        float3 lin = 0;

        {   // Sun
            float3 lig = normalize( float3(0.5, 0.4, -.75) );
            float3 hal = normalize( lig-rd );
            float dif = clamp( dot( i.nor, lig ), 0.0, 1.0 );

            float spe = pow( clamp( dot( i.nor, hal ), 0.0, 1.0 ),16.0);
            spe *= dif;
            spe *= 0.04+0.96*pow(clamp(1.0-dot(hal,lig),0.0,1.0),5.0);

            lin += mate * dif;
        }
        {   // Sky
            float dif = sqrt(clamp( 0.5+0.5*i.nor.y, 0.0, 1.0 ));
            float spe = smoothstep( -0.2, 0.2, ref.y );
            spe *= dif;
            spe *= 0.04+0.96*pow(clamp(1.0+dot(i.nor,rd),0.0,1.0), 5.0 );

            lin += mate*0.60*dif*float3(0.40,0.60,1.15);
            lin +=      2.00*spe*float3(0.40,0.60,1.30); 
        }
        col.rgb = lin;
        col.a = i.col.a;

        // if(_Wireframe)
        // {
        //     float2 uv = i.uv * _Grid.xy * 0.5;
        //     float thick = 0.1;

        //     float2 guv = saturate( abs(frac(uv)-0.5)*2.0 - thick); // sawtooth [0,1] to tri [-1,1] => clip thick            
        //     guv = 1.0 - saturate( guv/fwidth(uv*2) + 0.5);

        //     col.rgba *= pow(max(guv.x, guv.y)*2.0, 0.5);
        //     col.rgb *= 0.75;
        // }

        clip(col.a - 0.1); // discard on alpha

        return col;
    }
    ENDCG
}
}
}


