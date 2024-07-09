/*
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Custom.Generators.Makers
{
    using Modules;

    //////////////////////////////////////////////////////////////////////////
    [CreateAssetMenu(fileName = "NoiseMaker", menuName = "ScriptableObjects/Generators/NoiseMaker", order = 1)]
    public class NoiseMaker : Maker, IGenerator
    {
        [Serializable] public class GenerationParams
        {
            public NoiseTypes noiseType = NoiseTypes.Perlin;

            public Vector3Int targetResolution = new(512,512, 1);
            [Range(0.0f,100f)] public float scale = 10;
            public bool normalized = true;
        }

        [Serializable] public class ExportParams
        {
            public bool noiseToAlpha = false;
            public bool halfFloat = false;
            public FilterMode outFilterSetup = FilterMode.Point;
        }

        [SerializeField] private GenerationParams generation;
        [SerializeField] private ExportParams export;

        public NoiseTypes NoiseType{get=>generation.noiseType; set=>generation.noiseType = value;}

        public void Generate()
        {
            NoiseGenerator gen = new()
            {
                NoiseType = generation.noiseType,
                TargetResolution = generation.targetResolution,
                Normalized = generation.normalized,
                Scale = generation.scale,
                NoiseToAlpha = export.noiseToAlpha
            };

            NoiseTypes nt = generation.noiseType;
            bool isColor = 
                (nt==NoiseTypes.Fractal_Gradients) || 
                (nt==NoiseTypes.FBM_Derivatives);

            bool isFloat = 
                (nt==NoiseTypes.Perlin) || 
                (nt==NoiseTypes.Fractal)||
                (nt==NoiseTypes.FBM);

            if(isColor){
                if(gen.GenerateColor(out Color[][] results)) Export(results, generation.noiseType.ToString());
            }
            else if (isFloat){
                if(gen.GenerateFloat(out float[][] results)) Export(results, generation.noiseType.ToString());
            }
        }

        private void Export<T>(T[][] results, string suffix= "") where T : struct
        {        
            Texture tex = default;
            Vector3Int targetResolution = generation.targetResolution;

            if(targetResolution.z < 2)
            {
                if(typeof(T) == typeof(float)) 
                {
                    tex = TexUtils.CreateTex2D(
                        results[0] as float[], 
                        targetResolution.x, targetResolution.y, 
                        export.halfFloat, export.outFilterSetup
                    );
                }
                else if(typeof(T) == typeof(Color)) 
                {
                    TextureFormat format = export.halfFloat ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat; 

                    tex = TexUtils.CreateTex2D(
                        results[0] as Color[], 
                        targetResolution.x, targetResolution.y,
                        format,
                        export.outFilterSetup
                    );
                }
            }
            else
            {
                if(typeof(T) == typeof(float))
                {
                    tex = TexUtils.CreateTex3D(
                        results as float[][], 
                        targetResolution.x, targetResolution.y, 
                        export.halfFloat, export.outFilterSetup
                    );
                }

                else if(typeof(T) == typeof(Color)) 
                {
                    TextureFormat format = export.halfFloat ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat;

                    Debug.LogError("Texture 3D creator needed here, \nABORT");

                    // tex = TexUtils.CreateTex3D(
                    //     results[0] as Color[], 
                    //     targetResolution.x, targetResolution.y,
                    //     format
                    //     // export.halfFloat, export.outFilterSetup
                    // );
                }
            } 
            string path = GetPathFromObject(this, "_" + suffix);
            TexUtils.SaveTextureAsset(tex, path);
        }

        [ContextMenu("Log Noises Types Enum")]
        private void LogNoisesEnum()
        {
            string sb = "--- Noises Enum ---";
            for(int val = 0; val <= 64; val++ ) sb += "\n"+ string.Format("{0,3} - {1:G}", val, (NoiseTypes)val);
            Debug.Log(sb);
        }
    }
}
#endif





// if(generation.noiseType == NoiseTypes.Perlin)
// {
//     if(gen.GeneratePerlin(out float[][] results)) Export(results, generation.noiseType.ToString());
// }
// else if(generation.noiseType == NoiseTypes.Fractal)
// {
//     if(gen.GenerateFractal(out float[][] results)) Export(results, generation.noiseType.ToString());
// }
// else if(generation.noiseType == NoiseTypes.Fractal_Gradients)
// {
//     if(gen.GenerateFractalGradient(out Color[][] results)) Export(results, generation.noiseType.ToString());
// }

// else if(generation.noiseType == NoiseTypes.FBM)
// {
//     // if(gen.GenerateFractalGradient(out Color[][] results)) Export(results, generation.noiseType.ToString());
// }
// else if(generation.noiseType == NoiseTypes.FBM_Derivatives)
// {
//     // if(gen.GenerateFractalGradient(out Color[][] results)) Export(results, generation.noiseType.ToString());
// }