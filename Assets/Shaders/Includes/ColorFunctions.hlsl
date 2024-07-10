
#ifndef COLOR_FUNC_INCLUDED
#define COLOR_FUNC_INCLUDED

float3 ThermalGradient(in float fac){
    float3 c=sin(fac*1.5-float3(-.3,.2,.7));
    return c * c;
}

float4 BlendUnder(in float4 col, in float4 addCol){
    col.rgb += (1.0 - col.a) * addCol.a * addCol.rgb;
    col.a += (1.0 - col.a) * addCol.a;
    return col;
}

float ToBw(in float3 rgb){
    rgb *= float3(0.3,0.59,0.11); //float3(0.35,0.71,0.12);
    float luma = rgb.r + rgb.g + rgb.b;
    return luma;
}

float3 Palette( in float t, in float3 a, in float3 b, in float3 c, in float3 d ){
    return a + b*cos( 6.28318*(c*t+d) );
}

float4 Palette( in float t, in float4 a, in float4 b, in float4 c, in float4 d ){
    return a + b*cos( 6.28318*(c*t+d) );
}

// contrast
float3 SCurve(in float3 rgb){ return rgb*rgb*(3.0-2.0*rgb);}

// Vignetting factor
float Vignetting(in float2 p){ return 1.0 - 0.1*dot(p,p); }


// #define hue(v)  ( .6 + .6 * cos( 6.3*(v)  + float4(0,23,21,0)  ) )

float3 HueShift (in float3 rgb, in float sht)
{
    float3 P = (float3)0.55735*dot((float3)0.55735,rgb);
    float3 U = rgb-P;
    float3 V = cross((float3)0.55735,U);    

    rgb = U*cos(sht*6.2832) + V*sin(sht*6.2832) + P;
    
    return rgb;
}

// Official HSV to RGB conversion 
float3 Hsv2Rgb( in float3 c )
{
    float3 rgb = clamp( abs( ((c.x*6.0+float3(0.0,4.0,2.0)) % 6.0)-3.0)-1.0, 0.0, 1.0 );
	return c.z * lerp( (float3)1.0, rgb, c.y);
}


float3 Rgb2Hsb( in float3 c ){
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz),
                 float4(c.gb, K.xy),
                 step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r),
                 float4(c.r, p.yzx),
                 step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)),
                d / (q.x + e),
                q.x);
}

//  Function from Iñigo Quiles
//  https://www.shadertoy.com/view/MsS3Wc
float3 Hsb2Rgb( in float3 c ){
    float3 rgb = clamp(abs( ((c.x*6.0+float3(0.0,4.0,2.0))%
                             6.0)-3.0)-1.0,
                     0.0,
                     1.0 );
    rgb = rgb*rgb*(3.0-2.0*rgb);
    return c.z * lerp((float3)1.0, rgb, c.y);
}


//https://www.shadertoy.com/view/NstSWf
fixed3 Saturation(fixed3 rgb, float fac)
{
    float luma = dot(rgb, float3(0.2125, 0.7154, 0.0721));
    return lerp((float3)luma, rgb, (float3)fac * 5.);
}

#endif




// vibrance
// float average = (t1.r + t1.g + t1.b) / 3.;
// float mx = max(t1.r, max(t1.g, t1.b));
// float amount = (mx - average) * percentSlider * 3. * 5.;
// color = t1 - (mx - t1) * amount;


// http://freespace.virgin.net/hugo.elias/models/m_ffire.htm
//
// vec3 firePalette(float i){

//     float T = 1400. + 1300.*i; // Temperature range (in Kelvin).
//     vec3 L = vec3(7.4, 5.6, 4.4); // Red, green, blue wavelengths (in hundreds of nanometers).
//     L = pow(L,vec3(5)) * (exp(1.43876719683e5/(T*L)) - 1.);
//     return 1. - exp(-5e8/L); // Exposure level. Set to "50." For "70," change the "5" to a "7," etc.
// }