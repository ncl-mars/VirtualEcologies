
#ifndef PARTICLES_INCLUDED
    #define PARTICLES_INCLUDED
    
    //======================================================= Structs
    #ifndef ParticleData
        struct _DefaultParticle
        {
            float3 vel;
            float zone;

            float3 pos;
            float life;
        };
        #define ParticleData _DefaultParticle
    #endif

    // Buffers
    Texture2D<float4> _Positions;
    Texture2D<float4> _Velocities;
    
    float4 _Positions_TexelSize;

    ParticleData UnpackBuffers(uint2 id)
    {
        ParticleData P;
        float4 pos = _Positions[id]; 
        float4 vel = _Velocities[id]; 
        
        P.pos = pos.xyz;
        P.vel = vel.xyz;

        P.zone = vel.w;
        P.life = pos.w;

        return P;
    }

    float4 PackVelocity(in ParticleData P)
    {
        return float4(P.vel.xyz, P.zone);
    }

    // SIMULATION
    #ifdef SIMULATION
        uint3 GetIds(float2 uv)
        {
            uint3 ids;
            ids.xy = floor(uv * _Positions_TexelSize.zw);
            ids.z = PtcToIdt(ids.xy, _Positions_TexelSize.zw);
    
            clip(ids.z < (uint) _Settings[0]);
            return ids;
        }

        void ApplyAcceleration(inout float3 vel, float3 acc){

            vel += acc;
            vel = ClampMag(vel, 0, _Sim[0]);
        }
        void ApplyVelocity(inout float3 pos, in float3 vel){
            
            vel *= SIM_SCALE;
            vel *= _Times[1] * 50;
            pos += vel;
        }

        float3 GetSteer(float3 vel, float3 force){

            force *= _Sim[0]; // maxSpeed
            force -= vel;
            force = ClampMag(force, 0, _Sim[1]); // maxForce
            return force;
        }

    #endif

#endif

