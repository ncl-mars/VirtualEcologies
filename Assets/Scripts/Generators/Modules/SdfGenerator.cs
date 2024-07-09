/*
RESUME:
    Use the component SdfGeneratorUI to use in standalone mode, 
    => Main methods will be available from it's contextual menu

    this object is also used by the sdfCollection component to generate 2d Sdf
    for ray marching materials (see SdfCompositor)

    This is not operating at runtime, just in edit in the editor, 
    SDF texture asset will be saved at the root colorTexture path, followed by suffix "_SDF"
    
TODO:
    Less code would be great, as usual !

    + WarpCount (strides) could be calculated once and passed through
    + Kernels could also be found once, stored in int[] and passed through
    + make inspector dynamic and conditional (ex = show exportFormat if List<Texture2D> > 1)

NOTE ON OPTIMIZATION:

REFS-N-LINKS:
    AMD TressFX : https://github.com/GPUOpen-Effects/TressFX
    unityteam TressFX implementation : https://github.com/Unity-Technologies/com.unity.demoteam.mesh-to-sdf/tree/main
*/
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Custom.Generators.Modules
{
    // trigo circle (anti horaire), int flag = half pi
    public enum ReadRotation
    {
        // Manual      = -1, // TODO ?
        None        = 0,
        OneQuart    = 1,
        Half        = 2,
        ThreeQuart  = 3,
    }

    /// /////////////////////////////////////////////////////////////////////////////
    /// Generator's feature modes
    public enum ChannelMode
    {
        RedThresh       = 0,
        GreenThresh     = 1,
        BlueThresh      = 2,
        AlphaThresh     = 3,
        // RGBThresh       = 4, // (R + G + B) / 3 > thresh
    };

    /// /////////////////////////////////////////////////////////////////////////////
    /// Generator object
    public class SdfGenerator : UnityEngine.Object
    {
        static readonly string computePath   = "Assets/Shaders/Computes/SdfGenerator.compute";

        // ------------------------------------------------------------Gen fields
        protected Vector2Int targetResolution = new(512,512);
        public Vector2Int TargetResolution{ get => targetResolution; set => targetResolution = value;}

        protected List<Texture2D> textures;
        public List<Texture2D> Textures{ get => textures; set => textures = value;}

        protected ChannelMode channelMode = ChannelMode.AlphaThresh;
        public ChannelMode ChannelMode{ get => channelMode; set => channelMode = value;}

        protected float thresh = 0;
        public float Thresh{ get => thresh; set => thresh = value;}

        protected uint blurIterations = 0;
        public uint BlurIterations{ get => blurIterations; set => blurIterations = value;}

        protected bool keepFieldExact = false;
        public bool KeepFieldExact{ get => keepFieldExact; set => keepFieldExact = value;}

        protected bool computeNormals = false;
        public bool ComputeNormals{ get => computeNormals; set => computeNormals = value;}

        protected bool halfDistances   = false;
        public bool HalfDistances{ get => halfDistances; set => halfDistances = value;}

        protected bool halfNormals = false;
        public bool HalfNormals{ get => halfNormals; set => halfNormals = value;}

        protected int blurPaths = 0;
        public int BlurPaths{ get => blurPaths; set => blurPaths = value;}

        protected float smoothPathUnion = 0.0f;
        public float SmoothPathUnion{get => smoothPathUnion; set => smoothPathUnion = value;}

        protected List<float> readAngles;
        public List<float> ReadAngles {get => readAngles; set => readAngles = value;}

        protected bool compressNorDt = false;
        public bool CompressNorDt{ get => compressNorDt; set => compressNorDt = value;}


        //----------------------------------------------------------------------- Compute Property Ids
        static class CSProps
        {
            internal static readonly int targetWidth    = Shader.PropertyToID("_TargetWidth");
            internal static readonly int targetHeight   = Shader.PropertyToID("_TargetHeight");

            internal static readonly int sourceTex      = Shader.PropertyToID("_SourceTex");
            internal static readonly int sourceWidth    = Shader.PropertyToID("_SourceWidth");
            internal static readonly int sourceHeight   = Shader.PropertyToID("_SourceHeight");

            internal static readonly int thresh         = Shader.PropertyToID("_Thresh");
            internal static readonly int blurIter       = Shader.PropertyToID("_BlurIter");
            internal static readonly int channelMode    = Shader.PropertyToID("_ChannelMode");
            internal static readonly int smoothPath     = Shader.PropertyToID("_SmoothPathUnion");
            internal static readonly int pixCount       = Shader.PropertyToID("_PixelCount");

            internal static readonly int compressNorDt  = Shader.PropertyToID("_CompressNorDt");

            // internal static readonly int readAngle      = Shader.PropertyToID("_ReadAngle");
            
            internal static readonly string[] keys      = {"_SDF_INPUT",}; // TODO: Remove directive

            internal static class KernelA
            {
                internal static readonly string name    = "CSOutlines";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Result"),
                    Shader.PropertyToID("_OutlineID_A"),
                    Shader.PropertyToID("_SDF"),
                };
            }
            internal static class KernelB
            {
                internal static readonly string name    = "CSDistances";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Result"),
                    Shader.PropertyToID("_Sign"),
                    Shader.PropertyToID("_OutlineID"),
                };
            }
            internal static class KernelC
            {
                internal static readonly string name    = "CSBlurDistances";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Result"),
                    Shader.PropertyToID("_SDF"),
                };
            }
            internal static class KernelD
            {
                internal static readonly string name    = "CSNormals";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Normals"),
                    Shader.PropertyToID("_SDF"),
                };
            }
            internal static class KernelE
            {
                internal static readonly string name    = "CSFormater";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Colors"),
                };
            }
            internal static class KernelF
            {
                internal static readonly string name    = "CSPathLine";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_SDF"),
                    Shader.PropertyToID("_Normals"),
                    Shader.PropertyToID("_OutlineID_A"),

                    Shader.PropertyToID("_Result"),
                };
            }
            internal static class KernelG
            {
                internal static readonly string name    = "CSPathFromLine";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_SDF"),
                    Shader.PropertyToID("_Normals"),
                    Shader.PropertyToID("_PathID"), // path line pixel ids
                    Shader.PropertyToID("_Colors"),
                };
            }
            internal static class KernelH
            {
                internal static readonly string name    = "CSMergePath";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Path"),
                    Shader.PropertyToID("_Colors"),
                };
            }
            internal static class KernelI
            {
                internal static readonly string name    = "CSBlurNetPath";
                internal static readonly int[] buffers  = {
                    Shader.PropertyToID("_Path"),
                    Shader.PropertyToID("_Colors"),
                };
            }
        }
        
        //----------------------------------------------------------------------- Generation Methods
        public bool GenerateSdf(out float[][] sdfs , out Vector2[][] normals)
        {
            sdfs = null; normals = null;
            if(!LoadCompute(out ComputeShader compute)) return false;

            SetComputeGenParams(compute); // inspector generation params to compute
            int buffSize = targetResolution.x * targetResolution.y;  // buffersize correspond to one slice/tex

            sdfs = new float[textures.Count][];
            if(computeNormals) normals = new Vector2[textures.Count][]; 

            for(int t = 0; t < textures.Count; t++)
            {
                OutlinesResults outlines = GetOutlines(compute, buffSize, textures[t]);
                if(ExitNoShape(compute, outlines)) return false; // destroy compute and exit if no shape is found

                sdfs[t] = GetDistances(compute, outlines);

                if(blurIterations > 0)
                {
                    sdfs[t] = GetBlurredDistances(compute, sdfs[t]);
                    
                    if(keepFieldExact)
                    {
                        outlines    = GetOutlines(compute, buffSize, sdf: sdfs[t], isSdfInput: true);
                        sdfs[t]     = GetDistances(compute, outlines);
                    }
                }
                if(computeNormals) normals[t] = GetNormals(compute, sdfs[t]);
            }
            DestroyImmediate(compute); // destroy compute and save results
            return true;
        }

        public bool ConvertList(out Color[][] colors)
        {
            colors = null;
            if(!LoadCompute(out ComputeShader compute, false)) return false;

            SetComputeGenParams(compute, false); // inspector generation params to compute
            int buffSize = targetResolution.x * targetResolution.y;  // buffersize correspond to one slice/tex

            colors = new Color[textures.Count][];

            for(int t = 0; t < textures.Count; t++) colors[t] = GetConvertTex(compute, buffSize, textures[t]);

            DestroyImmediate(compute); // destroy compute and save results
            return true;
        }

        public bool GeneratePathFromSdf(float[][] sdfs , Vector2[][] normals, out Color[]netPath)
        {
            netPath = null;
            if(!LoadCompute(out ComputeShader compute, false)) return false;

            SetComputeGenParams(compute, false); // inspector generation params to compute
            int buffSize = targetResolution.x * targetResolution.y;  // buffersize correspond to one slice/tex

            for(int t = 0; t < sdfs.Length; t++) 
            {
                float[] pathIds = GetPathLine(compute, buffSize, sdfs[t], normals[t]); // ids as float to match strides
                Color[] curPath = GetPathFromLine(compute, buffSize, sdfs[t], normals[t], ReorderPathIds(pathIds, t));
                
                if(t > 0) netPath = GetMergedPath(compute, buffSize, curPath, netPath);
                else netPath = curPath;
            }

            if(blurPaths > 0) netPath = GetBlurredPath(compute, buffSize, netPath);

            DestroyImmediate(compute);
            return true;
        }

        //----------------------------------------------------------------------- Compute General Params
        private void SetComputeGenParams(ComputeShader compute, bool isSdfGen = true)
        {
            compute.SetFloat(CSProps.targetWidth,  targetResolution.x);
            compute.SetFloat(CSProps.targetHeight, targetResolution.y);

            if(isSdfGen)
            {
                compute.SetInt(CSProps.blurIter, (int)blurIterations);
                compute.SetFloat(CSProps.thresh, thresh);
                compute.SetFloat(CSProps.channelMode, Mathf.Clamp((int)channelMode, 0, 3)); // channel to use to isolate shape
            }

            compute.SetBool(CSProps.compressNorDt, compressNorDt);
        }

        private void SetSourceDimensions(ComputeShader compute, Texture2D tex)
        {
            compute.SetFloat(CSProps.sourceWidth,  tex.width);
            compute.SetFloat(CSProps.sourceHeight, tex.height);
        }

        //----------------------------------------------------------------------- Kernels Executions    
        // Execute KernelA (CSOutlines) from the compute shader
        private OutlinesResults GetOutlines(ComputeShader compute, int buffSize, Texture2D srcTex = null, float[]sdf = null, bool isSdfInput = false)
        {
            int kernel = compute.FindKernel(CSProps.KernelA.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            OutlinesResults results = new(){ sign = new float[buffSize] };
            ComputeBuffer[] buffers;

            if(isSdfInput){
                compute.EnableKeyword(CSProps.keys[0]); // set SDF as input
                buffers = new ComputeBuffer[]{
                    new(results.sign.Length, sizeof(float)),                                // result
                    new(results.sign.Length, sizeof(float), ComputeBufferType.Append),      // _OutlineID_A (appendBuffer)
                    new(results.sign.Length, sizeof(float)),                                // _SDF 
                };
                buffers[2].SetData(sdf);
            }
            else
            {
                compute.DisableKeyword(CSProps.keys[0]); // set Texture source as input
                compute.SetTexture(kernel, CSProps.sourceTex, srcTex);
                SetSourceDimensions(compute, srcTex);
            
                buffers = new ComputeBuffer[]{
                    new(results.sign.Length, sizeof(float)),                              // result
                    new(results.sign.Length, sizeof(float), ComputeBufferType.Append),    // _OutlineID_A (appendBuffer)
                };
            }

            // init buffers
            buffers[0].SetData(results.sign);
            buffers[1].SetCounterValue(0);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelA.buffers, buffers);

            compute.Dispatch(kernel, warpCount, 1, 1); // trigger computations

            // get counter value from one of the pos buffers
            int counter = ComputeUtils.GetAppendCount(buffers[1]);

            // create CPU array container of length = counter
            results.outlineIDs = new float[counter];

            //-- gpu to cpu transfert
            buffers[0].GetData(results.sign);
            buffers[1].GetData(results.outlineIDs, 0, 0, counter);

            ComputeUtils.DisposeBuffers(buffers);

            return results; 
        }

        // Execute KernelB (CSDistances) from the compute shader
        private float[] GetDistances(ComputeShader compute, OutlinesResults outlines)
        {
            int kernel = compute.FindKernel(CSProps.KernelB.name);
            compute.SetInt(CSProps.pixCount, outlines.outlineIDs.Length);

            float[] result = new float [outlines.sign.Length];

            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, result.Length);

            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new (result.Length, sizeof(float)),         // Result
                new (outlines.sign.Length, sizeof(float)), // Sign
                new (outlines.outlineIDs.Length, sizeof(float)),  // _OutlineID
            };

            //-- init buffers
            buffers[1].SetData(outlines.sign);
            buffers[2].SetData(outlines.outlineIDs);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelB.buffers, buffers);

            compute.Dispatch(kernel, warpCount, 1, 1); // trigger computations

            buffers[0].GetData(result); // transfert data

            ComputeUtils.DisposeBuffers(buffers);

            return result;
        }

        // Execute KernelC (CSBlurDistances) from the compute shader
        private float[] GetBlurredDistances(ComputeShader compute, float[] sdf)
        {
            int kernel = compute.FindKernel(CSProps.KernelC.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, sdf.Length);

            // create buffers
            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new(sdf.Length, sizeof(float)),     // result
                new(sdf.Length, sizeof(float)),     // sdf
            };

            buffers[1].SetData(sdf);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelC.buffers, buffers);

            compute.Dispatch(kernel, warpCount, 1, 1);

            buffers[0].GetData(sdf);
            ComputeUtils.DisposeBuffers(buffers);

            return sdf;
        }

        // Execute KernelD (CSNormals) from the compute shader
        private Vector2[] GetNormals(ComputeShader compute, float[] sdf)
        {
            int kernel = compute.FindKernel(CSProps.KernelD.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, sdf.Length);

            Vector2[] normals = new Vector2[sdf.Length];

            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new(sdf.Length, sizeof(float) * 2),     // normals
                new(sdf.Length, sizeof(float)),         // sdf
            };

            buffers[1].SetData(sdf);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelD.buffers, buffers);

            compute.Dispatch(kernel, warpCount, 1, 1);

            buffers[0].GetData(normals);

            ComputeUtils.DisposeBuffers(buffers);

            return normals;
        }

        // Execute KernelE (CSFormat) from the compute shader
        private Color[] GetConvertTex(ComputeShader compute, int buffSize, Texture2D srcTex)
        {
            int kernel = compute.FindKernel(CSProps.KernelE.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);
            
            compute.SetTexture(kernel, CSProps.sourceTex, srcTex);
            SetSourceDimensions(compute, srcTex);

            ComputeBuffer[] buffers = new ComputeBuffer[]{new(buffSize, sizeof(float) * 4)};

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelE.buffers, buffers);

            compute.Dispatch(kernel, warpCount, 1, 1);

            Color[] colors = new Color[buffSize];
            buffers[0].GetData(colors);

            ComputeUtils.DisposeBuffers(buffers);

            return colors;
        }

        //  Execute KernelF (CSPathLine) from the compute shader
        private float[] GetPathLine(ComputeShader compute, int buffSize, float[] sdfs , Vector2[] normals)
        {
            int kernel = compute.FindKernel(CSProps.KernelF.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            ComputeBuffer[] buffers = new ComputeBuffer[]
            {
                new(buffSize, sizeof(float)),     // sdf
                new(buffSize, sizeof(float) * 2), // normals
                new(buffSize, sizeof(float), ComputeBufferType.Append),    // _OutlineID_A (appendBuffer)
            };
            
            buffers[0].SetData(sdfs);
            buffers[1].SetData(normals);
            buffers[2].SetCounterValue(0);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelF.buffers, buffers);

            compute.Dispatch(kernel, warpCount, 1, 1);

            int counter = ComputeUtils.GetAppendCount(buffers[2]);

            float[] lineIds = new float[counter];
            buffers[2].GetData(lineIds, 0, 0, counter);

            // Debug.LogFormat("found {0} pixels on path line", counter);

            ComputeUtils.DisposeBuffers(buffers);

            return lineIds;
        }

        // Execute KernelG (CSPathFromLine) from the compute shader
        private Color[] GetPathFromLine(ComputeShader compute, int buffSize, float[] sdfs , Vector2[] normals, int[] pathIds)
        {
            int kernel = compute.FindKernel(CSProps.KernelG.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            compute.SetInt(CSProps.pixCount, pathIds.Length);

            ComputeBuffer[] buffers = new ComputeBuffer[]
            {
                new(buffSize, sizeof(float)),           // 1- sdf
                new(buffSize, sizeof(float) * 2),       // 2- normals
                new(pathIds.Length, sizeof(int)),       // 3- path ids
                new(buffSize, sizeof(float) * 4),       // 4- out colors
            };

            buffers[0].SetData(sdfs);
            buffers[1].SetData(normals);
            buffers[2].SetData(pathIds);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelG.buffers, buffers);

            compute.Dispatch(kernel, warpCount, 1, 1);

            Color[] colors = new Color[buffSize];
            buffers[3].GetData(colors);

            ComputeUtils.DisposeBuffers(buffers);

            return colors;
        }

        // Execute KernelH (CSMergePath) from the compute shader
        private Color[] GetMergedPath(ComputeShader compute, int buffSize, Color[] toMerge, Color[] netPath)
        {
            int kernel = compute.FindKernel(CSProps.KernelH.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            compute.SetFloat(CSProps.smoothPath, smoothPathUnion);

            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new(buffSize, sizeof(float) * 4),       // in current path
                new(buffSize, sizeof(float) * 4),       // out network path
            };

            buffers[0].SetData(toMerge);
            buffers[1].SetData(netPath);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelH.buffers, buffers);
            compute.Dispatch(kernel, warpCount, 1, 1);

            buffers[1].GetData(netPath);
            ComputeUtils.DisposeBuffers(buffers);

            return netPath;
        }

        // Execute KernelI (CSBlurNetPath) from the compute shader -- DO NOT USE FOR PARTICULES !!
        private Color[] GetBlurredPath(ComputeShader compute, int buffSize, Color[] netPath)
        {
            int kernel = compute.FindKernel(CSProps.KernelI.name);
            int warpCount = ComputeUtils.Get1DWarpCount(compute, kernel, buffSize);

            compute.SetInt(CSProps.blurIter, blurPaths);

            ComputeBuffer[] buffers = new ComputeBuffer[]{
                new(buffSize, sizeof(float) * 4),       // in current path
                new(buffSize, sizeof(float) * 4),       // out network path
            };

            buffers[0].SetData(netPath);

            ComputeUtils.SendBuffers(compute, kernel, CSProps.KernelI.buffers, buffers);
            compute.Dispatch(kernel, warpCount, 1, 1);

            buffers[1].GetData(netPath);
            ComputeUtils.DisposeBuffers(buffers);

            return netPath;
        }

        //----------------------------------------------------------------------- Struct
        private struct OutlinesResults
        {
            public float[] sign;
            public float[] outlineIDs;
        }

        //----------------------------------------------------------------------- Ordering
        public static Vector2 Rotate2dVec(Vector2 vector, float radians)
        {
            return new Vector2( vector.x * Mathf.Cos(radians) + vector.y * -Mathf.Sin(radians),
                                vector.x * Mathf.Sin(radians) + vector.y * Mathf.Cos(radians));
        }

        private Vector2 IdToUV(int pathId, int sdfId)
        {
            Vector2Int pix = new()
            {
                x = pathId % targetResolution.x,
                y = pathId / targetResolution.x % targetResolution.y
            };

            Vector2 pos = new((pix.x + 0.5f) / targetResolution.x, (pix.y + 0.5f) / targetResolution.y); // uv
            pos = pos * 2 - Vector2.one; // centered for rotation (ndc)
            pos = Rotate2dVec(pos, readAngles[sdfId]);

            return pos; // return ndc instead of uv
        }

        private int FilterClosestId(ref List<int>pathIds, int cId, int sdfId)
        {
            Vector2 cUV = IdToUV(cId, sdfId);
            float cDist = 1e20f;
            int index = 0;

            for(int i = 0; i<pathIds.Count; i++)
            {
                Vector2 nUV = IdToUV(pathIds[i], sdfId);
                float nDist = Vector2.SqrMagnitude(cUV - nUV);

                if(nDist < cDist){
                    index = i;
                    cDist = nDist;
                }
            }

            int closest = pathIds[index];
            pathIds.RemoveAt(index);

            return closest;
        }

        // Convex hull algo ??
        private int[] ReorderPathIds(float[] fIds, int idSdf = 0)
        {
            List<int> pathIds = fIds.ToList().ConvertAll(f => (int)f);

            int index = 0; // could be any point

            List<int> ordered = new(){pathIds[index]};
            Vector2 curUV = IdToUV(pathIds[index], idSdf);
            
            // filter starting point, could be in FilterClosestId method !
            for(int i = 0; i < pathIds.Count; i++)
            {
                Vector2 nUV = IdToUV(pathIds[i], idSdf);
                
                if(nUV.x < curUV.x) // order from left to right
                {
                    ordered[0] = pathIds[i];
                    curUV = nUV;
                    index = i;   
                }
            }
            pathIds.RemoveAt(index);

            while(pathIds.Count > 0) ordered.Add(FilterClosestId(ref pathIds, ordered[^1], idSdf));
            
            return ordered.ToArray();
        }

        //----------------------------------------------------------------------- Error Handlers
        // false if not setup properly
        private bool LoadCompute(out ComputeShader compute, bool isSdfGen = true)
        {
            compute = null;

            if(textures.Count == 0){
                string msg = "you must provide textures to be converted\nset at least one element from the textures list\nABORT";
                EditorUtility.DisplayDialog ("No Texture found", msg, "Ok");
                return false;
            }

            if(channelMode == ChannelMode.AlphaThresh && isSdfGen)
            {
                for(int t = 0; t < textures.Count; t++)
                if(!UnityEngine.Experimental.Rendering.GraphicsFormatUtility.HasAlphaChannel(textures[t].format)){
                    string msg = "all textures must have an alpha channel in order to use alpha mode\nABORT";
                    EditorUtility.DisplayDialog ("Texture element : " + t.ToString() + "has no alpha", msg, "Ok");
                    return false;
                }
            }

            compute = Instantiate(AssetDatabase.LoadAssetAtPath(computePath, typeof(ComputeShader))) as ComputeShader;

            if(compute == null){
                Debug.LogWarning("compute shader texToSDF not found ! make sur you have the right asset path for field computePath");
                return false;
            }

            if(readAngles == null) readAngles = new(new float[textures.Count]); // fill with 0
            else if(readAngles.Count != textures.Count)readAngles = new(new float[textures.Count]);

            return true;
        }

        // Exit if no shape is found
        private bool ExitNoShape(ComputeShader compute, OutlinesResults outlines, int slice = 0)
        {
            if(outlines.outlineIDs.Length == 0)
            {
                DestroyImmediate(compute);

                string msg = "Check that the generation mode fits your image!\nthis could come from a unappropriated threshold\nABORT";
                EditorUtility.DisplayDialog ("Texture element : " + slice.ToString() + " has no shape", msg, "Ok");
                return true;
                // Debug.Log("no shape found ! Check that the generation mode fits your image! ");
            }
            return false;
        }

    }
}
#endif


//____________________________________________________________________ FOOTNOTES
// //----------------------------------------------------------------------- Export (Depreciated)
