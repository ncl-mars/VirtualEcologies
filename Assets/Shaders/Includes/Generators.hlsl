/*
    READINGS : https://en.wikipedia.org/wiki/Configuration_space_(physics)
*/
// dispatch / compute dimensions
#ifndef TARGET_DIM
    #define TARGET_DIM float3(512,512,1)
#endif

#ifndef Dot2
    #define Dot2(x) dot(x,x)
#endif

#ifndef PI
    #define PI 3.14159265359
#endif

// ================================================================== Buffer utils
//--------------------------------------------- Coordinates
// IDT stands for index target, PTC for pixel target coordinates
uint2 IdtToPtc(uint id){

	uint xQ = id / (uint)TARGET_DIM.x;
	uint x 	= id % (uint)TARGET_DIM.x;
	uint y 	= xQ % (uint)TARGET_DIM.y;
	return uint2(x, y);
}

// PTC for pixel target coordinate, convert 2d pixel to 1d index 
uint PtcToIdt(uint2 pixel){

	pixel = clamp(pixel, 0, uint2(TARGET_DIM.x - 1, TARGET_DIM.y - 1));
	return pixel.x + pixel.y * TARGET_DIM.x;
}

// NTC stands for normalized Target coordinates
float2 PtcToNtc(uint2 pixel){

	float2 dim = float2(TARGET_DIM.x, TARGET_DIM.y);
	return (2.0 * (pixel + 0.5) - float2(dim)) / min(dim.x, dim.y); // target ratio, centered
}

// NIC stands for normalized image coordinates
float2 PtcToNic(uint2 pixel, float2 imDim){

	float2 pos = PtcToNtc(pixel);
	pos /= imDim / max(imDim.x, imDim.y); // image ratio
    return pos;
}

// PIC stands for pixel Image Coordinates
uint2 PtcToPic(uint2 pixel, float2 imDim){

	float2 uv = PtcToNic(pixel, imDim) * 0.5 + 0.5;
	return clamp((uint2)(uv * imDim), 0, imDim - 1);
}

//--------------------------------------------- Buffer Fetch
// use a sampler state for regular texture, will be faster
float FetchBufferBilinear(in StructuredBuffer<float> buffer, in float2 uv, in float2 fres){

    int2 ires = (int2)fres;

    float2 st = (frac(uv)-0.5/fres)*fres;
    int2 i = int2( floor( st ) );
    float2  w = frac( st );

    float a = buffer[ PtcToIdt(i+int2(0,0)) ];
    float b = buffer[ PtcToIdt(i+int2(1,0)) ];
    float c = buffer[ PtcToIdt(i+int2(0,1)) ];
    float d = buffer[ PtcToIdt(i+int2(1,1)) ];

    return lerp(lerp(a, b, w.x), lerp(c, d, w.x), w.y);
}

float4 FetchBufferBilinear(in StructuredBuffer<float4> buffer, in float2 uv, in float2 fres){

    int2 ires = (int2)fres;

    float2 st = (frac(uv)-0.5/fres)*fres;
    int2 i = int2( floor( st ) );
    float2  w = frac( st );

    float4 a = buffer[ PtcToIdt(i+int2(0,0)) ];
    float4 b = buffer[ PtcToIdt(i+int2(1,0)) ];
    float4 c = buffer[ PtcToIdt(i+int2(0,1)) ];
    float4 d = buffer[ PtcToIdt(i+int2(1,1)) ];

    return lerp(lerp(a, b, w.x), lerp(c, d, w.x), w.y);
}

// texture fetch > decode > filter > reencode
float2 FetchDecode2TexBilinear(in Texture2D<float4> tex, in float2 uv, in float2 fres, bool isFirst){

    int2 ires = (int2)fres;

    float2 st = (frac(uv)-0.5/fres)*fres;
    int2 i = int2( floor( st ) );
    float2  w = frac( st );

    float2 va, vb, vc, vd;

    if(isFirst){
        va = tex[ i+int2(0,0) ].rg;
        vb = tex[ i+int2(1,0) ].rg;
        vc = tex[ i+int2(0,1) ].rg;
        vd = tex[ i+int2(1,1) ].rg;
    }

    else{
        va = tex[ i+int2(0,0) ].ba;
        vb = tex[ i+int2(1,0) ].ba;
        vc = tex[ i+int2(0,1) ].ba;
        vd = tex[ i+int2(1,1) ].ba;
    }

    float ma = length(va);
    float mb = length(vb);
    float mc = length(vc);
    float md = length(vd);

    va = normalize(va);
    vb = normalize(vb);
    vc = normalize(vc);
    vd = normalize(vd);

    float mag = lerp(lerp(ma, mb, w.x), lerp(mc, md, w.x), w.y);    // lerp distances or whatever float
    float2 vec = lerp(lerp(va, vb, w.x), lerp(vc, vd, w.x), w.y);   // lerp normals or whatever unit vector

    vec = normalize(vec);
    vec *= mag; 

    return vec;
}


// ================================================================== Sdf based Generators
// Thanks Inigo Quilez ! https://www.shadertoy.com/view/MXfXzM
float2 SMin( float a, float b, float k ){
	
    k *= 6.0;
    float h = max( k-abs(a-b), 0.0 )/k;
    float m = h*h*h*0.5;
    float s = m*k*(1.0/3.0); 
    return (a<b) ? float2(a-s,m) : float2(b-s,1.0-m);
}









//_________________________________________________________________________________
    // buffer fetch > decode > filter > reencode
    // float2 FetchDecodeHalfBufferBilinear(in StructuredBuffer<float4> buffer, in float2 uv, in float2 fres, bool isFirst){

    //     int2 ires = (int2)fres;

    //     float2 st = (frac(uv)-0.5/fres)*fres;
    //     int2 i = int2( floor( st ) );
    //     float2  w = frac( st );

    //     float2 va, vb, vc, vd;

    //     if(isFirst){
    //         va = buffer[ PtcToIdt(i+int2(0,0)) ].rg;
    //         vb = buffer[ PtcToIdt(i+int2(1,0)) ].rg;
    //         vc = buffer[ PtcToIdt(i+int2(0,1)) ].rg;
    //         vd = buffer[ PtcToIdt(i+int2(1,1)) ].rg;
    //     }

    //     else{
    //         va = buffer[ PtcToIdt(i+int2(0,0)) ].ba;
    //         vb = buffer[ PtcToIdt(i+int2(1,0)) ].ba;
    //         vc = buffer[ PtcToIdt(i+int2(0,1)) ].ba;
    //         vd = buffer[ PtcToIdt(i+int2(1,1)) ].ba;
    //     }

    //     float ma = length(va);
    //     float mb = length(vb);
    //     float mc = length(vc);
    //     float md = length(vd);

    //     va = normalize(va);
    //     vb = normalize(vb);
    //     vc = normalize(vc);
    //     vd = normalize(vd);

    //     float mag = lerp(lerp(ma, mb, w.x), lerp(mc, md, w.x), w.y);    // lerp distances or whatever float
    //     float2 vec = lerp(lerp(va, vb, w.x), lerp(vc, vd, w.x), w.y);   // lerp normals or whatever unit vector

    //     vec = normalize(vec);
    //     vec *= mag; 

    //     return vec;
    // }

