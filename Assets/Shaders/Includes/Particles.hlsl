
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


    // SIMULATION
    #ifdef SIMULATION

        #ifndef EmitterData
            struct _DefaultEmitter
            {
                float4x4 txx;
            };
            #define EmitterData _DefaultEmitter
        #endif


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


// RENDERER

