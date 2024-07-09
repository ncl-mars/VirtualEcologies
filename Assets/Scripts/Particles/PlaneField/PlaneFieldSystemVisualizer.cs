/*
    https://docs.unity3d.com/ScriptReference/Graphics.RenderPrimitives.html
*/
using System;
using UnityEngine;

using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.Rendering;


namespace Custom.Particles.PlaneField.Visualizer
{
    internal static class MateProps
    {
        internal static readonly string[] k_numFields       = {"_1xF", "_2xF", "_3xF", "_4xF"}; // NUM_FIELDS

        internal static readonly string keyTexArray = "_FIELD_TEXARRAY";

        internal static readonly int uvb   = Shader.PropertyToID("_UVB");
        internal static readonly int umb  = Shader.PropertyToID("_UMB");

        internal static readonly int textures  = Shader.PropertyToID("_FieldTex");
    }

    public enum State : int
    {
        Hide = 0,
        Show = 1,
    }

    [ExecuteInEditMode]
    public class PlaneFieldSystemVisualizer : MonoBehaviour
    {
        //---------------------------------------------------------------------
        private PlaneFieldSystem system;
        [SerializeField] private ParticlesSceneObjects sceneObjects;

        public Material material;
        private RenderParams renderParams;

        public bool destroyOnStart = false;

        private Vector4 grid = new(64,64); // frag grid

        [SerializeField] private Vector4[] uvb;
        private Matrix4x4[] umb = new Matrix4x4[2];

        [SerializeField] private Mesh mesh;

        const int fieldsIndex = 3;

        //---------------------------------------------------------------------
        private void OnEnable()
        {
            if(!Application.isPlaying)
            {
                if(TryGetComponent(out PlaneFieldSystem system)) Init(system);

                else 
                {
                    enabled = false;
                    Debug.LogWarning("No PlaneFieldSystem found, This only work on a game object having a PlaneFieldSystem attach to it");
                }
            }
            else 
            {
                if(destroyOnStart) DestroyImmediate(this);
                else
                {
                    if(TryGetComponent(out PlaneFieldSystem system)) Init(system);
                    else 
                    {
                        enabled = false;
                        Debug.LogWarning("No PlaneFieldSystem found, This only work on a game object having a PlaneFieldSystem attach to it");
                    }
                }
            }
        }

        private void OnDisable(){ Dispose();}

        private void Update()
        {
            Graphics.RenderMeshPrimitives(renderParams, mesh, 0, sceneObjects.fields.Length);
        }

        private void OnValidate()
        {

        }

        //---------------------------------------------------------------------
        public PlaneFieldSystemVisualizer()
        {
            uvb ??= new Vector4[fieldsIndex]; // create without field
        }

        public void Init(PlaneFieldSystem system)
        {
            if((material == null) || (mesh == null))
            {
                enabled = false;
                return;
            }

            this.system = system;
            
            RecreateRenderer();
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged += RecreateRenderer;
#endif
        }

        public void Dispose()
        {
            UnregisterSceneObjects();
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged -= RecreateRenderer;
#endif
        }

        //---------------------------------------------------------------------
        private void OnFieldChanged(ParticlesForceField field, int index)
        {
            SetFromField(field, index);
            renderParams.matProps.SetMatrixArray(MateProps.umb, umb);
            renderParams.matProps.SetVectorArray(MateProps.uvb, uvb);
        }

        private void RecreateRenderer()
        {
            UnregisterSceneObjects();
            
            sceneObjects = system.GetParticlesSceneObjects();

            ParticlesForceField[] fields = sceneObjects.fields;
            PlaneFieldSimulation simulation =  system.Simulation as PlaneFieldSimulation;

            uvb[0][0] = fields.Length;                      // x : numFields
            uvb[0][1] = simulation.FieldTexture.width;      // z : field width
            uvb[0][2] = simulation.FieldTexture.height;     // w : field height

            uvb[1] = grid;
            uvb[2][0] = simulation.Mode.GetIndex();     // w : field height

            InitFieldsData(fields);
            InitRenderParams(simulation);
            RegisterToSceneObjects();
        }

        private void InitRenderParams(PlaneFieldSimulation simulation)
        {
            Material i_material = Instantiate(material);
            Texture fieldTex = simulation.FieldTexture;
            
            renderParams = new(i_material)
            {
                worldBounds = new Bounds(simulation.Origin, simulation.Extents * 2),
                matProps = new MaterialPropertyBlock(),
            };
            
            if(fieldTex.dimension == TextureDimension.Tex2DArray)
            {
                i_material.EnableKeyword(CSProps.k_fieldTexArray);
            }

            int numFields = Mathf.Clamp(umb.Length / 2, 1, 4); // WARNING !, numField cached?
            i_material.EnableKeyword(CSProps.k_numFields[numFields - 1]);

            renderParams.matProps.SetVectorArray(MateProps.uvb, uvb);
            renderParams.matProps.SetMatrixArray(MateProps.umb, umb);

            renderParams.matProps.SetTexture(MateProps.textures,fieldTex);
        }

        private void InitFieldsData(ParticlesForceField[] fields)
        {
            int arrLength = fieldsIndex + fields.Length;
            
            if(uvb.Length != arrLength)
            {
                Vector4[] vectors = new Vector4[arrLength];
                Array.Copy(uvb, 0, vectors, 0, Mathf.Min(arrLength, uvb.Length));
                uvb = vectors;
            }

            umb = new Matrix4x4[fields.Length * 2];
            
            for(int i = 0; i < fields.Length; i++) SetFromField(fields[i], i);
        }

        public void SetFromField(ParticlesForceField field, int bufferIndex)
        {
            uvb[fieldsIndex + bufferIndex][0] = field.transform.lossyScale.y;
            uvb[fieldsIndex + bufferIndex][1] = field.transform.lossyScale.z;
            uvb[fieldsIndex + bufferIndex][2] = field.FieldTextureID;

            umb[bufferIndex * 2]       = field.transform.worldToLocalMatrix; // (data need invert transpose matrices for scales)
            umb[bufferIndex * 2 + 1]   = field.transform.localToWorldMatrix;
        }

        private void RegisterToSceneObjects()
        {
            for(int i = 0; i < sceneObjects.fields.Length; i++)
            {
                sceneObjects.fields[i].BufferIndex = i;
                sceneObjects.fields[i].OnFieldChanged += OnFieldChanged;
            }
        }

        private void UnregisterSceneObjects()
        {
            if(sceneObjects == null) return;
            
            for(int i = 0; i < sceneObjects.fields.Length; i++)
            {
                if(sceneObjects.fields[i] != null) 
                {
                    sceneObjects.fields[i].OnFieldChanged -= OnFieldChanged;
                }
            }
        }
    }
}












        // internal static readonly int fieldBuffer    = Shader.PropertyToID("_FieldHelpers");
        // internal static readonly int fieldTexSize   = Shader.PropertyToID("_FieldTexSize");
        // internal static readonly int simulationMode = Shader.PropertyToID("_SimulationMode");
        // internal static readonly int numFields      = Shader.PropertyToID("_NumFields");
        // internal static readonly int grid           = Shader.PropertyToID("_Grid");
        // internal static readonly int wireframe      = Shader.PropertyToID("_Wireframe");




            // fieldsData[index].Field = field;
            // fieldBuffer.SetData(fieldsData, index, index, 1);
            // renderParams.matProps.SetBuffer(MateProps.fieldBuffer, fieldBuffer);

            // if(fieldBuffer != null)
            // {
            //     renderParams.matProps.SetInt(MateProps.wireframe, wireframe ? 1 : 0);
            //     fieldBuffer.SetData(fieldsData);
            //     renderParams.matProps.SetBuffer(MateProps.fieldBuffer, fieldBuffer);
            // }

            // fieldBuffer?.Dispose();
            // fieldBuffer = null;
                // renderParams.matProps.SetVector(MateProps.fieldTexSize, 
                //     new Vector4(fieldTex.width, fieldTex.height, (fieldTex as Texture2DArray).depth));

            // renderParams.matProps.SetInt(MateProps.simulationMode, simulation.Mode.GetIndex());
            // renderParams.matProps.SetVector(MateProps.grid, grid);
            // renderParams.matProps.SetInt(MateProps.numFields, fieldsData.Length);
            // renderParams.matProps.SetInt(MateProps.wireframe, wireframe ? 1 : 0);

            // else renderParams.matProps.SetVector(MateProps.fieldTexSize, new Vector4(fieldTex.width, fieldTex.height));


            // renderParams.matProps.SetBuffer(MateProps.fieldBuffer, fieldBuffer);
        // private void InitBuffers()
        // {
        //     fieldBuffer?.Release();
        //     fieldBuffer = new(GraphicsBuffer.Target.Structured, fieldsData.Length, FieldHelperData.Size);
        //     fieldBuffer.SetData(fieldsData);
        // }
