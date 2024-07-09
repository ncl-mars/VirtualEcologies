/* TODO
    #if SHAPE_TEXTURE_BUFFER
    private Texture2D shapesBuffer; // Todo if limited buffer amount (depends on graphic apis)
    #endif
*/
#if UNITY_EDITOR
// #define TEXTURE_BUFFER

using System;
using System.Collections.Generic;

using UnityEngine;
using Custom;

namespace Custom.Compositor
{
    //////////////////////////////////////////////////////////////////////////////
    public enum SdfTextureType
    {
        Sdf         = 0,
        Color       = 1,
        Outline     = 2,
    }

    [Serializable]
    public class SdfTexture
    {
        public SdfTexture(){}
        public SdfTexture(SdfTextureType t){ type = t; }
        public SdfTexture(SdfTextureType t, Texture tex){ type = t; texture = tex;}

        public SdfTextureType type;
        public Texture texture;
    }

    //////////////////////////////////////////////////////////////////////////////
    [ExecuteInEditMode]
    public class SdfCompositor : MonoBehaviour
    {
        private ComputeBuffer shapesBuffer;

        [HideInInspector][SerializeField] private SdfTexture[] texturesBuffer = new SdfTexture[2]{
            new(){type = SdfTextureType.Sdf},
            new(){type = SdfTextureType.Color},
        };

        public SdfTexture[] TexturesBuffer{get{return texturesBuffer;}}

        //////////////////////////////////////////////////////////////////////////////
        // Get / Set, make properties ?
        public SdfTexture GetBufferTexture(SdfTextureType type)
        {
            for(int t = 0; t < texturesBuffer.Length; t++) if(texturesBuffer[t].type == type) return texturesBuffer[t];
            return null;   
        }

        // public void SetBufferTexture(SdfTexture sdfTex)
        // {
        //     if(sdfTex.texture == null)return;

        //     for(int t = 0; t < texturesBuffer.Length; t++)
        //     {
        //         if(texturesBuffer[t].type == sdfTex.type)
        //         {
        //             texturesBuffer[t] = sdfTex;
        //             // SetPropertyBlock();
        //         }
        //     }
        // }

        private void SetPropertyBlock()
        {
            if(SdfTools.TryGetFirstInChilren(this, out Renderer rend))
            {
                MaterialPropertyBlock props = new();
                
                props.SetBuffer ("_Shapes", shapesBuffer);
                props.SetInt    ("_NumShapes", shapesBuffer.count);

                props.SetTexture("_SdfArray", texturesBuffer[0].texture);
                props.SetTexture("_ColArray", texturesBuffer[1].texture);

                rend.SetPropertyBlock(props);
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        // reactivate non-sdfelement (container, renderer) to create buffer, 
        // hold states to reset to once created
        public void Init()
        {
            SdfCollection col = GetComponentInChildren<SdfCollection>(true);
            bool stateCol = col.gameObject.activeSelf;
            col.gameObject.SetActive(true);

            CreateBufferFromChildren();

            col.gameObject.SetActive(stateCol);
        }

        private void Start()
        {
            if(Application.isPlaying){
                enabled = false;
                Init();
                if(SdfTools.TryGetFirstInChilren(this, out SdfCollection col, true))Destroy(col.gameObject);
            }
        }

        private void OnEnable() { if(!Application.isPlaying) Init(); }
        private void OnDisable(){ if(!Application.isPlaying) DisposeBuffer(); }
        private void OnDestroy(){ DisposeBuffer();}

        //////////////////////////////////////////////////////////////////////////////
        // called from children
        public void OnElementChanged(SdfElement element)
        {
            // Debug.Log("On Element changed");

            if(NeedsNewBuffer(out var elements))CreateBufferFromChildren(elements);
            else{
                int id = element.transform.GetSiblingIndex();
                element.WriteOnBuffer(ref shapesBuffer, id);
            }
        }

        public void OnCollectionChanged(SdfTexture sdfTex)
        {
            if (sdfTex.texture == null){
                Debug.LogWarning("Error trying to set a null texture, please set a supported texture and click on 'Recreate All Texture' from the contextual menu");
                return;
            }

            if      (sdfTex.type == SdfTextureType.Sdf) texturesBuffer[0] = sdfTex;
            else if (sdfTex.type == SdfTextureType.Color) texturesBuffer[1] = sdfTex;

            SetPropertyBlock();

            Debug.LogFormat("New TextureArray of type <{0}> loaded in compositor\n fomat : {1}, depths : {2}, width : {3}, height : {4}",
            sdfTex.type, ((Texture2DArray)sdfTex.texture).format, ((Texture2DArray)sdfTex.texture).depth, sdfTex.texture.width, sdfTex.texture.height);
        }

        public void OnHierarchyChanged()
        {
            // Debug.Log("hierachy change");

            SdfElement[] elements = GetComponentsInChildren<SdfElement>();
            bool recreateBuffer = false;

            if(shapesBuffer == null) recreateBuffer = true;
            else
            {
                if(elements.Length != shapesBuffer.count) recreateBuffer = true;
                else // check for reordered element in hierarchy
                {
                    for(int e = 0; e < elements.Length; e++)
                    {
                        string[] split = elements[e].name.Split("_");
                        if(split.Length > 1)
                        {
                            if(int.Parse(split[1]) != elements[e].transform.GetSiblingIndex())
                            {
                                recreateBuffer = true;
                                break;
                            }
                        }
                    }
                }
            }

            if(recreateBuffer) CreateBufferFromChildren(elements);
        }

        //////////////////////////////////////////////////////////////////////////////
        private bool NeedsNewBuffer(out SdfElement[] elements)
        {
            elements = GetComponentsInChildren<SdfElement>();
            if(shapesBuffer == null) return true;

            else{
                if(elements.Length != shapesBuffer.count) return true;
                else return false;
            }
        }

        private void CreateBufferFromChildren(SdfElement[] elements = null)
        {
            DisposeBuffer();
            elements??= GetComponentsInChildren<SdfElement>();

            if(elements.Length > 0)
            {
                shapesBuffer = new ComputeBuffer(elements.Length, elements[0].GetDataSize(), ComputeBufferType.Constant);

                for(int i = 0; i < elements.Length; i++)
                {
                    elements[i].transform.SetSiblingIndex(i);
                    elements[i].name = "SdElement_" + i.ToString(); // keep track of hierachy id in name
                    elements[i].WriteOnBuffer(ref shapesBuffer, i);
                    // shapesBuffer.SetData(elements[i].GetData(), 0, i, 1);
                }
                SetPropertyBlock();
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        [ContextMenu ("Recreate All Textures Arrays")]
        private void RecreateTextureArrays()
        {
            if(SdfTools.TryGetFirstInChilren(this, out SdfCollection col, true))
            {
                texturesBuffer = col.CreateNewRegisterBuild(this);
            }
            SetPropertyBlock();
        }
        
        private void OnValidate()
        {
            if( texturesBuffer[0].type != SdfTextureType.Sdf || texturesBuffer[1].type != SdfTextureType.Color){
                texturesBuffer[0].type = SdfTextureType.Sdf;
                texturesBuffer[1].type = SdfTextureType.Color;
                TextureStructException();
                return;
            }

            else if (texturesBuffer.Length > 2 )
            {
                List<SdfTexture> tex = new(texturesBuffer);
                tex.RemoveAt(2);
                texturesBuffer = tex.ToArray();
                TextureStructException();
                return;
            }
        }

        private void TextureStructException()
        {
            Debug.LogWarning("Texture Structures depends of the compositor's material, changing them at element's level is not allowed");
        }

        //////////////////////////////////////////////////////////////////////////////
        private void DisposeBuffer()
        {
            if(shapesBuffer != null){
                shapesBuffer.Release();
                shapesBuffer.Dispose();
            }
        }

    }
}
#endif