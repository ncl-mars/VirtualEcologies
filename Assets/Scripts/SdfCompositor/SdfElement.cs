/*
    https://docs.unity3d.com/Manual/editor-CustomEditors.html
    
    Helper class for the compositor
    DESTROYED AT RUNTIME

    THIS CLASS IS MEANT TO BE MADE ABSTRACT AND INHERITED

    TODO : inheritage from sd element with virtual methods 
    => for specifics shaders + buffers
*/
#if UNITY_EDITOR
// #define SHOW_IDS // DEBUG

using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using System;

namespace Custom.Compositor
{
    [ExecuteInEditMode]
    public class SdfElement : MonoBehaviour
    {
        private new Renderer renderer; // renderer is child of compositor

        public struct ShapeTRS
        {
            public Matrix4x4 txi;
            public float scale;
            public float depth;
        }

        [Serializable]
        private struct Shape
        {
            public ShapeTRS TRS;

            public int idSdf;
            public int idCol;

            public float off2d;
            public float round;
            public float blend;
        }

        //--
        [SerializeField] private SdfTexture[] textures = new SdfTexture[2]{
            new(){type = SdfTextureType.Sdf},
            new(){type = SdfTextureType.Color},
        };

        [SerializeField][Range(0,1)] private float offset2D = 0;
        [SerializeField][Range(0,1)] private float roundness = 0;
        [SerializeField][Range(0,1)] private float blendStrength = 0;

        // CPU buffer element
        [SerializeField][HideInInspector] private Shape shape;

        // booleans, used to sync OnValidate on main thread
        private bool hasDataChanged = false;
        private bool hasTextureChanged = false;

        public SdfTexture[] Textures{
            get=>textures; 
            set=>textures = value;
        }

    #if SHOW_IDS
        public int idSdfReadOnly;
        public int idColReadOnly;
    #endif

        //////////////////////////////////////////////////////////////////////////////
        public SdfTexture[] GetTextures(out int[] ids)
        {
            ids = new int[]{shape.idSdf, shape.idCol};
            return textures;
        }

        public void SetTextureSliceID(int texId, int sliceId)
        {
            switch(texId)
            {
                case 0:
                    shape.idSdf = sliceId;
                    break;
                
                case 1:
                    shape.idCol = sliceId;
                    break;

                default : break;
            }

            hasDataChanged = true;
            if(!Application.isPlaying && renderer != null)Update();

    #if SHOW_IDS
            idSdfReadOnly = shape.idSdf;
            idColReadOnly = shape.idCol;
    #endif
        }

        public int[] GetTextureSliceIDs()
        {
            return new int[]{shape.idSdf, shape.idCol};
        }

        //////////////////////////////////////////////////////////////////////////////
        protected void TextureStructException()
        {
            Debug.LogWarning("Texture Structures depends of the compositor's material, changing them at element's level is not allowed");
        }

        protected virtual void OnValidate()
        {
            if(textures[0].type != SdfTextureType.Sdf || textures[1].type != SdfTextureType.Color){
                textures[0].type = SdfTextureType.Sdf;
                textures[1].type = SdfTextureType.Color;
                TextureStructException();
                return;
            }

            else if (textures.Length > 2 )
            {
                List<SdfTexture> tex = new(textures);
                tex.RemoveAt(2);
                textures = tex.ToArray();
                TextureStructException();
                return;
            }

            hasDataChanged = true;

            if (shape.round != roundness) shape.round = roundness;
            else if (shape.off2d != offset2D) shape.off2d = offset2D;
            else if (shape.blend != blendStrength) shape.blend = blendStrength;

            else
            {
                hasDataChanged = false;
                hasTextureChanged = true;
            }
        }
        
        protected virtual void Update()
        {
            if(renderer.transform.hasChanged) 
            {
                // get siblings transform and set them as changed in order to get them all updated
                Transform[] transforms = transform.parent.GetComponentsInChildren<Transform>();
                for(int t = 0; t < transforms.Length; t++)transforms[t].hasChanged = true;
                renderer.transform.hasChanged = false;
            }

            if(transform.hasChanged)
            {
                transform.localScale = new Vector3(transform.localScale.y, transform.localScale.y, transform.localScale.z);

                // ApplyTransformToShapeData();
                ApplyTransformToShape(ref shape.TRS);

                transform.hasChanged = false;
                hasDataChanged = true;
            }

            if(hasDataChanged)
            {
                if(SdfTools.TryGetFirstInParents<SdfCompositor>(this, out var comp)) comp.OnElementChanged(this);
                hasDataChanged = false;
            }

            else if(hasTextureChanged)
            {
                if(SdfTools.TryGetFirstInParents<SdfCollection>(this, out var col)) col.OnTextureChanged(this);
                hasTextureChanged = false;
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        protected virtual void ApplyTransformToShape(ref ShapeTRS trs)
        {
            Vector3 pos     = Quaternion.Inverse(renderer.transform.rotation) * (transform.position - renderer.transform.position);
            Quaternion rot  = Quaternion.Inverse(renderer.transform.localRotation) * transform.parent.localRotation * transform.localRotation;
            
            trs.txi = Matrix4x4.TRS(pos, rot, Vector3.one).inverse;
            trs.scale = transform.lossyScale.y;
            trs.depth = Mathf.Abs(transform.lossyScale.z);
        }

        public virtual int GetDataSize(){ return Marshal.SizeOf<Shape>(); }

        public virtual void WriteOnBuffer(ref ComputeBuffer buffer, int id){ buffer.SetData(new Shape[]{shape}, 0, id, 1); }

        protected bool SetUpFromParents()
        {
            if(SdfTools.TryGetFirstInParents<SdfCompositor>(this, out var comp))
            {
                if(SdfTools.TryGetFirstInChilren<Renderer>(comp, out var rend)){
                    renderer = rend;
                    return true;
                }
                else Debug.LogWarning("Renderer not found");
            }
            else Debug.LogWarning("Compositor not found");
            return false;
        }

        protected virtual void Init()
        {
            if(SetUpFromParents())
            {
                shape.off2d = offset2D;
                shape.round = roundness; 
                shape.blend = blendStrength;

                ApplyTransformToShape(ref shape.TRS);
                hasDataChanged = true;
            }
            else return; // todo
        }

        protected virtual void Dispose()
        {
            if(!Application.isPlaying)
            {
                if(SdfTools.TryGetFirstInParents(this, out SdfCollection col)){
                    col.UnregisterElement(this);
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////////
        protected virtual void OnEnable()
        { 
            if(!Application.isPlaying)Init(); 
        }

        // private void OnDisable(){ if(!gameObject.activeSelf) Dispose(); }

        protected virtual void Awake()
        {
            if(Application.isPlaying) {
                enabled = false;
                Init();
            }
        }

        protected virtual void OnDestroy(){Dispose();}

        //TODO
        public virtual void WriteOnBuffer(ref Texture2D buffer, int id){ Debug.Log("TODO: Write on Texture buffer in elements"); }
    }
}
#endif

//____________________________________________________________________________________________________________
