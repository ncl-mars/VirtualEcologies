#ifndef HASHES_INCLUDED
    #define HASHES_INCLUDED


    #ifndef TAU
        #define TAU 6.2831853
    #endif


    //------------------------------------------------------------- float hashes
    float3 Hash31(float p)
    {
    float3 p3 = frac((float3)p * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yzx+33.33);
    return frac((p3.xxy+p3.yzz)*p3.zyx); 
    }


    //------------------------------------------------------------- Randoms
    int IHash11(int a){
        a = (a ^ 61) ^ (a >> 16);
        a = a + (a << 3);
        a = a ^ (a >> 4);
        a = a * 0x27d4eb2d;
        a = a ^ (a >> 15);
        return a;
    }

    #define IHash(a) ( float(IHash11(a)) / float(0x7FFFFFFF) ) // Uniform in [0,1]

    float4 Rand4(int seed){
        return float4(IHash(seed^0x34F85A93),
                    IHash(seed^0x85FB93D5),
                    IHash(seed^0x6253DF84),
                    IHash(seed^0x25FC3625));
    }

    // --- normal law random generator
    float2 Randn(float2 r){ // r: randuniform
        r.x = sqrt( -2.* log(1e-9+abs(r.x)));
        r.y *= TAU;
        return r.x * float2(cos(r.y),sin(r.y));
    }



    //________________________________________________________ TEMP, make Texture !
    #define H(p)  frac(sin(fmod(dot(p, float2(12.9898, 78.233)),6.283)) * 43758.5453)

    #define Blue(p) (  \
            (  H(p+float2(-1,-1)) + H(p+float2(0,-1)) + H(p+float2(1,-1))  \
            + H(p+float2(-1, 0)) - 8.* H( p )      + H(p+float2(1, 0))  \
            + H(p+float2(-1, 1)) + H(p+float2(0, 1)) + H(p+float2(1, 1))  \
            ) *.5/9. *2.1 +.5 )

#endif
