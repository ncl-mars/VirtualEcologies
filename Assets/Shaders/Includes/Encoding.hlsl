// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/CGIncludes/UnityCG.cginc
// g = DecodeFloatRGBA(gpack / (float)255.0) &&  fixed4 gpack = EncodeFloatRGBA(data.g);
// https://stackoverflow.com/questions/32915724/pack-two-floats-within-range-into-one-float


#ifndef ENCODING_INCLUDED
#define ENCODING_INCLUDED

float4 FloatToRGBA( float v ) {

    float4 enc = float4(1.0, 255.0, 65025.0, 16581375.0) * v;
    enc = frac(enc);
    enc -= enc.yzww * float4(
        0.0039215686274509803921568627451,
        0.0039215686274509803921568627451,
        0.0039215686274509803921568627451,
        0
        );
    return enc;
    // enc -= enc.yzww * float4(1.0/255.0,1.0/255.0,1.0/255.0,0.0); // avoid division
}

float RGBAToFloat( float4 rgba ) {

    return dot( rgba, float4(
        1.0, 
        0.0039215686274509803921568627451, 
        1.5378700499807766243752402921953e-5,
        6.0308629411010848014715305576287e-8
        ));
    // return dot( rgba, float4(1.0, 1/255.0, 1/65025.0, 1/16581375.0) );
}

#endif




// //---------------------------------------------------------- Heavy Precision Issues
// float RG16ToFloat(in float2 rg){

//     uint aScaled = rg.r * 0xFFFF;
//     uint bScaled = rg.g * 0xFFFF;
//     return asfloat((aScaled << 16) | (bScaled & 0xFFFF));
// }

// float2 FloatToRG16(in float r){

//     uint uintInput = asuint(r);
//     return float2( (uintInput >> 16) / 65535.0f, 
//                     (uintInput & 0xFFFF) / 65535.0f);
// }

// float PackFloats(float a, float b){

//     uint aScaled = a * 0xFFFF;
//     uint bScaled = b * 0xFFFF;
//     return asfloat((aScaled << 16) | (bScaled & 0xFFFF));
// }

// void UnpackFloat(float input, out float a, out float b){

//     uint uintInput = asuint(input);
//     a = (uintInput >> 16) / 65535.0f;            
//     b = (uintInput & 0xFFFF) / 65535.0f;
// }

// float4 Pack8to16(float4 rgba0, float4 rgba1){
//     float4 rgba;
//     rgba.r = PackFloats(rgba0.r, rgba1.r);
//     rgba.g = PackFloats(rgba0.g, rgba1.g);
//     rgba.b = PackFloats(rgba0.b, rgba1.b);
//     rgba.a = PackFloats(rgba0.a, rgba1.a);

//     return rgba;
// }

// void Unpack16To8(float4 rgba, out float4 rgba0, out float4 rgba1){

//     UnpackFloat(rgba.r, rgba0.r, rgba1.r);
//     UnpackFloat(rgba.g, rgba0.g, rgba1.g);
//     UnpackFloat(rgba.b, rgba0.b, rgba1.b);
//     UnpackFloat(rgba.a, rgba0.a, rgba1.a);
// }


    
    
    
    
    
    
    
    //__________________________________________________________________________________________
        // float PackFloats(float a, float b) {
            
        //     //Packing
        //     uint a16 = f32tof16(a);
        //     uint b16 = f32tof16(b);
        //     uint abPacked = (a16 << 16) | b16;
        //     return asfloat(abPacked);
        // }
    
        // void UnpackFloat(float input, out float a, out float b) {
    
        //     //Unpacking
        //     uint uintInput = asuint(input);
        //     a = f16tof32(uintInput >> 16);
        //     b = f16tof32(uintInput);
        // }