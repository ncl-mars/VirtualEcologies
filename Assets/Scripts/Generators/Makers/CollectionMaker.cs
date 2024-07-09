/*
    https://docs.unity3d.com/ScriptReference/Texture2D.PackTextures.html

*/
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

using System;

using Custom.Generators.Modules;
namespace Custom.Generators.Makers
{
    public enum LinkMode : byte
    {
        Unlinked = 0,
        Linked = 1,
    }

    [ExecuteInEditMode]
    [CreateAssetMenu(fileName = "CollectionMaker", menuName = "ScriptableObjects/Generators/CollectionMaker", order = 3)]
    public class CollectionMaker : Maker, IGenerator
    {
        [Serializable] public class InputData
        {
            public LinkMode linkMode = LinkMode.Unlinked;

            [Flag(0)] public MapType mapType = MapType.Color;
            [Flag(0)] public List<Texture2D> textures;
            [Flag(1)] public List<FieldMaker> makers;

            public Texture2D this [int id]{ get => textures[id];}
            public int Count { get => textures.Count;}

            public bool HasMakers
            {
                get
                {
                    if(makers != null)
                    {
                        if(makers.Count > 0)
                        {
                            if(makers[0] != null) return true;
                            else return false;
                        }
                        else return false;
                    }
                    else return false;
                }
            }
        }

        [Serializable] public class GenerationParams
        {
            public Vector2Int targetResolution = new(32,32);
        }

        [Serializable] public class ExportParams
        {
            [Flag(0)] public FilterMode filter = FilterMode.Point;

            // public ExportType type;
            public bool halfFloat;
        }

        [SerializeField] private InputData inputs = new();
        [SerializeField] private GenerationParams generation = new();
        [SerializeField] private ExportParams export = new();

        //-----------------------------------------------------------------------
        public MapType MapType
        {
            get => inputs.mapType;
            set
            {
                inputs.mapType = value;
                if(Linked) foreach(FieldMaker m in inputs.makers) m.MapType = value;
            }
        }

        public bool Linked { get => inputs.linkMode != LinkMode.Unlinked;}

        public LinkMode Link
        {
            get => inputs.linkMode;
            set
            {
                inputs.linkMode = value;
                if(Linked) OnLink();
                else OnUnlink();
            }
        }

        //-----------------------------------------------------------------------
        public void OnEnable()
        {
            if(Linked) OnLink();
        }

        public void OnDisable()
        {
            if(Linked) OnUnlink();
        }

        public void OnValidate()
        {
            if(Linked)
            {
                if(inputs.HasMakers)
                {
                    if(generation.targetResolution != inputs.makers[0].TargetResolution)
                    {
                        // foreach(FieldMaker m in inputs.makers)
                        for(int i = 0; i < inputs.makers.Count; i++)
                        {
                            if(inputs.makers[i] != null)
                            {
                                inputs.makers[i].TargetResolution = generation.targetResolution;
                                inputs.makers[i].MapType = MapType;
                                inputs.makers[i].CollectionLinkID = i;
                            }
                        }
                        return;
                    }
                }
            }
            // if(export.type == ExportType.Atlas)
            // {
            //     export.type = ExportType.Texture2D;
            //     Debug.LogWarning("Format not yet supported (for this maker)");
            //     return;
            // }
        }

        //-----------------------------------------------------------------------
        private void OnLink()
        {
            inputs.mapType = inputs.mapType == MapType.Color ? MapType.Topography : inputs.mapType;
            inputs.textures = new();

            for(int m = 0; m < inputs.makers.Count; m++) 
            {
                inputs.makers[m].CollectionLinkID = m;
                inputs.makers[m].MapType = inputs.mapType;
                inputs.makers[m].TargetResolution = generation.targetResolution;
                inputs.makers[m].OnGeneratedTexture += OnTextureGenerated;
            }
        }

        private void OnUnlink()
        {
            for(int m = 0; m < inputs.makers.Count; m++) 
            {
                inputs.makers[m].CollectionLinkID = -1;
                inputs.makers[m].OnGeneratedTexture -= OnTextureGenerated;
            }
        }

        private bool HasMakerSources()
        {
            if(inputs.textures == null) return false;
            if(inputs.textures.Count < 1) return false;
            return inputs.textures[0] != null;
        }

        public void OnTextureGenerated(int id, Texture2D generatedTexture)
        {
            if(HasMakerSources()) inputs.textures[id] = generatedTexture;
            else
            {
                inputs.textures = new();

                for(int m = 0; m < inputs.makers.Count; m++) 
                {
                    if(m == id) inputs.textures.Add(generatedTexture);
                    else
                    {
                        inputs.textures.Add((Texture2D)inputs.makers[m].Generated);
                    }
                }
            }
            Generate();
        }

        //-----------------------------------------------------------------------
        public void Generate()
        {
            SdfGenerator gen = new()
            {
                TargetResolution = generation.targetResolution,
                Textures = inputs.textures,
            };

            if(gen.ConvertList(out Color[][]colors))
                ExportTexture(colors);

            DestroyImmediate(gen);
        }

        public virtual void ExportTexture(Color[][] colors)
        {

            Texture2DArray tex = TexUtils.CreateTexArray(
                colors, 
                generation.targetResolution.x, generation.targetResolution.y, 
                inputs.mapType.ToTextureFormat(export.halfFloat), 
                filter : export.filter
            );

            string suffix = inputs.mapType switch
            {
                _ => "_" + MapType.ToString() + "_2DA",
            };

            TexUtils.SaveTextureAsset(tex,GetPathFromObject( this, suffix));
        }
    }
}
#endif




//__________________________________________________________________________________________
// [MenuItem("Assets/Create/Generators/SpriteCollectionMaker/Delete All Instances")]
// private static void DeleteAllInstances()
// {
//     string[] makers = AssetDatabase.FindAssets("t:" + typeof(SpriteCollectionMaker).Name );
//     for(int m = 0; m < makers.Length; m++) AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(makers[m]));
//     Debug.LogFormat("{0} Instances Of SpriteCollectionMaker Deleted" , makers.Length);
// }