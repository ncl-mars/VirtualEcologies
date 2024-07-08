//___________________________________________________________________________________________________________
// https://gist.github.com/Fewes/59d2c831672040452aa77da6eaab2234
float4 Cubic(float v){

	float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
	float4 s = n * n * n;
	float x = s.x;
	float y = s.y - 4.0 * s.x;
	float z = s.z - 4.0 * s.y + 6.0 * s.x;
	float w = 6.0 - x - y - z;
	return float4(x, y, z, w) * (1.0 / 6.0);
}

float4 Tex3DTricubic(sampler3D tex, float3 texCoords, float3 textureSize){

	float4 texelSize = float4(1.0 / textureSize.xz, textureSize.xz);

	texCoords = texCoords * textureSize - 0.5;

	float3 f = frac(texCoords);
	texCoords -= f;

	float4 xcubic = Cubic(f.x);
	float4 ycubic = Cubic(f.y);
	float4 zcubic = Cubic(f.z);

	float2 cx = texCoords.xx + float2(-0.5, 1.5);
	float2 cy = texCoords.yy + float2(-0.5, 1.5);
	float2 cz = texCoords.zz + float2(-0.5, 1.5);
	float2 sx = xcubic.xz + xcubic.yw;
	float2 sy = ycubic.xz + ycubic.yw;
	float2 sz = zcubic.xz + zcubic.yw;
	float2 offsetx = cx + xcubic.yw / sx;
	float2 offsety = cy + ycubic.yw / sy;
	float2 offsetz = cz + zcubic.yw / sz;
	offsetx /= textureSize.xx;
	offsety /= textureSize.yy;
	offsetz /= textureSize.zz;

	float4 sample0 = tex3Dlod(tex, float4(offsetx.x, offsety.x, offsetz.x, 0));
	float4 sample1 = tex3Dlod(tex, float4(offsetx.y, offsety.x, offsetz.x, 0));
	float4 sample2 = tex3Dlod(tex, float4(offsetx.x, offsety.y, offsetz.x, 0));
	float4 sample3 = tex3Dlod(tex, float4(offsetx.y, offsety.y, offsetz.x, 0));
	float4 sample4 = tex3Dlod(tex, float4(offsetx.x, offsety.x, offsetz.y, 0));
	float4 sample5 = tex3Dlod(tex, float4(offsetx.y, offsety.x, offsetz.y, 0));
	float4 sample6 = tex3Dlod(tex, float4(offsetx.x, offsety.y, offsetz.y, 0));
	float4 sample7 = tex3Dlod(tex, float4(offsetx.y, offsety.y, offsetz.y, 0));

	float gx = sx.x / (sx.x + sx.y);
	float gy = sy.x / (sy.x + sy.y);
	float gz = sz.x / (sz.x + sz.y);

	float4 x0 = lerp(sample1, sample0, gx);
	float4 x1 = lerp(sample3, sample2, gx);
	float4 x2 = lerp(sample5, sample4, gx);
	float4 x3 = lerp(sample7, sample6, gx);
	float4 y0 = lerp(x1, x0, gy);
	float4 y1 = lerp(x3, x2, gy);

	return lerp(y1, y0, gz);
}

// https://github.com/DannyRuijters/CubicInterpolationCUDA/blob/master/examples/glCubicRayCast/tricubic.shader
// https://www.shadertoy.com/view/cts3Rj
float4 Tex3DTricubicFast4(sampler3D tex, float3 coord, float3 sze){

	// shift the coordinate from [0,1] to [-0.5, nrOfVoxels-0.5]
	float3 nrOfVoxels = sze;
	float3 coord_grid = coord * nrOfVoxels - 0.5;

	float3 index = floor(coord_grid);
	float3 fraction = coord_grid - index;
	float3 one_frac = 1.0 - fraction;
	float3 w0 = 1.0/6.0 * one_frac*one_frac*one_frac;
	float3 w1 = 2.0/3.0 - 0.5 * fraction*fraction*(2.0-fraction);
	float3 w2 = 2.0/3.0 - 0.5 * one_frac*one_frac*(2.0-one_frac);
	float3 w3 = 1.0/6.0 * fraction*fraction*fraction;
	float3 g0 = w0 + w1;
	float3 g1 = w2 + w3;
	float3 mult = 1.0 / nrOfVoxels;
	float3 h0 = mult * ((w1 / g0) - 0.5 + index);  //h0 = w1/g0 - 1, move from [-0.5, nrOfVoxels-0.5] to [0,1]\n"
	float3 h1 = mult * ((w3 / g1) + 1.5 + index);  //h1 = w3/g1 + 1, move from [-0.5, nrOfVoxels-0.5] to [0,1]\n"

	float4 tex000 = tex3Dlod(tex, float4(h0, 0));
	float4 tex100 = tex3Dlod(tex, float4(h1.x, h0.y, h0.z, 0));
	tex000 = lerp(tex100, tex000, g0.x);  //weigh along the x-direction\n"

	float4 tex010 = tex3Dlod(tex, float4(h0.x, h1.y, h0.z, 0));
	float4 tex110 = tex3Dlod(tex, float4(h1.x, h1.y, h0.z, 0));
	tex010 = lerp(tex110, tex010, g0.x);  //weigh along the x-direction\n"
	tex000 = lerp(tex010, tex000, g0.y);  //weigh along the y-direction\n"
	
    float4 tex001 = tex3Dlod(tex, float4(h0.x, h0.y, h1.z, 0));
	float4 tex101 = tex3Dlod(tex, float4(h1.x, h0.y, h1.z, 0));
	tex001 = lerp(tex101, tex001, g0.x);  //weigh along the x-direction\n"
	
    float4 tex011 = tex3Dlod(tex, float4(h0.x, h1.y, h1.z, 0));
	float4 tex111 = tex3Dlod(tex, float4(h1, 0));
	tex011 = lerp(tex111, tex011, g0.x);  //weigh along the x-direction\n"
	tex001 = lerp(tex011, tex001, g0.y);  //weigh along the y-direction\n"

	return lerp(tex001, tex000, g0.z);  //weigh along the z-direction\n"
}

float Tex3DTricubicFast1(sampler3D tex, float3 coord, float3 sze){

	// shift the coordinate from [0,1] to [-0.5, nrOfVoxels-0.5]
	float3 nrOfVoxels = sze;
	float3 coord_grid = coord * nrOfVoxels - 0.5;

	float3 index = floor(coord_grid);
	float3 fraction = coord_grid - index;
	float3 one_frac = 1.0 - fraction;
	float3 w0 = 1.0/6.0 * one_frac*one_frac*one_frac;
	float3 w1 = 2.0/3.0 - 0.5 * fraction*fraction*(2.0-fraction);
	float3 w2 = 2.0/3.0 - 0.5 * one_frac*one_frac*(2.0-one_frac);
	float3 w3 = 1.0/6.0 * fraction*fraction*fraction;
	float3 g0 = w0 + w1;
	float3 g1 = w2 + w3;
	float3 mult = 1.0 / nrOfVoxels;
	float3 h0 = mult * ((w1 / g0) - 0.5 + index);  //h0 = w1/g0 - 1, move from [-0.5, nrOfVoxels-0.5] to [0,1]\n"
	float3 h1 = mult * ((w3 / g1) + 1.5 + index);  //h1 = w3/g1 + 1, move from [-0.5, nrOfVoxels-0.5] to [0,1]\n"

	float tex000 = tex3D(tex, h0).r;
	float tex100 = tex3D(tex, float3(h1.x, h0.y, h0.z)).r;
	tex000 = lerp(tex100, tex000, g0.x);  //weigh along the x-direction\n"

	float tex010 = tex3D(tex, float3(h0.x, h1.y, h0.z)).r;
	float tex110 = tex3D(tex, float3(h1.x, h1.y, h0.z)).r;
	tex010 = lerp(tex110, tex010, g0.x);  //weigh along the x-direction\n"
	tex000 = lerp(tex010, tex000, g0.y);  //weigh along the y-direction\n"
	
    float tex001 = tex3D(tex, float3(h0.x, h0.y, h1.z)).r;
	float tex101 = tex3D(tex, float3(h1.x, h0.y, h1.z)).r;
	tex001 = lerp(tex101, tex001, g0.x);  //weigh along the x-direction\n"
	
    float tex011 = tex3D(tex, float3(h0.x, h1.y, h1.z)).r;
	float tex111 = tex3D(tex, h1).r;
	tex011 = lerp(tex111, tex011, g0.x);  //weigh along the x-direction\n"
	tex001 = lerp(tex011, tex001, g0.y);  //weigh along the y-direction\n"

	return lerp(tex001, tex000, g0.z);  //weigh along the z-direction\n"
}

// https://www.shadertoy.com/view/MtjBWz tricubic interpolation ( 1 single fetch: thanks iq :-) )
float4 TriCubicTex(sampler3D sam, float3 u, float3 sze) {
    
    float3 R = sze;
    float3 U = u*R + .5;
    float3 F = frac(U);

    // U = floor(U) + F*F*(3.-2.*F);
    U = floor(U) + F*F*F*(F*(F*6.-15.)+10.);   // use if you want smooth gradients
    return tex3D( sam, (U-.5) / R );
}

//___________________________________________________________________________________________________________
// improved bilinear interpolated tex2D fetch, NO MIP MAP
// https://iquilezles.org/articles/hwinterpolation/
float4 FetchBilinear(in Texture2D tex, in float2 uv, in float2 res ){

    int2 ires = (int2)res.xy;
    float2  fres = res.xy;

    float2 st = (frac(uv)-0.5/fres)*fres;
    int2 i = int2( floor( st ) );
    float2  w = frac( st );

    float4 a = tex[ i+int2(0,0) ];
    float4 b = tex[ i+int2(1,0) ];
    float4 c = tex[ i+int2(0,1) ];
    float4 d = tex[ i+int2(1,1) ];

    return lerp(lerp(a, b, w.x), lerp(c, d, w.x), w.y);
}

// improved bilinear interpolated tex2D fetch, NO MIP MAP
float4 FetchClampBilinear(in Texture2D tex, in float2 uv, in float2 res ){

    int2 ires = (int2)res.xy;
    float2  fres = res.xy;

    float2 st = (frac(uv)-0.5/fres)*fres;
    int2 i = int2( floor( st ) );
    float2  w = frac( st );

    float4 a = tex[ clamp(i+int2(0,0), 0, ires-1) ];
    float4 b = tex[ clamp(i+int2(1,0), 0, ires-1) ];
    float4 c = tex[ clamp(i+int2(0,1), 0, ires-1) ];
    float4 d = tex[ clamp(i+int2(1,1), 0, ires-1) ];

    return lerp(lerp(a, b, w.x), lerp(c, d, w.x), w.y);
}

float4 TextureNice(Texture2D tex, SamplerState sam, float2 uv, float2 sze){

    uv = uv*sze.x + 0.5;

    float2 iuv = floor( uv );
    float2 fuv = frac( uv );
    uv = iuv + fuv*fuv*(3.0-2.0*fuv);
    uv = (uv - 0.5)/sze.x;

    return tex.SampleLevel(sam, uv, 0);
    // return tex2D( sam, uv );
}

float2 TextureNice(float2 uv, float2 sze){

    uv = uv*sze.x + 0.5;

    float2 iuv = floor( uv );
    float2 fuv = frac( uv );
    uv = iuv + fuv*fuv*(3.0-2.0*fuv);
    uv = (uv - 0.5)/sze.x;

    return uv;
    // return tex2D( sam, uv );
}

//___________________________________________________________________________________________________________
	// "p" point apply texture to
	// "n" normal at "p"
	// "k" controls the sharpness of the blending in the
	//     transitions areas.
	// "s" texture sampler
float4 BoxMap( in sampler2D sam, in float3 pos, in float3 nor, in float k ){

    // project+fetch
	float4 x = tex2D( sam, pos.yz );
	float4 y = tex2D( sam, pos.zx );
	float4 z = tex2D( sam, pos.xy );
    
    // and blend
    float3 m = pow( abs(nor), (float3)k );
	return (x*m.x + y*m.y + z*m.z) / (m.x + m.y + m.z);
}

float4 BoxMap( in Texture2DArray tex, in SamplerState sam, in float3 pos, in float3 nor, in float k, int slice){

    // project+fetch
	float4 x = tex.Sample(sam, float3(pos.yz, slice));
	float4 y = tex.Sample(sam, float3(pos.zx, slice));
	float4 z = tex.Sample(sam, float3(pos.xy, slice));

	// float4 x = tex.Sample(sam, float3(pos.zy, slice) );
	// float4 y = tex.Sample(sam, float3(pos.xz, slice) );
	// float4 z = tex.Sample(sam, float3(pos.xy, slice) );
    
    // and blend
    float3 m = pow( abs(nor), (float3)k );
	return (x*m.x + y*m.y + z*m.z) / (m.x + m.y + m.z);
}

//___________________________________________________________________________________________________________







// triplanar : https://www.shadertoy.com/view/MtsGWH
// "p" point apply texture to
// "n" normal at "p"
// "k" controls the sharpness of the blending in the
//     transitions areas.
// "s" texture sampler
// float4 boxmap( in sampler2D s, in float3 p, in float3 n, in float k )
// {
//     // project+fetch
// 	float4 x = texture( s, p.yz );
// 	float4 y = texture( s, p.zx );
// 	float4 z = texture( s, p.xy );
    
//     // and blend
//     float3 m = pow( abs(n), float3(k) );
// 	return (x*m.x + y*m.y + z*m.z) / (m.x + m.y + m.z);
// }


// float4 TextureNice(Texture3D tex, SamplerState sam, float3 uv, float3 sze)
// {
//     uv = uv*sze.x + 0.5;
//     float3 iuv = floor( uv );
//     float3 fuv = frac( uv );
//     uv = iuv + fuv*fuv*(3.0-2.0*fuv);
//     uv = (uv - 0.5)/sze.x;

//     return tex.SampleLevel(sam, uv, 0);
// }

// float3 Texture23( sampler2D tex, in float3 p, in float3 n )
// {
//     n = max((abs(n) - .2)*7., .001);
//     n /= (n.x + n.y + n.z );  
// 	p = (tex2D(tex, p.yz)*n.x + tex2D(tex, p.zx)*n.y + tex2D(tex, p.xy)*n.z).xyz;
//     return p*p;
// }


// // Cubic Polynomial Smooth-minimum
// float2 smin( float a, float b, float k )
// {
//     k *= 6.0;
//     float h = max( k-abs(a-b), 0.0 )/k;
//     float m = h*h*h*0.5;
//     float s = m*k*(1.0/3.0); 
//     return (a<b) ? float2(a-s,m) : float2(b-s,1.0-m);
// }



// float CubicPulse(in float c, in float w, in float x )
// {
//     x = abs(x - c);
//     if( x>w ) return 0.0;
//     x /= w;
//     return 1.0 - x*x*(3.0-2.0*x);
// }



// float SdBox( in float2 p, in float2 b )
// {
//     float2 d = abs(p)-b;
//     return length(max(d,0.0)) + min(max(d.x,d.y),0.0);
// }


// float ExtrudeMix( float y, float d1,float d2 ,float h,bool stacked)
// {
//     float2 p=float2(0,y-h);
//     float2 a=float2(d2,-h);
//     float2 b=float2(d1,h);
//     float s= (y<h*2. && y>0. && (d2-d1)*y<d2*2.*h)?-1.:1.; 
    
//     float2 eab=p- lerp(a, b, clamp(dot(p - a, b - a) / dot(b - a,b - a), 0., 1.));
//     if(stacked) return length(eab)*s; 
    
//     float2 ea =p- a +float2(1,0)*  clamp(a.x - p.x, 0., 1e6);
//     float2 eb =p- b +float2(1,0)*  clamp(b.x - p.x, 0., 1e6);
//     float d= min(min(dot(eab,eab),dot(ea,ea)),dot(eb,eb));   
//     return sqrt(d)*s;
// }

// float InterpolateSdf4Tex(in Texture2DArray<float> zSds, in float3 pos, in float sz)
// {
//     // d1= upper SDF, d2= lower SDF
//     float box = SdBox(pos.xy, (float2)2.1);
    
//     uint3 dim;
//     zSds.GetDimensions(dim.x, dim.y, dim.z);
    
//     uint2 pix = (uint2)(clamp(abs(pos.xy * 0.5),  0.001, 0.999) * dim.xy);

//     float d0=max(zSds[uint3(pix, 0)], box) * 1;
//     float d1=max(zSds[uint3(pix, 1)], box) * 1;
//     float d2=max(zSds[uint3(pix, 2)], box) * 1;

//     // float d3=max(zSds[uint3(pix.x, pix.y,1)], box) * 1;
//     float d = 1e6;

//     // d= min(d,ExtrudeMix(pos.z       , d0,d1,sz,true));
//     // d= min(d,ExtrudeMix(pos.z+sz    , d1,d2,sz,true));
//     // d= min(d,ExtrudeMix(pos.z+2*sz  , d2,d3,sz,false));

//     d= min(d,ExtrudeMix(pos.z,-d0,-d1,sz,false));
//     d= min(d,ExtrudeMix(pos.z,-d1,-d2,sz,true));

//     return d;
// }

// #define LINEAR
// float InterpolateSdfTexArray(in Texture2DArray<float> zSds, in uint3 dim, in float3 pos, in float sz)
// {
//     uint N = dim.z;
//     float box = SdBox(pos.xy, (float2)0.5);
    
//     uint2 pix = (uint2)(clamp(abs(pos.xy * 0.5),  0.001, 0.999) * dim.xy);

//     float2 p = pos.xy;
//     p = float2(pos.z, 0.0);
    
//     float d=1e5;
//     float sg=1.;

//     float left = 0.0;
//     float right=left+float(N-2)*sz;

//     for(uint i=0;i<N-2;i++)
//     {
//         float d0=min(zSds[uint3(pix,i  )], box) * 1;
//         float d1=min(zSds[uint3(pix,i+1)], box) * 1;
//         float d2=min(zSds[uint3(pix,i+2)], box) * 1;


//         float a, b, c, shift;

//         shift=float(i)*sz;

//     #ifdef LINEAR         
//         a= 0;
//         c= d0; 
//         b= (d1-d0)/sz;
//     #else
//         c=(d0+d1)/2.;
//         b=(d1-d0)/sz;
//         a=(d0+d2-2.*d1)/2./sz/sz;
//     #endif

//         //K<0 inflate, k>0 deflate
//         //float k=.25 ; a+= k/sz/sz; b-=2.*k;

//         float2 pp=p-float2(shift+left,0);
//         if(pp.x>0. && pp.x<sz && pp.y<c+pp.x*b+pp.x*pp.x*a) sg=-1.; //sign
               
//         float xx=clamp(p.x-shift-left,0.,sz); 
//         float yy=c+xx*b +xx*xx*a;

//         // d = min(d, length(p-float2(xx+shift+left,yy)) );
//         d = min(d, length(p-float2(xx+shift+left,yy)) );

//     }

//     // //vertical borders
//     d=min(d, length(p- float2(right,zSds[uint3(pix,N-1)])+float2(0,1)*clamp(float2(right,zSds[uint3(pix,N-1)])-p.y, 0., 1e6)));
//     d=min(d, length(p- float2(left, zSds[uint3(pix,0  )])+float2(0,1)*clamp(float2(left, zSds[uint3(pix,0  )])-p.y, 0., 1e6)));
    
//     d*=sg;
//     return d;
// }