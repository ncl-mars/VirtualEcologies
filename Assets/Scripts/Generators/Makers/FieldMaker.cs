/*
    + ModalUI field attribute store a MapType Enum
    Set the attribute for the field to be shown is module flag
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

using Custom.Generators.Modules;
using System.Data;

namespace Custom.Generators.Makers
{
    //////////////////////////////////////////////////////////////////////////
    [CreateAssetMenu(fileName = "FieldMaker", menuName = "ScriptableObjects/Generators/FieldMaker", order = 1)]
    public class FieldMaker : Maker, IGenerator
    {
        //----------------------------------------------------------------------- Types
        [Serializable] public class ColorSource 
        {
            public Texture2D texture;
            public ReadRotation readRotation;
        }

        [Serializable] public class HeightSource 
        {
            public Texture2D texture;
            public bool isClamped = true;
        }

        [Serializable] public class InputData
        {
            [Flag(6)] public List<ColorSource> colorSources;
            [Flag(20)] public List<HeightSource> heightSources;

            public List<Texture2D> Colors
            {
                get=>colorSources.Select(cs => cs.texture).ToList();
            }
        }

        [Serializable] public class GenerationParams
        {
            public MapType mapType; // property name used in the editor classe!
            public Vector2Int targetResolution = new(512,512);

            [Flag(6)] public ChannelMode channelMode = ChannelMode.AlphaThresh;

            [Flag(6)] [Range(0.0f,1.0f)] public float thresh = 0;
            [Flag(6)] [Range(0,20)] public uint blurDistances = 0;
            [Flag(2)] public bool keepFieldExact = true;

            [Flag(4)] [Range(0.0f,0.1f)] public float smoothPathUnion = 0.005f;
            [Flag(4)] [Range(0,20)] public int blurPaths = 2;
        }

        [Serializable] public class ExportParams
        {
            public bool halfFloat = false;
            public FilterMode outFilterSetup = FilterMode.Point;
            [HideInInspector] public int linkId = -1;
        }

        //----------------------------------------------------------------------- 
        [SerializeField] private InputData inputs;
        [SerializeField] private GenerationParams generation;
        [SerializeField] private ExportParams export;

        public delegate void OnGeneratedTextureEvent(int id, Texture2D texture);
        public event OnGeneratedTextureEvent OnGeneratedTexture;

        //-----------------------------------------------------------------------
        private bool HalfFloat
        {
            get
            {
                if(LinkedToCollection) return false;
                else return export.halfFloat;
            }
        }

        public Vector2Int TargetResolution
        {
            get => generation.targetResolution;
            set
            {
                generation.targetResolution = value;
            }
        }

        public bool LinkedToCollection{ get=>export.linkId > -0.5f;}

        public int CollectionLinkID 
        { 
            set => export.linkId = value;
        }
        
        public MapType MapType
        {
            get => generation.mapType; 
            set 
            { 
                generation.mapType = value;
                if(MapType != MapType.Plateform) return;
                inputs.colorSources ??= new(){new(){texture = Texture2D.blackTexture}};
                if(inputs.colorSources.Count < 1) inputs.colorSources.Add(new(){texture = Texture2D.blackTexture});
            }
        }
        
        public Texture Generated // (re)generate the texture on property call
        {
            get
            {
                Texture generated;
                switch(generation.mapType)
                {
                    case MapType.Plateform     : GeneratePlateformField(out generated, false); break;
                    case MapType.HighPath      : GeneratePathField(out generated, false); break;
                    case MapType.Topography    : GenerateTopoGrav(out generated, false); break;
                    default : generated = Texture2D.blackTexture; break;
                }
                return generated;
            }
        }
        
        //----------------------------------------------------------------------- Events
        private void OnValidate()
        {
            if(inputs.heightSources != null)
            {
                if(inputs.heightSources.Count > 1){
                    inputs.heightSources.RemoveAt(1);
                    Debug.LogWarning("Only one height map supported for this mode");
                }
            }

            // if(LinkedToCollection && (MapType == MapType.Plateform))
            // {
                // generation.mapType = MapType.TopoGravity;
                // Debug.LogWarning("This mode is not compatible when linked to a collection\nABORT");
            // }
        }

        public void Generate()
        {
            // Debug.Log("Generating from field maker");
            if(LinkedToCollection && (OnGeneratedTexture == null))
            {
                Debug.LogWarning("Linked to a collection, but has No Listener\nPlease Resync From The collection maker\nAborting Generation");
                return;
            }

            switch(generation.mapType)
            {
                case MapType.Plateform      : GeneratePlateformField(out _); break;
                case MapType.HighPath       : GeneratePathField(out _); break;
                case MapType.Topography     : GenerateTopoGrav(out _); break;
                default : break;
            }
        }

        //----------------------------------------------------------------------- Generations
        private bool GeneratePlateformField(out Texture texture, bool ouputTexture = true)
        {
            texture = null;
            SdfGenerator gen = new()
            {
                TargetResolution = generation.targetResolution,
                Textures = new(){inputs.Colors[0]},
                ChannelMode = generation.channelMode,
                Thresh = generation.thresh,
                BlurIterations = generation.blurDistances,
                KeepFieldExact = generation.keepFieldExact,
                ComputeNormals = true,
                CompressNorDt = true
            };

            if(!gen.GenerateSdf(out float[][] _ , out Vector2[][] normals)) return ExitGeneration("Could not compute Plateform field", gen);
            
            DepthGenerator depthgen = new()
            {
                TargetResolution = generation.targetResolution,
                DepthClamp = false,
            };

            if(!depthgen.GeneratePlaterformDepth(normals[0], out Color[] plateformDepth)) return ExitGeneration("could not generate elevation of path", depthgen);


            texture = TexUtils.CreateTex2D(
                plateformDepth, 
                generation.targetResolution.x, generation.targetResolution.y, 
                generation.mapType.ToTextureFormat(HalfFloat), 
                FilterMode.Point
            );
            
            // texture = TexUtils.CreateTex2D(
            //     normals[0], 
            //     generation.targetResolution.x, generation.targetResolution.y, 
            //     HalfFloat, 
            //     export.outFilterSetup
            // );

            if(ouputTexture) OutputTexture(texture, GetPathFromObject(inputs.colorSources[0].texture, "_Plateform"));
            
            DestroyImmediate(gen);
            return true;
        }

        private bool GeneratePathField(out Texture texture, bool ouputTexture = true)
        {
            texture = null;
            //------------------------------ generate 2d path datas
            SdfGenerator pathGen = new()
            {
                TargetResolution = generation.targetResolution,
                Textures = inputs.Colors,
                ChannelMode = generation.channelMode,
                Thresh = generation.thresh,
                BlurIterations = generation.blurDistances,
                KeepFieldExact = true,
                ComputeNormals = true,
                BlurPaths = generation.blurPaths,
                ReadAngles = GetReadAngles(),
                CompressNorDt = false
            };
            
            if(!pathGen.GenerateSdf(out float[][] sdfs , out Vector2[][] normals))  return ExitGeneration("could not compute sdfs datas", pathGen);
            if(!pathGen.GeneratePathFromSdf(sdfs, normals, out Color[] netPath))    return ExitGeneration("could not Generate Path From Sdf", pathGen);
            
            DestroyImmediate(pathGen);

            // ------------------------------ generate elevations from path
            Texture2D depthTex = GetTexHeightZero(out bool isClamped);

            DepthGenerator depthgen = new()
            {
                TargetResolution = generation.targetResolution,
                DepthTextures = new List<Texture2D>(){depthTex},
                DepthClamp = isClamped,
            };

            if(!depthgen.GeneratePathDepth(netPath, out Color[] elevatedPath)) return ExitGeneration("could not generate elevation of path", depthgen);

            texture = TexUtils.CreateTex2D(
                elevatedPath, 
                generation.targetResolution.x, generation.targetResolution.y, 
                generation.mapType.ToTextureFormat(HalfFloat), 
                FilterMode.Point
            );
            
            if(ouputTexture)OutputTexture(texture, GetPathFromObject(inputs.colorSources[0].texture, "_Path"));

            return true; 
        }

        private bool GenerateTopoGrav(out Texture texture, bool ouputTexture = true)
        {
            texture = null;

            HeightSource heightSource = inputs.heightSources[0];

            DepthGenerator gen = new()
            {
                TargetResolution = generation.targetResolution,
                DepthTextures = new List<Texture2D>(){heightSource.texture},
                DepthClamp = heightSource.isClamped,
            };

            if(!gen.GenerateNormalsDepth(out Color[][] colors)) return ExitGeneration("could not compute Topography Map", gen);
            
            texture = TexUtils.CreateTex2D(
                colors[0], 
                generation.targetResolution.x, generation.targetResolution.y, 
                generation.mapType.ToTextureFormat(HalfFloat),
                FilterMode.Point
            );


            // Texture2D tex = new Texture2D( generation.targetResolution.x, generation.targetResolution.y, generation.mapType.ToTextureFormat(HalfFloat), true)
            // {
            //     filterMode = FilterMode.Point
            // };
            // tex.SetPixels(colors[0], 0);
            // tex.Apply(true);
            // texture = tex;
            
            if(ouputTexture) OutputTexture(texture, GetPathFromObject(heightSource.texture, "_Topo"));
            
            DestroyImmediate(gen);
            return true;
        }

        private void OutputTexture(Texture generatedTexture, string path = "")
        {
            if(LinkedToCollection)
            {
                OnGeneratedTexture?.DynamicInvoke(export.linkId, generatedTexture);
            }
            else
            {
                TexUtils.SaveTextureAsset(generatedTexture, path);
            }
        }

        //----------------------------------------------------------------------- Utils
        private List<float> GetReadAngles()
        {
            List<float> readAngles = new();
            for(int d = 0; d < inputs.colorSources.Count; d++)readAngles.Add((int)inputs.colorSources[d].readRotation * MathF.PI / 2.0f);
            return readAngles;
        }

        private Texture2D GetTexHeightZero(out bool isClamped)
        {
            Texture2D tex = Texture2D.blackTexture; 
            isClamped = false;

            var heightSources = inputs.heightSources;

            if(heightSources == null) return tex;
            else if(heightSources.Count < 1) return tex;
            else if(heightSources[0] == null) return tex; 
            else{
                tex = heightSources[0].texture;
                isClamped = heightSources[0].isClamped;
                return tex;
            }
        }

        private bool ExitGeneration(string log, UnityEngine.Object module)
        {
            Debug.LogWarning(log);
            DestroyImmediate(module);
            return false;
        }

        //----------------------------------------------------------------------- Context Menu
        [ContextMenu("Log Enum")]
        private void LogEnumFlags()
        {
            string sb = "--- MapType Enum ---";
            for(int val = 0; val <= 64; val++ ) sb += "\n"+ string.Format("{0,3} - {1:G}", val, (MapType)val);
            Debug.Log(sb);
        }

    }
}
#endif