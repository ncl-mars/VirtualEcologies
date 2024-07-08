using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

using Custom.Generators.Modules;

namespace Custom.Generators.Makers
{
    [CreateAssetMenu(fileName = "SdfMaker", menuName = "ScriptableObjects/Generators/SdfMaker", order = 2)]
    public class SdfMaker : TextureMaker, IGenerator
    {
        [Serializable]public class GenerationParams
        {
            public Vector2Int targetResolution = new(512,512);
            [Tooltip("Channel used to isolate shape (> tresh)")]
            public ChannelMode channelMode = ChannelMode.AlphaThresh;
            [Tooltip("float in domain [0,1]\nUse ((8bit)value/255) in field if easier")][Range(0.0f,1.0f)]
            public float edgeDetectionThresh = 0;
            [Tooltip("blur distances by averaging neighboors\nWARNING : this makes the field unexact UNLESS you toggle \"keep field exact\", which recompute the sdf once blurred\nset it to 0 to bypass and save at max 3 passes of compute")]
            [Range(0,20)] public uint blurIterations = 0;
            [Tooltip("this implies another 2 passes of compute\nblurred distances will be recalculated to be exact")]
            public bool keepFieldExact = false;
            [Tooltip("this will compute and export the normals in a second texture")]
            public bool computeNormals = false;
        }

        [Serializable] public class InputData
        {
            public List<Texture2D> textures;

            public Texture2D this [int id]{ get => textures[id];}
            public int Count { get => textures.Count;}
        }

        [Serializable] public class ExportParams
        {   
            [Tooltip("Save SDF Textures with Half Float Precision")]
            public bool halfFloat   = false;
            [Tooltip("this will encode the distance in the length of the normal vector")]
            public bool encodeDistances = false;
            [Tooltip("Texture2DArray and Texture3D are only available if you set more that one texture to convert\nTexture3D may cause array overflow if many large textures, use 2DArray in that case")]
            public ExportType format = ExportType.Texture2D;
            [Tooltip("Output filter mode, prefer point for maps!\nyou can still change that aftewards from the inspector")]
            public FilterMode filter = FilterMode.Bilinear;
        }

        [SerializeField] private GenerationParams generation;
        [SerializeField] private InputData inputs;
        [SerializeField] private ExportParams export;

        public void OnValidate()
        {
            if(export.format == ExportType.Atlas){
                export.format = ExportType.Texture2D;
                Debug.LogWarning("Format not supported for this generator");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        public void Generate()
        {
            SdfGenerator gen = new()
            {
                TargetResolution = generation.targetResolution,
                Textures = inputs.textures,
                ChannelMode = generation.channelMode,
                Thresh = generation.edgeDetectionThresh,
                BlurIterations = generation.blurIterations,
                KeepFieldExact = generation.keepFieldExact,
                
                ComputeNormals = generation.computeNormals,
                CompressNorDt = export.encodeDistances
            };

            if(gen.GenerateSdf(out float[][] distances , out Vector2[][] normals)) Export(distances, normals);
            else Debug.LogWarning("Generation failed, be sure to have the generation params set accordingly to your inputs");
    
            DestroyImmediate(gen);
        }

        private void Export(float[][] sdfs, Vector2[][] normals = null)
        {
            export.format = inputs.Count == 1 ? ExportType.Texture2D : export.format;   // if one tex, export is Tex2D

            int width   = generation.targetResolution.x;
            int height  = generation.targetResolution.y;
            
            bool separateSdf = (generation.computeNormals == false) || (export.encodeDistances == false);
            string norSuffix = export.encodeDistances ? "_NorDt" : "_Nor";

            if(export.format == ExportType.Texture2D)
            {
                for(int t = 0; t < inputs.Count; t++)
                {
                    if(separateSdf){
                        Texture2D sdTex = TexUtils.CreateTex2D(sdfs[t], width, height, export.halfFloat, export.filter);
                        TexUtils.SaveTextureAsset(sdTex, GetPathFromObject(inputs[t], "_Sdf"));
                    }

                    if(generation.computeNormals){
                        Texture2D norTex = TexUtils.CreateTex2D(normals[t], width, height, export.halfFloat, export.filter);
                        TexUtils.SaveTextureAsset(norTex, GetPathFromObject(inputs[t], norSuffix));
                    }
                }
            }

            else if(export.format == ExportType.Texture2DArray)
            {
                if(separateSdf){
                    Texture2DArray sdf2DA = TexUtils.CreateTexArray(sdfs, width, height, export.halfFloat, export.filter);
                    TexUtils.SaveTextureAsset(sdf2DA, GetPathFromObject(inputs[0], "_Sdf"));
                }

                if(generation.computeNormals){
                    Texture2DArray nor2DA = TexUtils.CreateTexArray(normals, width, height, export.halfFloat, export.filter);
                    TexUtils.SaveTextureAsset(nor2DA, GetPathFromObject(inputs[0], norSuffix));
                } 
            }

            else // ExportType.Texture3D
            {
                if(separateSdf){
                    Texture3D sdf3D = TexUtils.CreateTex3D(sdfs, width, height, export.halfFloat, export.filter);
                    TexUtils.SaveTextureAsset(sdf3D, GetPathFromObject(inputs[0], "_Sdf"));
                }

                if(generation.computeNormals){
                    Texture3D nor3D = TexUtils.CreateTex3D(normals, width, height, export.halfFloat, export.filter);
                    TexUtils.SaveTextureAsset(nor3D, GetPathFromObject(inputs[0], norSuffix));
                } 
            }
        }
    }
}




// [MenuItem("Assets/Create/Generators/SdfMaker/Delete All Instances")]
// private static void DeleteAllInstances()
// {
//     string[] makers = AssetDatabase.FindAssets("t:" + typeof(SdfMaker).Name );
//     for(int m = 0; m < makers.Length; m++) AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(makers[m]));
//     Debug.LogFormat("{0} Instances Of SdfMaker Deleted", makers.Length);
// }
