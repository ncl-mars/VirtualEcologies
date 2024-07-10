/*
sdf_Transforms These are taken from MichaelPohoreski's RayMarching how to here: 
https://www.shadertoy.com/view/XllGW4

#define rot(a)      mat2( cos(a), -sin(a), sin(a), cos(a) )
*/
//--------------------------------------------------------------------- Base
//

#ifndef TRANSFORM_UTILS_INCLUDED
    #define TRANSFORM_UTILS_INCLUDED

    float3 Erot(in float3 p, in float3 ax, in float ro) {
        return lerp(dot(p,ax)*ax,p,cos(ro))+sin(ro)*cross(ax,p);
    }

    //
    float4x4 RotationAxisAngle(in float3 v, in float angle){
        float s = sin( angle );
        float c = cos( angle );
        float ic = 1.0 - c;

        return float4x4(v.x*v.x*ic + c,     v.y*v.x*ic - s*v.z, v.z*v.x*ic + s*v.y, 0.0,
                        v.x*v.y*ic + s*v.z, v.y*v.y*ic + c,     v.z*v.y*ic - s*v.x, 0.0,
                        v.x*v.z*ic - s*v.y, v.y*v.z*ic + s*v.x, v.z*v.z*ic + c,     0.0,
                        0.0,                0.0,                0.0,                1.0 );
    }

    //
    float4x4 Translate(in float x, in float y, in float z){
        return float4x4(1.0, 0.0, 0.0, 0.0,
                        0.0, 1.0, 0.0, 0.0,
                        0.0, 0.0, 1.0, 0.0,
                        x,   y,   z,   1.0 );
    }

    //
    float2x2 Rot2D(in float a) {
        return float2x2(cos(a),-sin(a),sin(a),cos(a));
    }

    float3x3 Rot3D(in float3 angles) {
        float3 c = cos(angles);
        float3 s = sin(angles);
        float3x3 rotX = float3x3(1.0, 0.0, 0.0, 0.0, c.x, s.x, 0.0, -s.x, c.x);
        float3x3 rotY = float3x3(c.y, 0.0, -s.y, 0.0, 1.0, 0.0, s.y, 0.0, c.y);
        float3x3 rotZ = float3x3(c.z, s.z, 0.0, -s.z, c.z, 0.0, 0.0, 0.0, 1.0);
        return rotX * rotY * rotZ;
    }

    //--------------------------------------------------------------------- Extras
    // Return 4x4 rotation X matrix, angle in radians
    float4x4 Rot4X(in float a) {
        float c = cos( a );
        float s = sin( a );
        return float4x4(1, 0, 0, 0,
                        0, c,-s, 0,
                        0, s, c, 0,
                        0, 0, 0, 1);
    }

    // Return 4x4 rotation Y matrix, angle in radians
    float4x4 Rot4Y(in float a) {
        float c = cos( a );
        float s = sin( a );
        return float4x4(c, 0, s, 0,
                        0, 1, 0, 0,
                        -s, 0, c, 0,
                        0, 0, 0, 1);
    }

    // Return 4x4 rotation Z matrix, angle in radians
    float4x4 Rot4Z(in float a) {
        float c = cos( a );
        float s = sin( a );
        return float4x4(c,-s, 0, 0,
                        s, c, 0, 0,
                        0, 0, 1, 0,
                        0, 0, 0, 1);
    }


    // http://stackoverflow.com/questions/349050/calculating-a-lookat-matrix
    float4x4 AxisMatrix(float3 right, float3 up, float3 forward){

        float3 xaxis = right;
        float3 yaxis = up;
        float3 zaxis = forward;
        return float4x4(
            xaxis.x, yaxis.x, zaxis.x, 0,
            xaxis.y, yaxis.y, zaxis.y, 0,
            xaxis.z, yaxis.z, zaxis.z, 0,
            0, 0, 0, 1
        );
    }

    float4x4 LookAt4x4(float3 forward, float3 up){

        float3 xaxis = normalize(cross(forward, up));
        float3 yaxis = up;
        float3 zaxis = forward;
        return AxisMatrix(xaxis, yaxis, zaxis);
    }

    float4x4 LookAt4x4(float3 at, float3 eye, float3 up){

        float3 zaxis = normalize(at - eye);
        float3 xaxis = normalize(cross(up, zaxis));
        float3 yaxis = cross(zaxis, xaxis);
        return AxisMatrix(xaxis, yaxis, zaxis);
    }
#endif








// float3x3 LookAt(in float3 pos, in float3 target) {
//     float3 f = normalize(target - pos);         // Forward
//     float3 r = normalize(float3(-f.z, 0.0, f.x)); // Right
//     float3 u = cross(r, f);                     // Up
//     return float3x3(r, u, f);
// }

// float4x4 LookAt4x4(float3 forward, float3 up)
// {
//     float3 xaxis = normalize(cross(forward, up));
//     float3 yaxis = up;
//     float3 zaxis = forward;
//     return AxisMatrix(xaxis, yaxis, zaxis);
// }

// float4x4 LookAt4x4(float3 at, float3 eye, float3 up)
// {
//     float3 zaxis = normalize(at - eye);
//     float3 xaxis = normalize(cross(up, zaxis));
//     float3 yaxis = cross(zaxis, xaxis);
//     return AxisMatrix(xaxis, yaxis, zaxis);
// }


// //______________________________________________________ DEPRECIATED !
// // Translate is simply: p - d
// // opTx will do transpose(m)
// // p' = m*p
// //    = [m0 m1 m2 m3 ][ p.x ]
// //      [m4 m5 m6 m7 ][ p.y ]
// //      [m8 m9 mA mB ][ p.z ]
// //      [mC mD mE mF ][ 1.0 ]
// float4x4 Loc4(in float3 p) {
//     p *= -1.;
//     return float4x4(
//         1,  0,  0,  p.x,
//         0,  1,  0,  p.y,
//         0,  0,  1,  p.z,
//         0,  0,  0,  1
//     );
// }

// // BETTER TO USE transpose() hlsl function !
// float4x4 TransposeM4(in float4x4 m) {
//     float4 r0 = m[0];
//     float4 r1 = m[1];
//     float4 r2 = m[2];
//     float4 r3 = m[3];

//     float4x4 t = float4x4(
//             float4( r0.x, r1.x, r2.x, r3.x ),
//             float4( r0.y, r1.y, r2.y, r3.y ),
//             float4( r0.z, r1.z, r2.z, r3.z ),
//             float4( r0.w, r1.w, r2.w, r3.w )
//     );
//     return t;
// }


// float3x3 LookAt(in float3 pos, in float3 target) {
//     float3 f = normalize(target - pos);         // Forward
//     float3 r = normalize(float3(-f.z, 0.0, f.x)); // Right
//     float3 u = cross(r, f);                     // Up
//     return float3x3(r, u, f);
// }


// // http://stackoverflow.com/questions/349050/calculating-a-lookat-matrix
// float4x4 AxisMatrix(float3 right, float3 up, float3 forward)
// {
//     float3 xaxis = right;
//     float3 yaxis = up;
//     float3 zaxis = forward;
//     return float4x4(
// 		xaxis.x, yaxis.x, zaxis.x, 0,
// 		xaxis.y, yaxis.y, zaxis.y, 0,
// 		xaxis.z, yaxis.z, zaxis.z, 0,
// 		0, 0, 0, 1
// 	);
// }

// float4x4 LookAt4x4(float3 forward, float3 up)
// {
//     float3 xaxis = normalize(cross(forward, up));
//     float3 yaxis = up;
//     float3 zaxis = forward;
//     return AxisMatrix(xaxis, yaxis, zaxis);
// }

// float4x4 LookAt4x4(float3 at, float3 eye, float3 up)
// {
//     float3 zaxis = normalize(at - eye);
//     float3 xaxis = normalize(cross(up, zaxis));
//     float3 yaxis = cross(zaxis, xaxis);
//     return AxisMatrix(xaxis, yaxis, zaxis);
// }


// float3x3 LookAt(float3 forward, float3 up) {

//     // float3 f = forward;         // Forward
//     // float3 r = normalize(float3(-f.z, 0.0, f.x)); // Right
//     // float3 u = up;                     // Up
//     // return float3x3(r, u, f);

//     float3 right = normalize(cross(forward, up));
//     return float3x3(right, up, forward);
// }



