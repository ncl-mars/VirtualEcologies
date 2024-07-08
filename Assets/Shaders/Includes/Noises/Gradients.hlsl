//
    //  https://github.com/stegu/webgl-noise/blob/master/src/classicnoise3D.glsl

    // Description : Array and textureless GLSL 2D/3D/4D simplex 
    //               noise functions.
    //      Author : Ian McEwan, Ashima Arts.
    //  Maintainer : stegu
    //     Lastmod : 20201014 (stegu)
    //     License : Copyright (C) 2011 Ashima Arts. All rights reserved.
    //               Distributed under the MIT License. See LICENSE file.
    //               https://github.com/ashima/webgl-noise
    //               https://github.com/stegu/webgl-noise
// 
// glsl style mod
#define mod(x, y) (x - y * floor(x / y))

float3 mod289(float3 x) {
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

float4 mod289(float4 x) {
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}

// Modulo 7 without a division
float3 mod7(float3 x) {
    return x - floor(x * (1.0 / 7.0)) * 7.0;
}

  float4 permute(float4 x) {
    return mod289(((x*34.0)+10.0)*x);
}

float3 permute(float3 x) {
    return mod289(((x*34.0)+10.0)*x);
}

float4 taylorInvSqrt(float4 r){
    return 1.79284291400159 - 0.85373472095314 * r;
}

float3 fade(float3 t) {
    return t*t*t*(t*(t*6.0-15.0)+10.0);
}

float2 fade(float2 t) {
    return t*t*t*(t*(t*6.0-15.0)+10.0);
}

//------------------------------------------------------------- Perlin
// Classic Perlin noise
float CNoise3(float3 P){

    float3 Pi0 = floor(P); // Integer part for indexing
    float3 Pi1 = Pi0 + (float3)1.0; // Integer part + 1
    Pi0 = mod289(Pi0);
    Pi1 = mod289(Pi1);
    float3 Pf0 = frac(P); // Fractional part for interpolation
    float3 Pf1 = Pf0 - (float3)1.0; // Fractional part - 1.0
    float4 ix = float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
    float4 iy = float4(Pi0.yy, Pi1.yy);
    float4 iz0 = Pi0.zzzz;
    float4 iz1 = Pi1.zzzz;

    float4 ixy = permute(permute(ix) + iy);
    float4 ixy0 = permute(ixy + iz0);
    float4 ixy1 = permute(ixy + iz1);

    float4 gx0 = ixy0 * (1.0 / 7.0);
    float4 gy0 = frac(floor(gx0) * (1.0 / 7.0)) - 0.5;
    gx0 = frac(gx0);
    float4 gz0 = (float4)0.5 - abs(gx0) - abs(gy0);
    float4 sz0 = step(gz0, (float4)0.0);
    gx0 -= sz0 * (step(0.0, gx0) - 0.5);
    gy0 -= sz0 * (step(0.0, gy0) - 0.5);

    float4 gx1 = ixy1 * (1.0 / 7.0);
    float4 gy1 = frac(floor(gx1) * (1.0 / 7.0)) - 0.5;
    gx1 = frac(gx1);
    float4 gz1 = (float4)0.5 - abs(gx1) - abs(gy1);
    float4 sz1 = step(gz1, (float4)0.0);
    gx1 -= sz1 * (step(0.0, gx1) - 0.5);
    gy1 -= sz1 * (step(0.0, gy1) - 0.5);

    float3 g000 = float3(gx0.x,gy0.x,gz0.x);
    float3 g100 = float3(gx0.y,gy0.y,gz0.y);
    float3 g010 = float3(gx0.z,gy0.z,gz0.z);
    float3 g110 = float3(gx0.w,gy0.w,gz0.w);
    float3 g001 = float3(gx1.x,gy1.x,gz1.x);
    float3 g101 = float3(gx1.y,gy1.y,gz1.y);
    float3 g011 = float3(gx1.z,gy1.z,gz1.z);
    float3 g111 = float3(gx1.w,gy1.w,gz1.w);

    float4 norm0 = taylorInvSqrt(float4(dot(g000, g000), dot(g010, g010), dot(g100, g100), dot(g110, g110)));
    g000 *= norm0.x;
    g010 *= norm0.y;
    g100 *= norm0.z;
    g110 *= norm0.w;
    float4 norm1 = taylorInvSqrt(float4(dot(g001, g001), dot(g011, g011), dot(g101, g101), dot(g111, g111)));
    g001 *= norm1.x;
    g011 *= norm1.y;
    g101 *= norm1.z;
    g111 *= norm1.w;

    float n000 = dot(g000, Pf0);
    float n100 = dot(g100, float3(Pf1.x, Pf0.yz));
    float n010 = dot(g010, float3(Pf0.x, Pf1.y, Pf0.z));
    float n110 = dot(g110, float3(Pf1.xy, Pf0.z));
    float n001 = dot(g001, float3(Pf0.xy, Pf1.z));
    float n101 = dot(g101, float3(Pf1.x, Pf0.y, Pf1.z));
    float n011 = dot(g011, float3(Pf0.x, Pf1.yz));
    float n111 = dot(g111, Pf1);

    float3 fade_xyz = fade(Pf0);
    float4 n_z = lerp(float4(n000, n100, n010, n110), float4(n001, n101, n011, n111), fade_xyz.z);
    float2 n_yz = lerp(n_z.xy, n_z.zw, fade_xyz.y);
    float n_xyz = lerp(n_yz.x, n_yz.y, fade_xyz.x); 
    return 2.2 * n_xyz;
}

// Classic Perlin noise, periodic variant
float PCNoise3(float3 P, float3 rep){

    float3 Pi0 = mod(floor(P), rep); // Integer part, modulo period
    float3 Pi1 = mod( (Pi0 + (float3)1.0) , rep); // Integer part + 1, mod period
    Pi0 = mod289(Pi0);
    Pi1 = mod289(Pi1);
    float3 Pf0 = frac(P); // Fractional part for interpolation
    float3 Pf1 = Pf0 - (float3)1.0; // Fractional part - 1.0
    float4 ix = float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
    float4 iy = float4(Pi0.yy, Pi1.yy);
    float4 iz0 = Pi0.zzzz;
    float4 iz1 = Pi1.zzzz;

    float4 ixy = permute(permute(ix) + iy);
    float4 ixy0 = permute(ixy + iz0);
    float4 ixy1 = permute(ixy + iz1);

    float4 gx0 = ixy0 * (1.0 / 7.0);
    float4 gy0 = frac(floor(gx0) * (1.0 / 7.0)) - 0.5;
    gx0 = frac(gx0);
    float4 gz0 = (float4)0.5 - abs(gx0) - abs(gy0);
    float4 sz0 = step(gz0, 0);
    gx0 -= sz0 * (step(0.0, gx0) - 0.5);
    gy0 -= sz0 * (step(0.0, gy0) - 0.5);

    float4 gx1 = ixy1 * (1.0 / 7.0);
    float4 gy1 = frac(floor(gx1) * (1.0 / 7.0)) - 0.5;
    gx1 = frac(gx1);
    float4 gz1 = (float4)0.5 - abs(gx1) - abs(gy1);
    float4 sz1 = step(gz1, 0);
    gx1 -= sz1 * (step(0.0, gx1) - 0.5);
    gy1 -= sz1 * (step(0.0, gy1) - 0.5);

    float3 g000 = float3(gx0.x,gy0.x,gz0.x);
    float3 g100 = float3(gx0.y,gy0.y,gz0.y);
    float3 g010 = float3(gx0.z,gy0.z,gz0.z);
    float3 g110 = float3(gx0.w,gy0.w,gz0.w);
    float3 g001 = float3(gx1.x,gy1.x,gz1.x);
    float3 g101 = float3(gx1.y,gy1.y,gz1.y);
    float3 g011 = float3(gx1.z,gy1.z,gz1.z);
    float3 g111 = float3(gx1.w,gy1.w,gz1.w);

    float4 norm0 = taylorInvSqrt(float4(dot(g000, g000), dot(g010, g010), dot(g100, g100), dot(g110, g110)));
    g000 *= norm0.x;
    g010 *= norm0.y;
    g100 *= norm0.z;
    g110 *= norm0.w;
    float4 norm1 = taylorInvSqrt(float4(dot(g001, g001), dot(g011, g011), dot(g101, g101), dot(g111, g111)));
    g001 *= norm1.x;
    g011 *= norm1.y;
    g101 *= norm1.z;
    g111 *= norm1.w;

    float n000 = dot(g000, Pf0);
    float n100 = dot(g100, float3(Pf1.x, Pf0.yz));
    float n010 = dot(g010, float3(Pf0.x, Pf1.y, Pf0.z));
    float n110 = dot(g110, float3(Pf1.xy, Pf0.z));
    float n001 = dot(g001, float3(Pf0.xy, Pf1.z));
    float n101 = dot(g101, float3(Pf1.x, Pf0.y, Pf1.z));
    float n011 = dot(g011, float3(Pf0.x, Pf1.yz));
    float n111 = dot(g111, Pf1);

    float3 fade_xyz = fade(Pf0);
    float4 n_z = lerp(float4(n000, n100, n010, n110), float4(n001, n101, n011, n111), fade_xyz.z);
    float2 n_yz = lerp(n_z.xy, n_z.zw, fade_xyz.y);
    float n_xyz = lerp(n_yz.x, n_yz.y, fade_xyz.x); 
    return 2.2 * n_xyz;
}

// Classic Perlin noise
float CNoise2(float2 P){

    float4 Pi = floor(P.xyxy) + float4(0.0, 0.0, 1.0, 1.0);
    float4 Pf = frac(P.xyxy) - float4(0.0, 0.0, 1.0, 1.0);
    Pi = mod289(Pi); // To avoid truncation effects in permutation
    float4 ix = Pi.xzxz;
    float4 iy = Pi.yyww;
    float4 fx = Pf.xzxz;
    float4 fy = Pf.yyww;

    float4 i = permute(permute(ix) + iy);

    float4 gx = frac(i * (1.0 / 41.0)) * 2.0 - 1.0 ;
    float4 gy = abs(gx) - 0.5 ;
    float4 tx = floor(gx + 0.5);
    gx = gx - tx;

    float2 g00 = float2(gx.x,gy.x);
    float2 g10 = float2(gx.y,gy.y);
    float2 g01 = float2(gx.z,gy.z);
    float2 g11 = float2(gx.w,gy.w);

    float4 norm = taylorInvSqrt(float4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
    g00 *= norm.x;  
    g01 *= norm.y;  
    g10 *= norm.z;  
    g11 *= norm.w;  

    float n00 = dot(g00, float2(fx.x, fy.x));
    float n10 = dot(g10, float2(fx.y, fy.y));
    float n01 = dot(g01, float2(fx.z, fy.z));
    float n11 = dot(g11, float2(fx.w, fy.w));

    float2 fade_xy = fade(Pf.xy);
    float2 n_x = lerp(float2(n00, n01), float2(n10, n11), fade_xy.x);
    float n_xy = lerp(n_x.x, n_x.y, fade_xy.y);
    return 2.3 * n_xy;
}

// Classic Perlin noise, periodic variant
float PCNoise2(float2 P, float2 rep){

    float4 Pi = floor(P.xyxy) + float4(0.0, 0.0, 1.0, 1.0);
    float4 Pf = frac(P.xyxy) - float4(0.0, 0.0, 1.0, 1.0);
    Pi = mod(Pi, rep.xyxy); // To create noise with explicit period
    Pi = mod289(Pi);        // To avoid truncation effects in permutation
    float4 ix = Pi.xzxz;
    float4 iy = Pi.yyww;
    float4 fx = Pf.xzxz;
    float4 fy = Pf.yyww;

    float4 i = permute(permute(ix) + iy);

    float4 gx = frac(i * (1.0 / 41.0)) * 2.0 - 1.0 ;
    float4 gy = abs(gx) - 0.5 ;
    float4 tx = floor(gx + 0.5);
    gx = gx - tx;

    float2 g00 = float2(gx.x,gy.x);
    float2 g10 = float2(gx.y,gy.y);
    float2 g01 = float2(gx.z,gy.z);
    float2 g11 = float2(gx.w,gy.w);

    float4 norm = taylorInvSqrt(float4(dot(g00, g00), dot(g01, g01), dot(g10, g10), dot(g11, g11)));
    g00 *= norm.x;  
    g01 *= norm.y;  
    g10 *= norm.z;  
    g11 *= norm.w;  

    float n00 = dot(g00, float2(fx.x, fy.x));
    float n10 = dot(g10, float2(fx.y, fy.y));
    float n01 = dot(g01, float2(fx.z, fy.z));
    float n11 = dot(g11, float2(fx.w, fy.w));

    float2 fade_xy = fade(Pf.xy);
    float2 n_x = lerp(float2(n00, n01), float2(n10, n11), fade_xy.x);
    float n_xy = lerp(n_x.x, n_x.y, fade_xy.y);
    return 2.3 * n_xy;
}

//------------------------------------------------------------- Simplex
// Simplex
float SNoise3(float3 v){

    const float2  C = float2(1.0/6.0, 1.0/3.0) ;
    const float4  D = float4(0.0, 0.5, 1.0, 2.0);

    // First corner
    float3 i  = floor(v + dot(v, C.yyy) );
    float3 x0 =   v - i + dot(i, C.xxx) ;

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min( g.xyz, l.zxy );
    float3 i2 = max( g.xyz, l.zxy );


    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
    float3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

    // Permutations
    i = mod289(i); 
    float4 p = permute( permute( permute( 
            i.z + float4(0.0, i1.z, i2.z, 1.0 ))
            + i.y + float4(0.0, i1.y, i2.y, 1.0 )) 
            + i.x + float4(0.0, i1.x, i2.x, 1.0 ));


    float n_ = 0.142857142857; // 1.0/7.0
    float3  ns = n_ * D.wyz - D.xzx;

    float4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

    float4 x = x_ *ns.x + ns.yyyy;
    float4 y = y_ *ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4( x.xy, y.xy );
    float4 b1 = float4( x.zw, y.zw );

    //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
    //float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
    float4 s0 = floor(b0)*2.0 + 1.0;
    float4 s1 = floor(b1)*2.0 + 1.0;
    float4 sh = -step(h, (float4)0.0);

    float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
    float4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

    float3 p0 = float3(a0.xy,h.x);
    float3 p1 = float3(a0.zw,h.y);
    float3 p2 = float3(a1.xy,h.z);
    float3 p3 = float3(a1.zw,h.w);

    //Normalise gradients
    float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    // Mix final noise value
    float4 m = max(0.5 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
    m = m * m;
    return 105.0 * dot( m*m, float4( dot(p0,x0), dot(p1,x1), 
                                dot(p2,x2), dot(p3,x3) ) );
}

// Simplex + Gradient
float SNoiseGrad3(float3 v, out float3 gradient){

    const float2  C = float2(1.0/6.0, 1.0/3.0) ;
    const float4  D = float4(0.0, 0.5, 1.0, 2.0);

    // First corner
    float3 i  = floor(v + dot(v, C.yyy) );
    float3 x0 =   v - i + dot(i, C.xxx) ;

    // Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min( g.xyz, l.zxy );
    float3 i2 = max( g.xyz, l.zxy );

    //   x0 = x0 - 0.0 + 0.0 * C.xxx;
    //   x1 = x0 - i1  + 1.0 * C.xxx;
    //   x2 = x0 - i2  + 2.0 * C.xxx;
    //   x3 = x0 - 1.0 + 3.0 * C.xxx;
    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
    float3 x3 = x0 - D.yyy;      // -1.0+3.0*C.x = -0.5 = -D.y

    // Permutations
    i = mod289(i); 
    float4 p = permute( permute( permute( 
            i.z + float4(0.0, i1.z, i2.z, 1.0 ))
            + i.y + float4(0.0, i1.y, i2.y, 1.0 )) 
            + i.x + float4(0.0, i1.x, i2.x, 1.0 ));

    // Gradients: 7x7 points over a square, mapped onto an octahedron.
    // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float n_ = 0.142857142857; // 1.0/7.0
    float3  ns = n_ * D.wyz - D.xzx;

    float4 j = p - 49.0 * floor(p * ns.z * ns.z);  //  mod(p,7*7)

    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_ );    // mod(j,N)

    float4 x = x_ *ns.x + ns.yyyy;
    float4 y = y_ *ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4( x.xy, y.xy );
    float4 b1 = float4( x.zw, y.zw );

    //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
    //float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
    float4 s0 = floor(b0)*2.0 + 1.0;
    float4 s1 = floor(b1)*2.0 + 1.0;
    float4 sh = -step(h, (float4)0.0);

    float4 a0 = b0.xzyw + s0.xzyw*sh.xxyy ;
    float4 a1 = b1.xzyw + s1.xzyw*sh.zzww ;

    float3 p0 = float3(a0.xy,h.x);
    float3 p1 = float3(a0.zw,h.y);
    float3 p2 = float3(a1.xy,h.z);
    float3 p3 = float3(a1.zw,h.w);

    //Normalise gradients
    float4 norm = taylorInvSqrt(float4(dot(p0,p0), dot(p1,p1), dot(p2, p2), dot(p3,p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    // Mix final noise value
    float4 m = max(0.5 - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3)), 0.0);
    float4 m2 = m * m;
    float4 m4 = m2 * m2;
    float4 pdotx = float4(dot(p0,x0), dot(p1,x1), dot(p2,x2), dot(p3,x3));

    // Determine noise gradient
    float4 temp = m2 * m * pdotx;
    gradient = -8.0 * (temp.x * x0 + temp.y * x1 + temp.z * x2 + temp.w * x3);
    gradient += m4.x * p0 + m4.y * p1 + m4.z * p2 + m4.w * p3;
    gradient *= 105.0;

    return 105.0 * dot(m4, pdotx);
}

//------------------------------------------------------------- Worley
// Cellular noise, returning F1 and F2 in a float2.
// 3x3x3 search region for good F2 everywhere, but a lot
// slower than the 2x2x2 version.
// The code below is a bit scary even to its author,
// but it has at least half decent performance on a
// modern GPU. In any case, it beats any software
// implementation of Worley noise hands down.
float2 Cellular(float3 P) {

    const float K = 0.142857142857; // 1/7
    const float Ko = 0.428571428571; // 1/2-K/2
    const float K2 = 0.020408163265306; // 1/(7*7)
    const float Kz = 0.166666666667; // 1/6
    const float Kzo = 0.416666666667; // 1/2-1/6*2
    const float jitter = 1.0; // smaller jitter gives more regular pattern

    float3 Pi = mod289(floor(P));
    float3 Pf = frac(P) - 0.5;

    float3 Pfx = Pf.x + float3(1.0, 0.0, -1.0);
    float3 Pfy = Pf.y + float3(1.0, 0.0, -1.0);
    float3 Pfz = Pf.z + float3(1.0, 0.0, -1.0);

    float3 p = permute(Pi.x + float3(-1.0, 0.0, 1.0));
    float3 p1 = permute(p + Pi.y - 1.0);
    float3 p2 = permute(p + Pi.y);
    float3 p3 = permute(p + Pi.y + 1.0);

    float3 p11 = permute(p1 + Pi.z - 1.0);
    float3 p12 = permute(p1 + Pi.z);
    float3 p13 = permute(p1 + Pi.z + 1.0);

    float3 p21 = permute(p2 + Pi.z - 1.0);
    float3 p22 = permute(p2 + Pi.z);
    float3 p23 = permute(p2 + Pi.z + 1.0);

    float3 p31 = permute(p3 + Pi.z - 1.0);
    float3 p32 = permute(p3 + Pi.z);
    float3 p33 = permute(p3 + Pi.z + 1.0);

    float3 ox11 = frac(p11*K) - Ko;
    float3 oy11 = mod7(floor(p11*K))*K - Ko;
    float3 oz11 = floor(p11*K2)*Kz - Kzo; // p11 < 289 guaranteed

    float3 ox12 = frac(p12*K) - Ko;
    float3 oy12 = mod7(floor(p12*K))*K - Ko;
    float3 oz12 = floor(p12*K2)*Kz - Kzo;

    float3 ox13 = frac(p13*K) - Ko;
    float3 oy13 = mod7(floor(p13*K))*K - Ko;
    float3 oz13 = floor(p13*K2)*Kz - Kzo;

    float3 ox21 = frac(p21*K) - Ko;
    float3 oy21 = mod7(floor(p21*K))*K - Ko;
    float3 oz21 = floor(p21*K2)*Kz - Kzo;

    float3 ox22 = frac(p22*K) - Ko;
    float3 oy22 = mod7(floor(p22*K))*K - Ko;
    float3 oz22 = floor(p22*K2)*Kz - Kzo;

    float3 ox23 = frac(p23*K) - Ko;
    float3 oy23 = mod7(floor(p23*K))*K - Ko;
    float3 oz23 = floor(p23*K2)*Kz - Kzo;

    float3 ox31 = frac(p31*K) - Ko;
    float3 oy31 = mod7(floor(p31*K))*K - Ko;
    float3 oz31 = floor(p31*K2)*Kz - Kzo;

    float3 ox32 = frac(p32*K) - Ko;
    float3 oy32 = mod7(floor(p32*K))*K - Ko;
    float3 oz32 = floor(p32*K2)*Kz - Kzo;

    float3 ox33 = frac(p33*K) - Ko;
    float3 oy33 = mod7(floor(p33*K))*K - Ko;
    float3 oz33 = floor(p33*K2)*Kz - Kzo;

    float3 dx11 = Pfx + jitter*ox11;
    float3 dy11 = Pfy.x + jitter*oy11;
    float3 dz11 = Pfz.x + jitter*oz11;

    float3 dx12 = Pfx + jitter*ox12;
    float3 dy12 = Pfy.x + jitter*oy12;
    float3 dz12 = Pfz.y + jitter*oz12;

    float3 dx13 = Pfx + jitter*ox13;
    float3 dy13 = Pfy.x + jitter*oy13;
    float3 dz13 = Pfz.z + jitter*oz13;

    float3 dx21 = Pfx + jitter*ox21;
    float3 dy21 = Pfy.y + jitter*oy21;
    float3 dz21 = Pfz.x + jitter*oz21;

    float3 dx22 = Pfx + jitter*ox22;
    float3 dy22 = Pfy.y + jitter*oy22;
    float3 dz22 = Pfz.y + jitter*oz22;

    float3 dx23 = Pfx + jitter*ox23;
    float3 dy23 = Pfy.y + jitter*oy23;
    float3 dz23 = Pfz.z + jitter*oz23;

    float3 dx31 = Pfx + jitter*ox31;
    float3 dy31 = Pfy.z + jitter*oy31;
    float3 dz31 = Pfz.x + jitter*oz31;

    float3 dx32 = Pfx + jitter*ox32;
    float3 dy32 = Pfy.z + jitter*oy32;
    float3 dz32 = Pfz.y + jitter*oz32;

    float3 dx33 = Pfx + jitter*ox33;
    float3 dy33 = Pfy.z + jitter*oy33;
    float3 dz33 = Pfz.z + jitter*oz33;

    float3 d11 = dx11 * dx11 + dy11 * dy11 + dz11 * dz11;
    float3 d12 = dx12 * dx12 + dy12 * dy12 + dz12 * dz12;
    float3 d13 = dx13 * dx13 + dy13 * dy13 + dz13 * dz13;
    float3 d21 = dx21 * dx21 + dy21 * dy21 + dz21 * dz21;
    float3 d22 = dx22 * dx22 + dy22 * dy22 + dz22 * dz22;
    float3 d23 = dx23 * dx23 + dy23 * dy23 + dz23 * dz23;
    float3 d31 = dx31 * dx31 + dy31 * dy31 + dz31 * dz31;
    float3 d32 = dx32 * dx32 + dy32 * dy32 + dz32 * dz32;
    float3 d33 = dx33 * dx33 + dy33 * dy33 + dz33 * dz33;

        // Sort out the two smallest distances (F1, F2)
    #if 0
        // Cheat and sort out only F1
        float3 d1 = min(min(d11,d12), d13);
        float3 d2 = min(min(d21,d22), d23);
        float3 d3 = min(min(d31,d32), d33);
        float3 d = min(min(d1,d2), d3);
        d.x = min(min(d.x,d.y),d.z);
        return float2(sqrt(d.x)); // F1 duplicated, no F2 computed
    #else
        // Do it right and sort out both F1 and F2
        float3 d1a = min(d11, d12);
        d12 = max(d11, d12);
        d11 = min(d1a, d13); // Smallest now not in d12 or d13
        d13 = max(d1a, d13);
        d12 = min(d12, d13); // 2nd smallest now not in d13
        float3 d2a = min(d21, d22);
        d22 = max(d21, d22);
        d21 = min(d2a, d23); // Smallest now not in d22 or d23
        d23 = max(d2a, d23);
        d22 = min(d22, d23); // 2nd smallest now not in d23
        float3 d3a = min(d31, d32);
        d32 = max(d31, d32);
        d31 = min(d3a, d33); // Smallest now not in d32 or d33
        d33 = max(d3a, d33);
        d32 = min(d32, d33); // 2nd smallest now not in d33
        float3 da = min(d11, d21);
        d21 = max(d11, d21);
        d11 = min(da, d31); // Smallest now in d11
        d31 = max(da, d31); // 2nd smallest now not in d31
        d11.xy = (d11.x < d11.y) ? d11.xy : d11.yx;
        d11.xz = (d11.x < d11.z) ? d11.xz : d11.zx; // d11.x now smallest
        d12 = min(d12, d21); // 2nd smallest now not in d21
        d12 = min(d12, d22); // nor in d22
        d12 = min(d12, d31); // nor in d31
        d12 = min(d12, d32); // nor in d32
        d11.yz = min(d11.yz,d12.xy); // nor in d12.yz
        d11.y = min(d11.y,d12.z); // Only two more to go
        d11.y = min(d11.y,d11.z); // Done! (Phew!)
        return sqrt(d11.xy); // F1, F2
    #endif
}


//------------------------------------------------------------- 
// replace these by something better (production ready)
float3 Hash33( float3 p ){

	p = float3( dot(p,float3(127.1,311.7, 74.7)),
			  dot(p,float3(269.5,183.3,246.1)),
			  dot(p,float3(113.5,271.9,124.6)));

	return -1.0 + 2.0*frac(sin(p)*43758.5453123);
}

float2 Hash22( float2 p ){

	p = float2( dot(p,float2(127.1,311.7)), dot(p,float2(269.5,183.3)) );
	return -1.0 + 2.0*frac(sin(p)*43758.5453123);
}

float Hash12( float2 p ){

    p  = 50.0*frac( p*0.3183099 );
    return frac( p.x*p.y*(p.x+p.y) );
}

float3 IHash33( uint3 x ){

    const uint k = 1103515245U;  // GLIB C

    x = ((x>>8U)^x.yzx)*k;
    x = ((x>>8U)^x.yzx)*k;
    x = ((x>>8U)^x.yzx)*k;
    
    return (float3)x*(1.0/float(0xffffffffU));
}


//------------------------------------------------------------- noises
// https://www.shadertoy.com/view/4ttSWf
float Noise( in float3 x ){

    float3 p = floor(x);
    float3 w = frac(x);
    
    float3 u = w*w*w*(w*(w*6.0-15.0)+10.0);

    float n = p.x + 317.0*p.y + 157.0*p.z;
    
    float a = Hash12(n+0.0);
    float b = Hash12(n+1.0);
    float c = Hash12(n+317.0);
    float d = Hash12(n+318.0);
    float e = Hash12(n+157.0);
	float f = Hash12(n+158.0);
    float g = Hash12(n+474.0);
    float h = Hash12(n+475.0);

    float k0 =   a;
    float k1 =   b - a;
    float k2 =   c - a;
    float k3 =   e - a;
    float k4 =   a - b - c + d;
    float k5 =   a - c - e + g;
    float k6 =   a - b - e + f;
    float k7 = - a + b + c - d + e - f - g + h;

    return -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z);
}

#ifndef INTERPOLANT
    #define INTERPOLANT 0
#endif

float GradNoise3D( in float3 p ){

    float3 i = floor( p );
    float3 f = frac( p );

    #if INTERPOLANT==1
    // quintic interpolant
    float3 u = f*f*f*(f*(f*6.0-15.0)+10.0);
    #else
    // cubic interpolant
    float3 u = f*f*(3.0-2.0*f);
    #endif    

    return lerp( lerp(  lerp(   dot( Hash33( i + float3(0.0,0.0,0.0) ), f - float3(0.0,0.0,0.0) ), 
                                dot( Hash33( i + float3(1.0,0.0,0.0) ), f - float3(1.0,0.0,0.0) ), u.x),
                        lerp(   dot( Hash33( i + float3(0.0,1.0,0.0) ), f - float3(0.0,1.0,0.0) ), 
                                dot( Hash33( i + float3(1.0,1.0,0.0) ), f - float3(1.0,1.0,0.0) ), u.x), u.y),
                lerp( lerp(     dot( Hash33( i + float3(0.0,0.0,1.0) ), f - float3(0.0,0.0,1.0) ), 
                                dot( Hash33( i + float3(1.0,0.0,1.0) ), f - float3(1.0,0.0,1.0) ), u.x),
                     lerp(      dot( Hash33( i + float3(0.0,1.0,1.0) ), f - float3(0.0,1.0,1.0) ), 
                                dot( Hash33( i + float3(1.0,1.0,1.0) ), f - float3(1.0,1.0,1.0) ), u.x), u.y), u.z );
}

float SimplexNoise( in float2 p ){

    const float K1 = 0.366025404; // (sqrt(3)-1)/2
    const float K2 = 0.211324865; // (3-sqrt(3))/6

	float2  i = floor( p + (p.x+p.y)*K1 );
    float2  a = p - i + (i.x+i.y)*K2;
    float m = step(a.y,a.x); 
    float2  o = float2(m,1.0-m);
    float2  b = a - o + K2;
	float2  c = a - 1.0 + 2.0*K2;
    float3  h = max( 0.5-float3(dot(a,a), dot(b,b), dot(c,c) ), 0.0 );
	float3  n = h*h*h*h*float3( dot(a,Hash22(i+0.0)), dot(b,Hash22(i+o)), dot(c,Hash22(i+1.0)));
    return dot( n, (float3)70.0 );
}


//----------------------------------------------------------------------- FBM
// float Hash11( float n ) { return frac(sin(n)*753.5453123); }
int IHash11(int a){
	a = (a ^ 61) ^ (a >> 16);
	a = a + (a << 3);
	a = a ^ (a >> 4);
	a = a * 0x27d4eb2d;
	a = a ^ (a >> 15);
	return a;
}

#define Hash11(a) ( float(IHash11(a)) / float(0x7FFFFFFF) ) // Uniform in [0,1]

//---------------------------------------------------------------
// value noise, and its analytical derivatives
//---------------------------------------------------------------
float4 NoiseD3( in float3 x )
{
    float3 p = floor(x);
    float3 w = frac(x);
	float3 u = w*w*(3.0-2.0*w);
    float3 du = 6.0*w*(1.0-w);
    
    float n = p.x + p.y*157.0 + 113.0*p.z;
    
    float a = Hash11(n+  0.0);
    float b = Hash11(n+  1.0);
    float c = Hash11(n+157.0);
    float d = Hash11(n+158.0);
    float e = Hash11(n+113.0);
	float f = Hash11(n+114.0);
    float g = Hash11(n+270.0);
    float h = Hash11(n+271.0);

    float k0 =   a;
    float k1 =   b - a;
    float k2 =   c - a;
    float k3 =   e - a;
    float k4 =   a - b - c + d;
    float k5 =   a - c - e + g;
    float k6 =   a - b - e + f;
    float k7 = - a + b + c - d + e - f - g + h;

    return float4( k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z, 
                 du * (float3(k1,k2,k3) + u.yzx*float3(k4,k5,k6) + u.zxy*float3(k6,k4,k5) + k7*u.yzx*u.zxy ));
}

#ifndef M3
#define M3  float3x3(0.00, 0.80, 0.60, -0.80,0.36,-0.48, -0.60,-0.48,0.64)
#define M3i float3x3(0.00,-0.80,-0.60,  0.80,0.36,-0.48,  0.60,-0.48,0.64)
#endif


// returns 3D fbm and its 3 derivatives
float4 FbmD( in float3 x, int octaves )
{
    float3x3 m = float3x3(
        1.0,0.0,0.0,
        0.0,1.0,0.0,
        0.0,0.0,1.0
    );

    float f = 1.98;  // could be 2.0
    float s = 0.49;  // could be 0.5
    
    float a = 0.0;  // noise
    float3 d = 0.0; // derivative

    float b = 0.5;  // decreasing scale
                            
    for( int i=0; i < octaves; i++ )
    {
        float4 n = NoiseD3(x);
        a += b*n.x;                // accumulate values
        d += mul(m, b*n.yzw);      // accumulate derivatives
        b *= s;
        x = mul(M3, f*x);
        m = mul(M3i,f*m);
    }
    return float4(a, d);
}









// const float3x3 m3  = float3x3( 
//     0.00,  0.80,  0.60,
//     -0.80,  0.36, -0.48,
//     -0.60, -0.48,  0.64
// );
// const float3x3 m3i = float3x3( 
//     0.00, -0.80, -0.60,
//     0.80,  0.36, -0.48,
//     0.60, -0.48,  0.64 
// );



// // value noise, and its analytical derivatives
// float4 noised( in float3 x )
// {
//     float3 p = floor(x);
//     float3 w = fract(x);
//     #if 1
//     float3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
//     float3 du = 30.0*w*w*(w*(w-2.0)+1.0);
//     #else
//     float3 u = w*w*(3.0-2.0*w);
//     float3 du = 6.0*w*(1.0-w);
//     #endif

//     float n = p.x + 317.0*p.y + 157.0*p.z;
    
//     float a = hash1(n+0.0);
//     float b = hash1(n+1.0);
//     float c = hash1(n+317.0);
//     float d = hash1(n+318.0);
//     float e = hash1(n+157.0);
// 	float f = hash1(n+158.0);
//     float g = hash1(n+474.0);
//     float h = hash1(n+475.0);

//     float k0 =   a;
//     float k1 =   b - a;
//     float k2 =   c - a;
//     float k3 =   e - a;
//     float k4 =   a - b - c + d;
//     float k5 =   a - c - e + g;
//     float k6 =   a - b - e + f;
//     float k7 = - a + b + c - d + e - f - g + h;

//     return float4( -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z), 
//                       2.0* du * float3( k1 + k4*u.y + k6*u.z + k7*u.y*u.z,
//                                       k2 + k5*u.z + k4*u.x + k7*u.z*u.x,
//                                       k3 + k6*u.x + k5*u.y + k7*u.x*u.y ) );
// }






// void CSSimplexFractalRGB()
// {
//     float f = 0;

//     uv *= 5.0;
//     mat2 m = mat2( 1.6,  1.2, -1.2,  1.6 );
//     f  = 0.5000*noise( uv ); uv = m*uv;
//     f += 0.2500*noise( uv ); uv = m*uv;
//     f += 0.1250*noise( uv ); uv = m*uv;
//     f += 0.0625*noise( uv ); uv = m*uv;
// }