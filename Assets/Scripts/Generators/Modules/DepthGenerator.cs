/*
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;


namespace Custom.Generators.Modules
{
    public class DepthGenerator : UnityEngine.Object
    {
        static readonly string computePath   = "Assets/Shaders/Computes/DepthGenerator.compute";

        //----------------------------------------------------------------------- Gen fields
        private Vector2Int targetResolution = new(512,512);
        public Vector2Int TargetResolution {get => targetResolution; set => targetResolution = value;}

        //------------------------------------- List inputs !
        private List<Texture2D> fieldTextures;
        public List<Texture2D> FieldTextures {get => fieldTextures; set => fieldTextures = value;}
        
        private List<Texture2D> depthTextures;
        public List<Texture2D> DepthTextures {get => depthTextures; set => depthTextures = value;}

        private bool depthClamp = false;
        public bool DepthClamp {get => depthClamp; set => depthClamp = value;}

        //----------------------------------------------------------------------- Compute Property Ids
        static class CSProps
        {
            internal static readonly int targetWidth    = Shader.PropertyToID("_TargetWidth");
            internal static readonly int targetHeight   = Shader.PropertyToID("_TargetHeight");

            internal static readonly int fieldTex       = Shader.PropertyToID("_FieldTex");
            internal static readonly int fieldWidth     = Shader.PropertyToID("_FieldWidth");
            internal static readonly int fieldHeight    = Shader.PropertyToID("_FieldHeight");

            internal static readonly int depthTex       = Shader.PropertyToID("_DepthTex");
            internal static readonly int depthWidth     = Shader.PropertyToID("_DepthWidth");
            internal static readonly int depthHeight    = Shader.PropertyToID("_DepthHeight");

            internal static readonly int depthClamp     = Shader.PropertyToID("_DepthClamp");

            internal static readonly int blurIter       = Shader.PropertyToID("_BlurIter");

            internal static class KernelA
            {
                internal static readonly string name    = "CSNormalsDepth";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Colors"),
                };
            }
            internal static class KernelB
            {
                internal static readonly string name    = "CSPathDepth";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Colors"),
                    Shader.PropertyToID("_Path"),
                };
            }
            internal static class KernelC
            {
                internal static readonly string name    = "CSPlateformDepth";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Colors"),
                    Shader.PropertyToID("_NorDt"),
                };
            }
        }

        //----------------------------------------------------------------------- Inspector Methods
        public bool GenerateNormalsDepth(out Color[][] colors)
        {
            colors = null;
            if(!LoadCompute(out ComputeShader compute)) return false;
            int buffSize = InitGenCompute(compute);

            colors = new Color[depthTextures.Count][];

            for(int t = 0; t < depthTextures.Count; t++)
            {
                colors[t] = GetTopoFromHeightMap(compute, buffSize, depthTextures[t]);
                // colors[t] = GetTopoFromHeightMap(compute, buffSize, depthTextures[t], fieldTextures[t]);
            }

            DestroyImmediate(compute);
            return true;
        }

        public bool GeneratePathDepth(Color[] netPath, out Color[] colors)
        {
            colors = null;
            if(!LoadCompute(out ComputeShader compute)) return false;
            int buffSize = InitGenCompute(compute);

            colors = GetElevatedPath(compute, buffSize, netPath);
            
            DestroyImmediate(compute);
            return true;
        }

        public bool GeneratePlaterformDepth(Vector2[] norDt, out Color[] colors)
        {
            colors = null;
            if(!LoadCompute(out ComputeShader compute)) return false;
            int buffSize = InitGenCompute(compute);

            colors = GetPlateformDepth(compute, buffSize, norDt);
            
            DestroyImmediate(compute);
            return true;
        }

        //----------------------------------------------------------------------- Compute General Params
        private int InitGenCompute(ComputeShader compute)
        {
            compute.SetFloat(CSProps.targetWidth,  targetResolution.x);
            compute.SetFloat(CSProps.targetHeight, targetResolution.y);
            
            return targetResolution.x * targetResolution.y;
        }

        private void SetComputeDepthTexture(ComputeShader compute, int kernel, Texture2D tex)
        {
            compute.SetTexture(kernel, CSProps.depthTex, tex);
            compute.SetFloat(CSProps.depthWidth,  tex.width);
            compute.SetFloat(CSProps.depthHeight, tex.height);
            compute.SetBool(CSProps.depthClamp, depthClamp);
        }

        //----------------------------------------------------------------------- Kernels Executions    
        // Execute KernelA (CSPathTerrain)
        private Color[] GetTopoFromHeightMap(ComputeShader compute, int buffSize, Texture2D depthTex, Texture2D fieldTex = null)
        {
            int kernel = compute.FindKernel(CSProps.KernelA.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            SetComputeDepthTexture(compute, kernel, depthTex);

            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new (buffSize, sizeof(float) * 4),
            };
            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelA.buffers, buffers);
            compute.Dispatch(kernel, warpCount, 1,1);

            Color[] colors = new Color[buffSize];
            buffers[0].GetData(colors);

            ComputeUtils.DisposeBuffers(buffers);

            return colors;
        }

        // Execute KernelB
        private Color[] GetElevatedPath(ComputeShader compute, int buffSize, Color[] netPath)
        {
            int kernel = compute.FindKernel(CSProps.KernelB.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            SetComputeDepthTexture(compute, kernel, depthTextures[0]);

            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new (buffSize, sizeof(float) * 4),
                new (buffSize, sizeof(float) * 4),
            };

            buffers[1].SetData(netPath);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelB.buffers, buffers);
            compute.Dispatch(kernel, warpCount, 1,1);

            Color[] colors = new Color[buffSize];
            buffers[0].GetData(colors);

            ComputeUtils.DisposeBuffers(buffers);

            return colors;
        }

        // Execute KernelC
        private Color[] GetPlateformDepth(ComputeShader compute, int buffSize, Vector2[] norDt)
        {
            int kernel = compute.FindKernel(CSProps.KernelC.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);


            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new (buffSize, sizeof(float) * 4),
                new (buffSize, sizeof(float) * 2),
            };

            buffers[1].SetData(norDt);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelC.buffers, buffers);
            compute.Dispatch(kernel, warpCount, 1,1);

            Color[] colors = new Color[buffSize];
            buffers[0].GetData(colors);

            ComputeUtils.DisposeBuffers(buffers);

            return colors;
        }


        //----------------------------------------------------------------------- Error Handlers
        public static bool HasTexture(Texture2D tex, string name)
        {
            if(tex == null){
                string msg = "you must provide a " + name + "texture (made by the path maker)\nABORT";
                EditorUtility.DisplayDialog ("No Texture found", msg, "Ok");
                return false;
            }
            else return true;
        }

        private bool LoadCompute(out ComputeShader compute)
        {
            // compute = null;
            // if(!HasTexture(fieldTextures, "path") || !HasTexture(depthTextures, "heights")) return false;

            compute = Instantiate(AssetDatabase.LoadAssetAtPath(computePath, typeof(ComputeShader))) as ComputeShader;

            if(compute == null){
                Debug.LogWarning("compute shader HeightGenerator not found ! make sur you have the right asset path for field computePath");
                return false;
            }

            return true;
        }
    }
}








//_________________________________________________________________________________________

        // private UpAxis depthAxis;
        // public UpAxis DepthAxis{get => depthAxis; set => depthAxis = value;}