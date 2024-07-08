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
    // "Queue"="Transparent" 
    // "RenderType"="Transparent"
}

Cull Off
// Cull Front
// Blend SrcAlpha OneMinusSrcAlpha

Pass
{
    AlphaToMask On // This could behave oddly without MSAA

    CGPROGRAM
    #pragma vertex vert
    #pragma fragment frag

    #pragma multi_compile __ _FIELD_TEXARRAY

    #include "UnityCG.cginc"

    struct v2f {

        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 view : TEXCOORD1;
        float3 nor : NORMAL;
        uint inst : SV_InstanceID;
        float4 col : COLOR0; // used for field data
    };

    struct FieldHelperData {

        float4x4 localToWorld; 
        float4x4 worldToLocal;

        float2 scales;
        int texId;
        int state;
    };

    StructuredBuffer<FieldHelperData> _FieldHelpers;
    uniform int _NumFields;

    float4 _Grid;
    float4 _FieldTexSize;

    int _SimulationMode;
    int _Wireframe;

    #ifdef _FIELD_TEXARRAY 
        Texture2DArray _FieldTex;
        #define FetchField(pixel, idField) _FieldTex[uint3(pixel.xy, _FieldHelpers[idField].texId)]
    #else
        Texture2D _FieldTex;
        #define FetchField(pixel, idField) _FieldTex[pixel]
    #endif

    #include "../Includes/Utils.hlsl"
    #include "../Includes/ColorFunctions.hlsl"
    #include "../Includes/SDFOperations.hlsl"

    // 2 triangles to generate a quad (n % 6)
    static int trisOffset [6] = {
        0,
        _Grid.x,
        _Grid.x + 1,
        0,
        _Grid.x + 1,
        1
    };

    #define TrisIdsToQuadIds(vertexID) (floor(vertexID/6) + trisOffset[vertexID%6])

    bool GetTrisPlanarCoords(uint id, uint2 grid, out float3 pos){

        uint posId = TrisIdsToQuadIds(id);
        uint2 pixel = IdtToPtc(posId, grid);

        if(_SimulationMode != 2){
            if(pixel.x<1) return false;
        }
        
        pos = float3(PtcToNtc(pixel, grid), 0);
        return true;
    }

    void PlateformVert(inout v2f o, in FieldHelperData helper) {

        float3 pos = o.pos;
        pos.xy = pos.xy * 0.5 + 0.5;
        pos.x *= 2.0;
        
        float2 uv = float2(saturate( floor(pos.x)), pos.y);
        float sgn = -(uv.x * 2 - 1); // 1 > -1
        
        uv.x = abs(uv.x * 2 - pos.x); // triangle gradient 0>1>0
        pos.xy = uv.xy * 2 - 1;

        uint2 pixel = uv * _FieldTexSize.xy;
        float4 field = FetchField(pixel, o.inst);
        float dist = length(field.rgb) * 2 - 1;

        float3 nor = normalize(field.rgb);
        nor.z *= sgn;

        float height = field.a * sgn;
        o.nor = normalize( mul( transpose(helper.worldToLocal), float4(nor, 0)).xyz) ;
        
        pos.z = height;
        o.pos.xyz = pos;

        fixed3 mate = height * 0.5 + 0.5;
        o.col.rgb = lerp(mate, o.nor, 1);
        o.col.a = saturate(ceil(abs(height)));

        o.col.a = (dist < 0.0125) ? 1 : 0;
    }

    void GravityVert(inout v2f o, in FieldHelperData helper) {
        
        uint2 pixel = o.uv * _FieldTexSize.xy;
        float4 field = FetchField(pixel, o.inst);

        o.nor = normalize( mul( transpose(helper.worldToLocal), float4(field.xyz, 0)).xyz);
        float height = field.a;

        fixed3 mate = height * 0.5 + 0.5;
        o.col.rgb = lerp(mate, o.nor, 1);
        o.col.a = 1;

        o.pos.z = height; 
    }

    void AltiVert(inout v2f o, in FieldHelperData helper) {
        
        uint2 pixel = o.uv * _FieldTexSize.xy;
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
        o.nor = normalize( mul( transpose(helper.worldToLocal), float4(o.nor, 0)).xyz);

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

    v2f vert(uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID){
        
        uint2 grid = (uint2)_Grid;

        v2f o; o.inst = instanceID; o.col = 0;

        FieldHelperData helper = _FieldHelpers[o.inst];
        if(helper.state == 0) return o;

        float3 pos;
        if(!GetTrisPlanarCoords(vertexID, grid, pos))return o;

        o.pos = float4(pos,0);
        o.uv = o.pos * 0.5 + 0.5;
        o.view = 0;
        
        if      (_SimulationMode == 0) AltiVert(o, helper);
        else if (_SimulationMode == 1) GravityVert(o, helper);
        else if (_SimulationMode == 2) PlateformVert(o, helper);

        float4 wpos = mul(helper.localToWorld, float4( o.pos.xyz * 0.5, 1.0));
        o.view = wpos - _WorldSpaceCameraPos;
        o.pos = mul(UNITY_MATRIX_VP, wpos);
        return o;
    }

    float4 frag(v2f i) : SV_Target {

        if(_FieldHelpers[i.inst].state == 0) return 0;
        
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

        if(_Wireframe)
        {
            float2 uv = i.uv * _Grid.xy * 0.5;
            float thick = 0.1;

            float2 guv = saturate( abs(frac(uv)-0.5)*2.0 - thick); // sawtooth [0,1] to tri [-1,1] => clip thick            
            guv = 1.0 - saturate( guv/fwidth(uv*2) + 0.5);

            col.rgba *= pow(max(guv.x, guv.y)*2.0, 0.5);
            col.rgb *= 0.75;
        }

        return col;
    }
    ENDCG
}
}
}



    // float PlateformHeight(uint2 pixel, uint instance, float sgn) {

    //     return parabola(saturate(-(length(FetchField(pixel, instance).rg) * 2 - 1) * 0.5), 0.25) * sgn;
    // }


// void PlateformVert(inout v2f o, in FieldHelperData helper) {

//     float3 pos = o.pos;
//     pos.xy = pos.xy * 0.5 + 0.5;
//     pos.x *= 2.0;
    
//     float2 uv = float2(saturate( floor(pos.x)), pos.y);
    
//     float sgn = -(uv.x * 2 - 1);
    
//     uv.x = abs(uv.x * 2 - pos.x); // triangle gradient 0>1>0
//     pos.xy = uv.xy * 2 - 1;

//     uint2 pixel = uv * _FieldTexSize.xy;
//     float4 field = FetchField(pixel, o.inst);
//     float dist = length(field.rg) * 2 - 1;
//     float disp = PlateformHeight(pixel, o.inst, sgn);

//     const int3 off = int3(-1,0,1);
//     const float eps = 0.005;
//     const float2 size = float2(eps,0.0);

//     float s01 = PlateformHeight(pixel + off.xy, o.inst, sgn);
//     float s21 = PlateformHeight(pixel + off.zy, o.inst, sgn);
//     float s10 = PlateformHeight(pixel + off.yx, o.inst, sgn);
//     float s12 = PlateformHeight(pixel + off.yz, o.inst, sgn);

//     float3 va = normalize(float3(size.xy,2*(s21-s01)));
//     float3 vb = normalize(float3(size.yx,2*(s12-s10)));

//     o.nor = cross(va,vb);
//     o.nor = normalize( mul( transpose(helper.worldToLocal), float4(o.nor, 0)).xyz);

//     pos.z += disp;
//     o.pos.xyz = pos;
    
//     o.col.rgb = o.nor;
//     o.col.a = 1;
//     o.col.a = (dist < 0.05) ? 1 : 0;

// }


// fixed3 mate = Palette( dist * cscl, fixed3(0.8,0.5,0.4),fixed3(0.2,0.4,0.2),fixed3(2.0,1.0,1.0),fixed3(0.0,0.25,0.25) );


    // v2f vert(uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID){

    //     const uint2 grid = (uint2)_Grid;

    //     v2f o; o.inst = instanceID; o.col = 0;

    //     FieldHelperData helper = _FieldHelpers[o.inst];
    //     if(helper.state == 0) return o;

    //     float3 pos;
    //     if(!GetTrisPlanarCoords(vertexID, grid, pos))return o;

        
    //     o.uv = pos.xy * 0.5 + 0.5;
    //     float4 field = FetchField(o.uv * _FieldTexSize.xy, o.inst);

    //     float height;

    //     if(_SimulationMode == 0) {   // alti-path

    //         // uint2 pixel = o.uv * _FieldTexSize.xy;

    //         // height = length(field.ba) * 2 - 1; 

    //         // const int3 off = int3(-1,0,1);
    //         // const float eps = 0.005;
    //         // const float2 size = float2(eps,0.0);

    //         // float s01 = length(FetchField(pixel + off.xy, o.inst).ba);
    //         // float s21 = length(FetchField(pixel + off.zy, o.inst).ba);
    //         // float s10 = length(FetchField(pixel + off.yx, o.inst).ba);
    //         // float s12 = length(FetchField(pixel + off.yz, o.inst).ba);

    //         // float3 va = normalize(float3(size.xy,2*(s21-s01)));
    //         // float3 vb = normalize(float3(size.yx,2*(s12-s10)));

    //         // o.nor = cross(va,vb);
    //         // o.nor = normalize( mul( transpose(helper.worldToLocal), float4(o.nor, 0)).xyz);

    //         // float2 dir2 = -normalize(field.ba); 
    //         // float dist = abs(length(field.rg) * 2 - 1); 
    //         // dist -= 0.05;

    //         // fixed3 mate = height * 0.5 + 0.5;
    //         // o.col.rgb = lerp(mate, fixed3(dir2, 0.0), 1-smoothstep(0.0,0.1,dist));

    //         // o.col.rgb = o.nor;
    //         // o.col.a = height;
    //         // pos.z = height;
    //     }

    //     else if (_SimulationMode == 1) {  // gravity

    //         o.nor = normalize( mul( transpose(helper.worldToLocal), float4(field.xyz, 0)).xyz);
    //         height = field.a;

    //         fixed3 mate = height * 0.5 + 0.5;
    //         o.col.rgb = lerp(mate, o.nor, 1);
    //         o.col.a = height;
    //         pos.z = height;
    //     }

    //     else if (_SimulationMode == 2) {  // plateform
        
    //         o.nor = normalize( mul( transpose(helper.worldToLocal), float4(normalize(field.rg), 0, 0)).xyz);
    //         float d = length(field.rg) * 2 - 1;
    //         float m = 1.0 - smoothstep(0.0, 0.1, abs(d));
    //         m = pow(m,4);

    //         float3 cola;
    //         float3 colb;
    //         cola = abs(float3(normalize(field.rg), 0));
    //         colb = o.nor;

    //         o.col.rgb = lerp(colb, cola, m);
    //     }
    //     else return o;

    //     float4 wpos = mul(helper.localToWorld, float4( pos.xyz * 0.5, 1.0));
    //     o.view = wpos - _WorldSpaceCameraPos;
    //     o.pos = mul(UNITY_MATRIX_VP, wpos);
    //     return o;
    // }