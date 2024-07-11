#pragma multi_compile __ _1xF _2xF _3xF _4xF
#pragma multi_compile __ _1xE _2xE _3xE _4xE

#pragma multi_compile __ _FIELD_TEXARRAY

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

#ifdef _1xE
    #define NUM_EMITTERS 1
#elif _2xE
    #define NUM_EMITTERS 2
#elif _3xE
    #define NUM_EMITTERS 3
#elif _4xE
    #define NUM_EMITTERS 4
#else
    #define NUM_EMITTERS 1
#endif

//---------------------------------------------------------- Structs and Uniforms
// 2d path field
struct PathData{

    float3 nor;
    float dist;

    float3 flow;
    float depth;
};

struct PlateformData{

    float3 nor;
    
    float3 flow;

    float dist;
    float depth;
};

#include "../Includes/Noises.hlsl"
#include "../Includes/Utils.hlsl"

// Global textures
Texture2D _NoiseTex;

// Unpack global vector
float4 _GV;
#define _NoiseTexSize _GV.xy

#define FIELDS_INDEX 8

// Unpack vector buffer
float4 _UVB[FIELDS_INDEX + NUM_FIELDS];
#define _Settings   _UVB[0]    // numParticles + numEmitters + FieldTexSize.xy
#define _Times      _UVB[1]    // time, deltaTime, delayStart, lifeTime
#define _Origin     _UVB[2]    // xyz, w = sytem scale param
#define _Extents    _UVB[3]    // xyz, w = system scale object 
#define _Sim        _UVB[4]    // maxSpeed, maxForce
#define _Weights_A  _UVB[5]    // forces strengths
#define _Weights_B  _UVB[6]    // forces strengths
#define _Weights_C  _UVB[7]    // forces strengths
// TODO : Add player data

#define SIM_SCALE _Extents[3] * 0.01 * _Origin[3]

// Unpack Field Data
#define FieldData(id)  _UVB[FIELDS_INDEX + id] // x : linear 2D scale, y : depth scale, z = fieldTexId

// Unpack Matrices
float4x4 _UMB[NUM_FIELDS * 2 + NUM_EMITTERS];
#define WorldToField(id) _UMB[id * 2]
#define FieldToWorld(id) _UMB[id * 2 + 1]

#define FieldWorldPos(id) FieldToWorld(id)._m03_m13_m23

#define EmitterToWorld(id) _UMB[NUM_FIELDS * 2 + id]


// TODO : Atlas FallBack
#ifdef _FIELD_TEXARRAY 
    Texture2DArray _FieldTex;
    #define FetchField(uv, idField) _FieldTex[uint3( uv.xy * _Settings.zw, FieldData(idField)[2] )]
#else
    Texture2D _FieldTex;
    #define FetchField(uv, idField) _FieldTex[uv * _Settings.zw]
#endif

#define SIMULATION
#include "../Includes/Particles.hlsl"


#define DomainPos(pos) (pos - _Origin.xyz) / MinComp(_Extents.xyz);

//---------------------------------------------------------- Read Methods
PathData GetPathData(float2 uv, uint idField, float noise){
    
    PathData path;
    float4 field = FetchField(uv, idField);

    path.dist = length(field.rg) * 2 - 1;    
    float3 nor = float3(normalize(field.rg), 0);

    path.depth = length(field.ba) * 2 - 1;
    float3 flow = float3(normalize(field.ba), 0);

    float theta = noise * PI / 2;
    flow.xy = mul( Rot2(theta), flow.xy);
    
    // float side = sign(path.dist); // no more Side with new path ?
    // theta = (-side - noise) * PI/2;

    // float3 fnor = float3( mul( Rot2(theta), nor.xy ), 0.0);
    // flow = lerp( flow, fnor, clamp( max(path.dist, 0) * 0.0, 0.0,1.0) );

    // apply field matrix
    float4x4 matData = transpose(WorldToField(idField));

    path.nor  = normalize( mul(matData, float4(nor,  0)).xyz );
    path.flow = normalize( mul(matData, float4(flow, 0)).xyz );

    return path;
}
float4 GetTopoData(float2 uv, uint idField){

    float4 topo = FetchField(uv, idField);

    // multiply by invert transpose matrix
    topo.xyz = normalize( 
        mul( transpose(WorldToField(idField)), float4(topo.xyz, 0)).xyz
    );

    return topo;
}
PlateformData GetPlateformData(float2 uv, uint idField, int sgn){

    PlateformData data;
    float4 raw = FetchField(uv, idField);

    data.dist = length(raw.xyz) * 2 - 1;
    data.depth = raw.a;

    raw.xyz = normalize(raw.xyz);
    raw.z *= sgn;

    float3 flow = float3(normalize(raw.xy), 0);
    flow.xy = mul(Rot2(PI/2.0), flow.xy); 


    // multiply by invert transpose matrix
    data.nor = normalize( 
        mul( transpose(WorldToField(idField)), float4(raw.xyz, 0)).xyz
    );

    data.flow = normalize( 
        mul( transpose(WorldToField(idField)), float4(flow, 0)).xyz
    );


    return data;
}
//---------------------------------------------------------- Common Methods
float GetBoidNoise(uint id, float timeScl){

    float2 dimBuff = pow(_Settings[0], 0.5);
    float2 posBoids = PtcToNtc(IdtToPtc(id.x, dimBuff), dimBuff);
    
    float2 an = 0;
    an.x = cos(_Times.x * 0.7 * timeScl);
    an.y = sin(_Times.x * 0.5 * timeScl);
    
    float2 posNoise = posBoids * 0.5;
    posNoise += an * 0.01;

    float noiseBoid = _NoiseTex[posNoise * _NoiseTexSize.xy].r;
    return noiseBoid; 
}
void ResetPosition(inout float3 pos, uint id){

    float4x4 emittToWorld = EmitterToWorld(id % NUM_EMITTERS);

    float3 rand = Hash31(id * 10);
    pos = rand * 2 - 1; // Should be a texture !
    pos = mul(emittToWorld, float4(pos * 0.5, 1)).xyz;
}

bool CheckReset(uint id, inout ParticleData P){

    float3 bounds = max(abs(P.pos - _Origin.xyz) - _Extents.xyz, 0);

    // out bounds
    if( (Dot2(bounds) > 0.001) || P.life < 0.0001){

        #if  PLATEFORM
            P.vel = normalize(rand * 2 - 1) * _Sim[0];
        #else    
            P.vel = 0;
        #endif

        P.zone = -(NUM_FIELDS + 1);
        return true;
    }
    else return false;
}
float GetState(uint id){

    return 1.0 - step(_Times.x, (id / _Settings.x) * _Times.z); // time sequencing
}
float GetClosestField(float3 pos, inout int idClosest){
    idClosest = 0;
    float distance = 1e16;

    for(int f = 0; f < NUM_FIELDS; f++)
    {
        float3 dir = FieldWorldPos(f) - pos;
        float d = Dot2(dir);

        if(d < distance)
        {
            idClosest = f;
            distance = d;
        }
    }
    return distance; // squared !
}
float3 GetWorldBodyFallofAttraction(float3 vel, float3 pos, float3 bodyPos){

    float3 dir = bodyPos - pos;
    float str = Falloff(length(dir), MaxComp(_Extents)*2.0);
    return GetSteer(vel, normalize(dir)) * str;
}
float3 GetWorldBodyFallofAttraction(float3 vel, float3 pos, float3 bodyPos, float len){

    float3 dir = bodyPos - pos;
    float str = Falloff(len, MaxComp(_Extents)*2.0);
    return GetSteer(vel, normalize(dir)) * str;
}
float3 RandomizeFieldPosition(int idField, uint id){

    float3 target = FieldWorldPos(idField);
    target += 0.25 * (Hash31(id * 10 + _Times[0] * 0.001) * 2 - 1) * min(FieldData(idField).x, FieldData(idField).y);
    return target;
}

//------------------------------------------------------------ Gravity Collide
void CollideGravityFields(inout ParticleData P){

    int idClosest;
    float distance;

    #if NUM_FIELDS > 1
        distance = sqrt(GetClosestField(P.pos, idClosest));
    #else
        idClosest = 0;
        distance = length(P.pos-FieldWorldPos(0));
    #endif

    P.zone = idClosest + min( distance/(MaxComp(_Extents.xyz) * 2.0), 0.999);
    
    float3 fieldPos = mul(WorldToField(idClosest), float4(P.pos, 1)).xyz * 2.; // fieldPos = normalized field coord, -1 1

    bool clip  = Dot2(floor(abs(fieldPos) + 0.05)) < 0.5; // in field space
    if(clip)
    {
        float2 uv = fieldPos.xy * 0.5  + 0.5;
        float4 topo = GetTopoData(uv, idClosest);

        float thresh = 0.0125; // collisions
        float diff = fieldPos.z - (topo.a + thresh);
        
        if(diff < 0)
        {
            P.vel = reflect(P.vel, topo.xyz);
            P.vel *= 1.0 - (_Weights_A[1] * 0.5 + 0.5);
        }
    }
}
//---------------------------------------------------------- Path forces
float3 GetPathAcceleration(inout ParticleData P, float3 fieldPos, uint idField, uint id){

    // float noiseBoid = GetBoidNoise(id, 2) * 2 - 1; // Noise from boid's index
    float rand = IHash(id) * 2 - 1;

    float2 uv = abs(fieldPos.xy * 0.5 + 0.5);
    uv = abs(floor(uv % 2.0) - frac(uv)); // mirror uv

    PathData field = GetPathData(uv, idField, rand * _Weights_A[3]);
    
    //--------------------------------------------- Physics
    float3 flow = GetSteer(P.vel, field.flow);

    float3 tctr = GetSteer(P.vel, -field.nor);
    tctr *= max(field.dist, 0);

    float3 attr = normalize(mul(transpose(WorldToField(idField)), float4(0,0,-1,0)).xyz);
    float diff = fieldPos.z - (field.depth + rand * _Weights_A[1] * 0.5);
    attr = GetSteer(P.vel, attr) * diff;

    attr *= _Weights_A[0];
    flow *= _Weights_A[2];
    tctr *= _Weights_B[0];

    return flow + tctr + attr;
}
float3 GetPathFieldsAcceleration(inout ParticleData P, uint id){

    int curZoneID = 0;
    float distance;

    #if NUM_FIELDS > 1
        curZoneID = floor(P.zone);
    #endif

    float3 fieldPos = mul(WorldToField(curZoneID), float4(P.pos, 1)).xyz * 2.0;
    bool clip = Dot2(floor(abs(fieldPos))) < 0.5; // in field space
    float3 acc = 0;

    // if still inside zone, return path acc
    if(clip) 
    {
        distance = length(P.pos - FieldWorldPos(curZoneID));
        acc += GetPathAcceleration(P, fieldPos, curZoneID, id);
    }
    else
    {   
        #if NUM_FIELDS == 1
            distance = length(P.pos - FieldWorldPos(0));
            acc += GetWorldBodyFallofAttraction(P.vel, P.pos, RandomizeFieldPosition(0, id)) * _Sim[2];

        #else 
            int idClosest;
            distance = sqrt(GetClosestField(P.pos, idClosest));

            if(idClosest != curZoneID)
            {
                float3 fieldPos = mul(WorldToField(idClosest), float4(P.pos, 1)).xyz * 2.0;

                const float padding = 0.1;
                bool clip = Dot2(floor(abs(fieldPos) + padding)) < 0.5; // in field space

                if(clip){
                    curZoneID = idClosest; // update current zone if inside
                }
                else{ // go toward the field
                    acc += GetWorldBodyFallofAttraction(P.vel, P.pos, RandomizeFieldPosition(idClosest, id), distance) * _Sim[2]; 
                }
            }
        #endif
    }

    P.zone = curZoneID + min( distance/(MaxComp(_Extents.xyz) * 2.0), 0.999);
    return acc;
}

//---------------------------------------------------------- Plateforms forces
float3 GetPlateformAttractions(inout ParticleData P, uint id, out PlateformData data, out float3 fieldPos){

    int idClosest = 0;
    float distance;
    float3 acc = 0;

    bool outType = IHash(id) < (_Sim[2] * 0.5 + 0.5);
    int outSign = outType ? 1 : -1;

    #if NUM_FIELDS == 1 // field zero is attractor when out of its bounds
        distance = Dot2(P.pos-FieldWorldPos(0));
    #else 
        distance = GetClosestField(P.pos, idClosest);
    #endif

    distance = sqrt(distance);

    fieldPos = mul(WorldToField(idClosest), float4(P.pos, 1)).xyz * 2.0;
    data = GetPlateformData( (fieldPos.xy*0.5+0.5), idClosest, sign(fieldPos.z));

    float thresh = _Sim[3] * 0.5;
    float diff = abs(fieldPos.z) - (data.depth);
    bool inShape = (data.dist<thresh) && (diff<thresh);
    
    if( inShape ){ // set as current zone, apply path acc next frame

        P.zone = idClosest;
        thresh *= 0.5;
        bool collide = (diff>-thresh) || (data.dist>-thresh);

        if(collide){

            float3 ref = reflect(P.vel, -outSign * data.nor);
            float3 rec = refract(P.vel, -outSign * data.nor, 1);
            float s = saturate( sign( dot(P.vel, outSign * data.nor)) );

            float fac = max(1.0 - (_Weights_A[1] * 0.5 + 0.5), 0.01); // friction
            P.vel = lerp(ref * fac, rec, s);
        }
        else{

            acc += GetSteer(P.vel, data.flow) * _Weights_A[0] * 0.5; // flow
            float3 rand = Hash31(id * 10 + _Times[0] * 0.05 ) * 2 - 1;
            acc += GetSteer(P.vel, normalize(rand)) * _Weights_A[3] * 1.0; // rand
        }
    }

    else {
        P.zone = -abs(idClosest+1);

        if(outType)
        {
            float3 rand = Hash31(id * 10 + _Times[0] * 0.05 ) * 2 - 1;
            acc += GetSteer(P.vel, normalize(rand)) * _Weights_B[3]; // rand

            float3 fpos = P.pos - FieldWorldPos(idClosest);
            float3 cro = -normalize(cross(P.vel, fpos));
            float3 tan = normalize(cross(cro, fpos));

            acc += GetSteer(P.vel, tan) * _Weights_B[0]; 

            float3 target = FieldWorldPos(idClosest) + normalize(fpos) * FieldData(idClosest)[0] * _Weights_B[2];//* (rand.x * 0.25 + 0.75)
            acc += GetWorldBodyFallofAttraction(P.vel, P.pos, target, distance) * _Weights_B[1];
        }

        else{
            float3 target = RandomizeFieldPosition(idClosest, id);
            acc += GetWorldBodyFallofAttraction(P.vel, P.pos, target, distance) * _Weights_A[2];
        }

        // world stuff
        float3 windDir = normalize(_Weights_C.xyz);
        acc += GetSteer(P.vel, windDir) * _Weights_C.w;
    }

    P.zone += sign(P.zone + 0.0001) * min( distance/(MaxComp(_Extents.xyz) * 2.0), 0.999);

    return acc;
}

////////////////////////////////////////////////////////////////////////////////
float4 PSPlateform (uint3 ids){
 
    ParticleData P = UnpackBuffers(ids.xy);

    if(!CheckReset(ids.z, P))
    {
        float3 acc = 0;
        PlateformData data; float3 fieldPos;

        acc = GetPlateformAttractions(P, ids.z, data, fieldPos);

        ApplyAcceleration(P.vel, acc);
    }

    return PackVelocity(P);
}
////////////////////////////////////////////////////////////////////////////////
float4 PSAltiPath (uint3 ids){

    ParticleData P = UnpackBuffers(ids.xy);

    if(!CheckReset(ids.z, P))
    {

        float3 acc = GetPathFieldsAcceleration(P, ids.z);
        ApplyAcceleration(P.vel, acc);
    }

    return PackVelocity(P);
}
////////////////////////////////////////////////////////////////////////////////
float4 PSGravity (uint3 ids){
 
    ParticleData P = UnpackBuffers(ids.xy);

    if(!CheckReset(ids.z, P))
    {
    
        float3 grav = GetSteer(P.vel, float3(0,-9.8,0)) * _Weights_A[0];
        ApplyAcceleration(P.vel, grav);

        CollideGravityFields(P);
    }
    
    return PackVelocity(P);
}
////////////////////////////////////////////////////////////////////////////////
float4 PSPosition(uint3 ids){

    float4 pos = _Positions[ids.xy];
    float4 vel = _Velocities[ids.xy];

    float life = pos.w;

    if(vel.w <= -(NUM_FIELDS + 1))
    {
        ResetPosition(pos.xyz, ids.z);
        life = 1;
    }
    else
    {
        ApplyVelocity(pos.xyz, vel.xyz);
        life -= _Times[1] / _Times[3];
    }

    pos.w = life * GetState(ids.z);

    return pos;
}