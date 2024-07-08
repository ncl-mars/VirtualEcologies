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
        internal static readonly string keyTexArray = "_FIELD_TEXARRAY";

        internal static readonly int fieldBuffer   = Shader.PropertyToID("_FieldHelpers");
        internal static readonly int fieldTexture  = Shader.PropertyToID("_FieldTex");

        internal static readonly int fieldTexSize   = Shader.PropertyToID("_FieldTexSize");
        internal static readonly int simulationMode = Shader.PropertyToID("_SimulationMode");
        internal static readonly int numFields      = Shader.PropertyToID("_NumFields");
        internal static readonly int grid           = Shader.PropertyToID("_Grid");
        internal static readonly int wireframe      = Shader.PropertyToID("_Wireframe");
    }

    public enum State : int
    {
        Hide = 0,
        Show = 1,
    }

    [ExecuteInEditMode]
    public class PlaneFieldSystemVisualizer : MonoBehaviour
    {
        [Serializable] public struct FieldHelperData
        {
            [HideInInspector] public Matrix4x4 localToWorld;
            [HideInInspector] public Matrix4x4 worldToLocal;
            [HideInInspector] public Vector2 scales;
            [HideInInspector] public int texId;
            public State state;
            
            public static int Size{get=>Marshal.SizeOf<FieldHelperData>();}

            public ParticlesForceField Field
            {
                set
                {
                    localToWorld = value.transform.localToWorldMatrix;
                    worldToLocal = value.transform.worldToLocalMatrix;

                    scales.x = value.transform.lossyScale.y;
                    scales.y = value.transform.lossyScale.z;
                    texId = value.FieldTextureID;
                }
            }
        }

        //---------------------------------------------------------------------
        private PlaneFieldSystem system;
        [SerializeField] private ParticlesSceneObjects sceneObjects;

        public Material material;
        private RenderParams renderParams;

        public bool wireframe = true;
        public bool destroyOnStart = true;

        [SerializeField] private FieldHelperData[] fieldsData;
        private GraphicsBuffer fieldBuffer;

        private Vector4 grid = new(64,64); // vertex grid
        private int indexCount; // tris vertices

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
            Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indexCount, fieldsData.Length);
        }

        private void OnValidate()
        {
            if(fieldBuffer != null)
            {
                renderParams.matProps.SetInt(MateProps.wireframe, wireframe ? 1 : 0);
                fieldBuffer.SetData(fieldsData);
                renderParams.matProps.SetBuffer(MateProps.fieldBuffer, fieldBuffer);
            }
        }

        //---------------------------------------------------------------------
        public void Init(PlaneFieldSystem system)
        {
            if(material == null)
            {
                enabled = false;
                return;
            }

            this.system = system;
            indexCount = (int)((grid.x-1)*(grid.y-1) * 6); // 6 indices, 2 tris per quad
            
            RecreateRenderer();
            
            EditorApplication.hierarchyChanged += RecreateRenderer;
        }

        public void Dispose()
        {
            UnregisterSceneObjects();

            EditorApplication.hierarchyChanged -= RecreateRenderer;

            fieldBuffer?.Dispose();
            fieldBuffer = null;
        }

        //---------------------------------------------------------------------
        private void OnFieldChanged(ParticlesForceField field, int index)
        {
            fieldsData[index].Field = field;
            fieldBuffer.SetData(fieldsData, index, index, 1);
            renderParams.matProps.SetBuffer(MateProps.fieldBuffer, fieldBuffer);
        }

        private void RecreateRenderer()
        {
            UnregisterSceneObjects();
            InitFieldsData();
            InitBuffers();
            InitRenderParams();
            RegisterToSceneObjects();
        }

        private void InitBuffers()
        {
            fieldBuffer?.Release();
            fieldBuffer = new(GraphicsBuffer.Target.Structured, fieldsData.Length, FieldHelperData.Size);
            fieldBuffer.SetData(fieldsData);
        }

        private void InitRenderParams()
        {
            Material i_material = Instantiate(material);
            Texture fieldTex =  (system.Simulation as PlaneFieldSimulation).FieldTexture;
            
            renderParams = new(i_material)
            {
                worldBounds = new Bounds(system.Simulation.Origin, system.Simulation.Extents * 2),
                matProps = new MaterialPropertyBlock(),
            };

            SimulationMode mode = (system.Simulation as PlaneFieldSimulation).Mode;

            renderParams.matProps.SetInt(MateProps.simulationMode, mode.GetIndex());
            renderParams.matProps.SetVector(MateProps.grid, grid);
            
            renderParams.matProps.SetInt(MateProps.numFields, fieldsData.Length);
            renderParams.matProps.SetInt(MateProps.wireframe, wireframe ? 1 : 0);
            
            if(fieldTex.dimension == TextureDimension.Tex2DArray)
            {
                i_material.EnableKeyword(CSProps.k_fieldTexArray);
                renderParams.matProps.SetVector(MateProps.fieldTexSize, 
                    new Vector4(fieldTex.width, fieldTex.height, (fieldTex as Texture2DArray).depth));
            }
            else renderParams.matProps.SetVector(MateProps.fieldTexSize, new Vector4(fieldTex.width, fieldTex.height));

            renderParams.matProps.SetTexture(MateProps.fieldTexture,fieldTex);
            renderParams.matProps.SetBuffer(MateProps.fieldBuffer, fieldBuffer);
        }

        private void InitFieldsData()
        {
            sceneObjects = system.GetParticlesSceneObjects();
            int newFieldsLength = sceneObjects.fields.Length;

            if(fieldsData == null || fieldsData.Length != newFieldsLength) 
            {
                fieldsData = new FieldHelperData[newFieldsLength];

                for(int i = 0; i < newFieldsLength; i++)
                {
                    fieldsData[i].Field = sceneObjects.fields[i];
                    fieldsData[i].state = State.Show;
                }
            }
            else for(int i = 0; i < newFieldsLength; i++) fieldsData[i].Field = sceneObjects.fields[i];
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
