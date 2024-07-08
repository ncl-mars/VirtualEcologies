using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;


namespace Custom.Particles.PlaneField
{
    [Flags] public enum SimulationMode : short
    {
        AltitudePath    = 1,
        GravityTopo     = 2,
        Plateform     = 4,
    }

    ///////////////////////////////////////////////////////////////////////////////////
    internal static class CSProps
    {
        //----------------------- LABELS
        internal static readonly string[] k_names    = {
            "CSAltiPath",
            "CSGravTopo",
            "CSPlateform"
        };

        internal static readonly string[] k_numFields       = {"_1xF", "_2xF", "_3xF", "_4xF"}; // NUM_FIELDS
        internal static readonly string k_fieldTexArray     = "_FIELD_TEXARRAY";

        //----------------------- GLOBALS / CONSTANT
        internal static readonly int    g_vectors   = Shader.PropertyToID("_GV");
        internal static readonly int[]  g_textures  = {
            Shader.PropertyToID("_NoiseTex"),
        };

        //----------------------- UNIFORMS/VARYING Array
        internal static readonly int uvb   = Shader.PropertyToID("_UVB");
        // 0: settings : numParticles, numEmitters, fieldWidth, fieldHeight
        // 1: times    : time, deltaTime, delayStart,
        // 2: origin
        // 3: extents
        // 4: simParams
        // 5: weights

        internal static readonly int umb  = Shader.PropertyToID("_UMB");

        //----------------------- COMPUTE BUFFER
        internal static readonly int[] u_buffers = {
            Shader.PropertyToID("_Particles"),
            Shader.PropertyToID("_Emitters"),
        };
        //----------------------- TEXTURES
        internal static readonly int[] textures = {
            Shader.PropertyToID("_FieldTex"),
        };
    }

    //-- Particles Buffer, TODO : FallBack to texture buffer
    public class EmittersHandler : ParticlesBufferHandler
    {
        public struct Data // particule data
        {
            public Matrix4x4 txx;
            public static int Size{get=>Marshal.SizeOf<Data>();}
        }

        Data[] data;

        public void SetFromEmitter(ParticlesEmitter emitter, int id)
        {
            data[id].txx = emitter.transform.localToWorldMatrix;
            buffer.SetData(data, id, id, 1);
        }
        
        public EmittersHandler(ParticlesSimulation _, ParticlesSceneObjects scene)
        {
            buffer = new ComputeBuffer(scene.emitters.Length, Data.Size, ComputeBufferType.Constant);
            buffer.SetData(InitData(scene.emitters));
        }

        private Array InitData(ParticlesEmitter[] emitters)
        {
            data = new Data[emitters.Length];
            for(int i = 0; i < data.Length; i++){
                data[i].txx = emitters[i].transform.localToWorldMatrix;
            }
            return data;
        }
    }

    //-- Particles Buffer
    public class ParticlesHandler : ParticlesBufferHandler
    {
        public struct Data // particule data, should fit in 2 textures (2*vector4)
        {
            public Vector3 vel;
            public float zone;

            public Vector3 pos;
            public float life; // life, zone
            
            public static int Size{get=>Marshal.SizeOf<Data>();}
        }

        public ParticlesHandler(ParticlesSimulation simulation, ParticlesSceneObjects scene)
        {
            buffer = new ComputeBuffer(simulation.MaxCount, Data.Size);
            buffer.SetData(InitData(scene.emitters));
        }

        private Array InitData(ParticlesEmitter[] emitters)
        {
            Data[] data = new Data[buffer.count];

            for(int i = 0; i < buffer.count; i++)
            {
                Vector3 randPos = VectorUtils.Random3() * 0.5f;
                Transform trs = emitters[i%emitters.Length].transform;
                randPos = trs.localToWorldMatrix.MultiplyPoint(randPos);
                
                data[i] = new()
                {
                    // vel = VectorUtils.Random3().normalized,
                    pos = randPos,
                    vel = Vector3.zero,
                    life = 1.0f,
                    zone = -1.0f,
                };
            }
            return data;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////
    [Serializable] public class PlaneFieldSimulation : ParticlesSimulation
    {
        [SerializeField] private SimulationMode mode = SimulationMode.AltitudePath;
        public SimulationMode Mode{get => mode; set => mode = value;}
        
        // global textures, for all instance of the shader
        // [SerializeField] 
        private Texture[] g_textures = new Texture[1]; 

        [SerializeField] private Texture[] textures = new Texture[1];
        public Texture FieldTexture{get => textures[0];}

        // matrices array, 0: Domain To Field, 1: FieldToDomain
        private Matrix4x4[] umb = new Matrix4x4[2];

        // buffer handlers
        private ParticlesHandler particles;
        private EmittersHandler emitters;

        public override ComputeBuffer ParticlesBuffer{get => particles.Buffer;}

        static readonly int fieldsIndex = 8; // in uvb, end of "params uniforms"

        private ParticlesSceneObjects scene;
        private bool hasMatrixUpdate = false;

        //-------------------------------------------- Construct
       public PlaneFieldSimulation()
        {
            uvb ??= new Vector4[fieldsIndex]; // create without field
        }

        public void SetFromField(ParticlesForceField field, int bufferIndex)
        {
            uvb[fieldsIndex + bufferIndex][0] = field.transform.lossyScale.y;
            uvb[fieldsIndex + bufferIndex][1] = field.transform.lossyScale.z;
            uvb[fieldsIndex + bufferIndex][2] = field.FieldTextureID;

            umb[bufferIndex * 2]       = field.transform.worldToLocalMatrix; // (data need invert transpose matrices for scales)
            umb[bufferIndex * 2 + 1]   = field.transform.localToWorldMatrix;
        }

        public void InitFieldsData(ParticlesForceField[] fields)
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
        
        public void Init(PlaneFieldSystem system, ParticlesSceneObjects scene)
        {
            this.compute = UnityEngine.Object.Instantiate(system.compute); // or hold the keyword
            this.scene = scene;

            g_textures = new Texture2D[]{system.noiseTex};

            // 0 : settings, x : numParticles
            uvb[0][1] = scene.emitters.Length;    // y : numEmitters
            uvb[0][2] = textures[0].width;      // z : field width
            uvb[0][3] = textures[0].height;     // w : field height

            DomainTransform = scene.domain.transform;   // 2 : origin, 3: extents
            uvb[3][3] = system.transform.lossyScale.y;
            // Debug.Log("setting domain scale at : " + uvb[3][3]);

            InitFieldsData(scene.fields);

            particles = new ParticlesHandler(this, scene);
            emitters  = new EmittersHandler(this, scene);

            InitCompute();

            RegisterSceneObjects();
        }

        private void InitCompute()
        {
            int indexMode = Array.IndexOf(Enum.GetValues(mode.GetType()), mode);
            kernelID = compute.FindKernel(CSProps.k_names[ Mathf.Clamp( indexMode, 0, CSProps.k_names.Length) ]); // to prevent errors

            warpCount = ComputeUtils.Get1DWarpCount(compute, kernelID, MaxCount);

            // keywords
            int numFields = Mathf.Clamp(umb.Length / 2, 1, 4); // WARNING !, numField cached?
            compute.EnableKeyword(CSProps.k_numFields[numFields - 1]);
            
            if(textures[0].dimension == TextureDimension.Tex2DArray){
                compute.EnableKeyword(CSProps.k_fieldTexArray);
            }
            
            // Set Datas
            // Debug.Log("set global noise with tex name : " + g_textures[0].name);
            compute.SetTexture(kernelID, CSProps.g_textures[0], g_textures[0]);
            compute.SetVector(CSProps.g_vectors, new Vector4( g_textures[0].width, g_textures[0].height, 0, 0));
            
            compute.SetTexture(kernelID, CSProps.textures[0], textures[0]);

            compute.SetBuffer(kernelID, CSProps.u_buffers[0], particles.Buffer);
            compute.SetBuffer(kernelID, CSProps.u_buffers[1], emitters.Buffer);
            
            compute.SetMatrixArray(CSProps.umb, umb);
            compute.SetVectorArray(CSProps.uvb, uvb);
        }

        private void UpdateTimes()
        {
            uvb[1][0] = Time.fixedTime;
            uvb[1][1] = Time.fixedDeltaTime;
        }

        public void Run()
        {
            UpdateTimes();
            compute.SetVectorArray(CSProps.uvb, uvb); // update vectors each frame

            if(hasMatrixUpdate)
            {
                compute.SetMatrixArray(CSProps.umb, umb);
                hasMatrixUpdate = false;
            }

            compute.Dispatch(kernelID, warpCount, 1, 1);
        }
        
        public override void Dispose()
        {
            UnregisterSceneObjects();
            emitters?.Dispose();
            particles?.Dispose();
        }

        //---------------------------------------------------------------------------------------
        public void OnFieldChanged(ParticlesForceField field, int bufferIndex)
        {
            SetFromField(field, bufferIndex);
            hasMatrixUpdate = true;
        }

        public void OnEmitterChanged(ParticlesEmitter emitter, int bufferIndex)
        {
            emitters.SetFromEmitter(emitter, bufferIndex);
            hasMatrixUpdate = true;
        }

        private void RegisterSceneObjects()
        {   
            for(int i = 0; i < scene.fields.Length; i++)
            {
                if(scene.fields[i] != null) 
                {
                    scene.fields[i].BufferIndex = i;
                    scene.fields[i].OnFieldChanged += OnFieldChanged;
                }
            }

            for(int i = 0; i < scene.emitters.Length; i++)
            {
                if(scene.emitters[i] != null)
                {
                    scene.emitters[i].BufferIndex = i;
                    scene.emitters[i].OnEmitterChange += OnEmitterChanged;
                }
            }
        }

        private void UnregisterSceneObjects()
        {
            for(int i = 0; i < scene.fields.Length; i++)
            {
                if(scene.fields[i] != null) scene.fields[i].OnFieldChanged -= OnFieldChanged;
            }

            for(int i = 0; i < scene.emitters.Length; i++)
            {
                if(scene.emitters[i] != null) scene.emitters[i].OnEmitterChange -= OnEmitterChanged;
            }
        }
    
        //---------------------------------------------------------------------------------------
        public void RecreateSerializedVectors()
        {
            Debug.Log("recreating serialized object");

            Vector4[] vectors = new Vector4[fieldsIndex];
            for(int u = 0; u < uvb.Length ; u++)
            {
                uvb[u] = new Vector4(0.5f, 0.25f, 0.125f, 0.75f);
            }
            Array.Copy(uvb, 0, vectors, 0, Mathf.Min(fieldsIndex, uvb.Length));
            uvb = vectors;
        }
    }
}




        // [SerializeField] private MapType mapType = MapType.TopoGravity;
        // public MapType MapType{get => mapType; set => mapType = value;}