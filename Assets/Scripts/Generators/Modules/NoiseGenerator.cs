/*
    A Generator of noise textures

    --- Noises Enum ---
  0 - 0
  1 - Perlin
  2 - Fractal
  3 - Perlin, Fractal
  4 - Gradient_Fractal
  5 - Perlin, Gradient_Fractal
  6 - Fractal, Gradient_Fractal
  7 - Perlin, Fractal, Gradient_Fractal
  8 - FBM
  9 - Perlin, FBM
 10 - Fractal, FBM
 11 - Perlin, Fractal, FBM
 12 - Gradient_Fractal, FBM
 13 - Perlin, Gradient_Fractal, FBM
 14 - Fractal, Gradient_Fractal, FBM
 15 - Perlin, Fractal, Gradient_Fractal, FBM
 16 - FBM_Derivatives
 17 - Perlin, FBM_Derivatives
 18 - Fractal, FBM_Derivatives
 19 - Perlin, Fractal, FBM_Derivatives
 20 - Gradient_Fractal, FBM_Derivatives
 21 - Perlin, Gradient_Fractal, FBM_Derivatives
 22 - Fractal, Gradient_Fractal, FBM_Derivatives
 23 - Perlin, Fractal, Gradient_Fractal, FBM_Derivatives
 24 - FBM, FBM_Derivatives
 25 - Perlin, FBM, FBM_Derivatives
 26 - Fractal, FBM, FBM_Derivatives
 27 - Perlin, Fractal, FBM, FBM_Derivatives
 28 - Gradient_Fractal, FBM, FBM_Derivatives
 29 - Perlin, Gradient_Fractal, FBM, FBM_Derivatives
 30 - Fractal, Gradient_Fractal, FBM, FBM_Derivatives
 31 - Perlin, Fractal, Gradient_Fractal, FBM, FBM_Derivatives

*/

using UnityEngine;
using UnityEditor;
using Custom;
using System;

namespace Custom.Generators.Modules
{
    internal static class CSProps
    {
        internal static readonly int resolution    = Shader.PropertyToID("_Resolution");
        internal static readonly int scale      = Shader.PropertyToID("_Scale");
        internal static readonly int depth      = Shader.PropertyToID("_Depth");
        internal static readonly int normalized = Shader.PropertyToID("_Normalized");
        internal static readonly int noiseToAlpha = Shader.PropertyToID("_NoiseToAlpha");

        internal static string[] k_names = {
            "CSPerlin",
            "CSFractal",
            "CSFractalGrad",
            "CSFbm",
            "CSFbmDerivatives",
        };


        internal static readonly int[] buffers  = {
            Shader.PropertyToID("_Result1"),
            Shader.PropertyToID("_Result2"),
            Shader.PropertyToID("_Result4"),
        };
    }

    ////////////////////////////////////////////////////////////////////////////////
    // Generator's feature modes
    [Flags] public enum NoiseTypes : short
    {
        Perlin = 1,
        Fractal = 2,
        Fractal_Gradients = 4,
        FBM = 8,
        FBM_Derivatives = 16,
        Worley = 32,
    }
    
    public class NoiseGenerator : UnityEngine.Object
    {
        // public ComputeShader compute;
        static readonly string computePath   = "Assets/Shaders/Computes/NoiseGenerator.compute";
        
        private NoiseTypes noiseType = NoiseTypes.Perlin;
        public NoiseTypes NoiseType{get=>noiseType; set=> noiseType = value;}

        private Vector3Int targetResolution;
        public Vector3Int TargetResolution{get=>targetResolution; set=>targetResolution = value;}
        
        public bool NoiseToAlpha{get;set;}

        [Range(0.0001f,1000.0f)]
        private float scale = 10;
        public float Scale{get=>scale; set=>scale=value;}

        [Tooltip("either 0 centered or normalized (0-1)")]
        private bool normalized = true;
        public bool Normalized{get=>normalized; set=>normalized=value;}


        public bool GenerateFloat(out float[][] results)
        {
            results = null;
            if(!LoadCompute(out ComputeShader compute, out int buffSize, out int kernel, out int warpCount))return false;

            ComputeBuffer buffer = new(buffSize, sizeof(float));
            compute.SetBuffer(kernel, CSProps.buffers[0], buffer);

            results = new float[targetResolution.z][];

            for(int z = 0; z < targetResolution.z; z++)
            {
                results[z] = new float[buffSize];

                compute.SetInt(CSProps.depth, z);
                compute.Dispatch(kernel, warpCount, 1, 1); // trigger computations
                buffer.GetData(results[z]); // transfert data
            }
            buffer.Dispose();

            DestroyImmediate(compute);
            return true;
        }

        public bool GenerateColor(out Color[][] results)
        {
            results = null;
            if(!LoadCompute(out ComputeShader compute, out int buffSize, out int kernel, out int warpCount))return false;

            ComputeBuffer buffer = new(buffSize, sizeof(float)*4);
            compute.SetBuffer(kernel, CSProps.buffers[2], buffer);

            results = new Color[targetResolution.z][];

            for(int z = 0; z < targetResolution.z; z++)
            {
                results[z] = new Color[buffSize];

                compute.SetInt(CSProps.depth, z);
                compute.Dispatch(kernel, warpCount, 1, 1); // trigger computations
                buffer.GetData(results[z]); // transfert data
            }
            buffer.Dispose();

            DestroyImmediate(compute);
            return true;
        }

        // set main params once at init
        private void SetComputeGenParams(ComputeShader compute)
        {
            compute.SetVector(CSProps.resolution, new Vector4(targetResolution.x, targetResolution.y, targetResolution.z, 1));
            compute.SetFloat(CSProps.scale, scale);
            compute.SetBool(CSProps.normalized, normalized);
            compute.SetBool(CSProps.noiseToAlpha, NoiseToAlpha);
        }

        private bool LoadCompute(out ComputeShader compute, out int buffSize, out int kernel, out int warpCount)
        {
            buffSize = kernel = warpCount = 0;
            compute = Instantiate(AssetDatabase.LoadAssetAtPath(computePath, typeof(ComputeShader))) as ComputeShader;

            if(compute == null){
                Debug.LogWarning("compute shader HeightGenerator not found ! make sur you have the right asset path for field computePath");
                return false;
            }
            if(targetResolution.x == 0 || targetResolution.y == 0 || targetResolution.z == 0){
                string msg = "targetResolution to zero detected\nyou must set a proper output targetResolution\nABORT";
                EditorUtility.DisplayDialog ("Wrong output targetResolution", msg, "Ok");
                return false;
            }

            buffSize = targetResolution.x * targetResolution.y;
            
            kernel = noiseType switch
            {
                NoiseTypes.Perlin => compute.FindKernel(CSProps.k_names[0]),
                NoiseTypes.Fractal => compute.FindKernel(CSProps.k_names[1]),
                NoiseTypes.Fractal_Gradients => compute.FindKernel(CSProps.k_names[2]),
                NoiseTypes.FBM => compute.FindKernel(CSProps.k_names[3]),
                NoiseTypes.FBM_Derivatives => compute.FindKernel(CSProps.k_names[4]),
                _=> default,
            };

            warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            SetComputeGenParams(compute);

            // Debug.Log("compute loaded to generate noise type : " + noiseType);
            return true;
        }
    }
}







//_______________________________________________________________________________________________
// public enum ExportType
// {
//     Texture2D           = 0,
//     Texture2DArray      = 1,
//     Texture3D           = 2
// };

// private void Export(float[][] results, string name)
// {        
//     Texture tex;
//     if(targetResolution.z < 2) tex = TexUtils.CreateTex2D(results[0], targetResolution.x, targetResolution.y, half);
//     else tex = TexUtils.CreateTex3D(results, targetResolution.x, targetResolution.y, half);

//     tex.filterMode = FilterMode.Trilinear;
//     tex.wrapMode = TextureWrapMode.Repeat;

//     string path = opath + "/" + name;
//     TexUtils.SaveTextureAsset(tex, path);
// }


        //-------------------------------------------------------------------------- Kernels
        // // execute kernelA
        // public bool GeneratePerlin(out float[][] results)
        // {
        //     results = null;
        //     if(!LoadCompute(out ComputeShader compute, out int buffSize, out int kernel, out int warpCount))return false;

        //     ComputeBuffer buffer = new(buffSize, sizeof(float));
        //     compute.SetBuffer(kernel, CSProps.buffers[0], buffer);

        //     results = new float[targetResolution.z][];

        //     for(int z = 0; z < targetResolution.z; z++)
        //     {
        //         results[z] = new float[buffSize];

        //         compute.SetInt(CSProps.depth, z);
        //         compute.Dispatch(kernel, warpCount, 1, 1); // trigger computations
        //         buffer.GetData(results[z]); // transfert data
        //     }
        //     buffer.Dispose();

        //     DestroyImmediate(compute);
        //     return true;
        // }

        // // execute kernelB
        // public bool GenerateFractal(out float[][] results)
        // {
        //     results = null;
        //     if(!LoadCompute(out ComputeShader compute, out int buffSize, out int kernel, out int warpCount))return false;

        //     ComputeBuffer buffer = new(buffSize, sizeof(float));
        //     compute.SetBuffer(kernel, CSProps.buffers[0], buffer);

        //     results = new float[targetResolution.z][];

        //     for(int z = 0; z < targetResolution.z; z++)
        //     {
        //         results[z] = new float[buffSize];

        //         compute.SetInt(CSProps.depth, z);
        //         compute.Dispatch(kernel, warpCount, 1, 1); // trigger computations
        //         buffer.GetData(results[z]); // transfert data
        //     }
        //     buffer.Dispose();

        //     DestroyImmediate(compute);
        //     return true;
        // }

        // execute kernelC
        // public bool GenerateFractalGradient(out Color[][] results)
        // {
        //     results = null;
        //     if(!LoadCompute(out ComputeShader compute, out int buffSize, out int kernel, out int warpCount))return false;

        //     ComputeBuffer buffer = new(buffSize, sizeof(float)*4);
        //     compute.SetBuffer(kernel, CSProps.buffers[2], buffer);

        //     results = new Color[targetResolution.z][];

        //     for(int z = 0; z < targetResolution.z; z++)
        //     {
        //         results[z] = new Color[buffSize];

        //         compute.SetInt(CSProps.depth, z);
        //         compute.Dispatch(kernel, warpCount, 1, 1); // trigger computations
        //         buffer.GetData(results[z]); // transfert data
        //     }
        //     buffer.Dispose();

        //     DestroyImmediate(compute);
        //     return true;
        // }
