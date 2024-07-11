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


    //-- Particles Buffer
    public class ParticlesBuffers
    {
        private readonly RenderTexture[] buffers;
        public RenderTexture[] Buffers{get=>buffers;}

        public RenderTexture Positions{ get=>buffers[0];set=>buffers[0] = value;}
        public RenderTexture Velocities{get=>buffers[1];set=>buffers[1] = value;}

        public ParticlesBuffers(ParticlesSimulation simulation, ParticlesSceneObjects scene)
        {
            int dim = Mathf.CeilToInt(Mathf.Sqrt(simulation.MaxCount));

            Debug.Log("Creating particles texture buffers with size : "+ dim + " * " + dim);
            
            buffers = new RenderTexture[2];
            Texture2D[] tmpBuffers = new Texture2D[buffers.Length];

            for(int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new RenderTexture(dim, dim, 0, RenderTextureFormat.ARGBFloat){};
                buffers[i].Create();

                tmpBuffers[i] = TexUtils.CreateFloatTexFromRTex(buffers[i]);
            }

            for(int i = 0; i < simulation.MaxCount; i++)
            {
                int X = i % dim;
                int Y = Mathf.FloorToInt(i/dim);

                Vector3 randPos = VectorUtils.Random3() * 0.5f;

                Transform trs = scene.emitters[i%scene.emitters.Length].transform;
                randPos = trs.localToWorldMatrix.MultiplyPoint(randPos);
                
                Vector4 velocity = new(0,0,0,-1);
                Vector4 position = new(randPos.x, randPos.y, randPos.z, 0);

                tmpBuffers[0].SetPixel(X, Y, position);
                tmpBuffers[1].SetPixel(X, Y, velocity);
            }

            for(int i = 0; i < buffers.Length; ++i)
            {
                tmpBuffers[i].Apply();
                Graphics.Blit(tmpBuffers[i], buffers[i]);
                UnityEngine.Object.Destroy(tmpBuffers[i]);
            }
        }

        public void Dispose()
        {
            for(int i = 0; i < buffers.Length; i++)
            {
                buffers[i].Release();
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////////////
    [Serializable] public class PlaneFieldSimulation : ParticlesSimulation
    {
        internal static class MateProps
        {
            internal static readonly string[] k_names    = {
                "_ALTI_PATH",
                "_GRAV_TOPO",
                "_PLATEFORM"
            };

            internal static readonly string[] k_numFields       = {"_1xF", "_2xF", "_3xF", "_4xF"}; // NUM_FIELDS
            internal static readonly string[] k_numEmitts       = {"_1xE", "_2xE", "_3xE", "_4xE"}; // NUM_EMITTER
            internal static readonly string k_fieldTexArray     = "_FIELD_TEXARRAY";

            //----------------------- GLOBALS / CONSTANT
            internal static readonly int    g_vectors   = Shader.PropertyToID("_GV");
            internal static readonly int[]  g_textures  = {
                Shader.PropertyToID("_NoiseTex"),
            };

            //----------------------- buffers
            internal static readonly int uvb   = Shader.PropertyToID("_UVB");
            internal static readonly int umb  = Shader.PropertyToID("_UMB");

            internal static readonly int[] buffers      = {
                Shader.PropertyToID("_Positions"),
                Shader.PropertyToID("_Velocities"),
            };

            //----------------------- TEXTURES
            internal static readonly int[] textures = {
                Shader.PropertyToID("_FieldTex"),
            };
        }

        [SerializeField] private SimulationMode mode = SimulationMode.AltitudePath;
        public SimulationMode Mode{get => mode; set => mode = value;}
        
        private Texture[] g_textures = new Texture[1]; 

        [SerializeField] private Texture[] textures = new Texture[1];
        public Texture FieldTexture{get => textures[0];}

        // matrices array, 0: Domain To Field, 1: FieldToDomain
        private Matrix4x4[] umb = new Matrix4x4[2];

        private ParticlesBuffers particles;
        public override RenderTexture[] Buffers{get => particles.Buffers;}

        private RenderTexture target;

        const int fieldsIndex = 8; // in uvb, end of "params uniforms"

        private ParticlesSceneObjects scene;
        private bool hasMatrixUpdate = false;

        //-------------------------------------------- Construct
       public PlaneFieldSimulation()
        {
            uvb ??= new Vector4[fieldsIndex]; // create without field
        }
        
        public void Init(PlaneFieldSystem system, ParticlesSceneObjects scene)
        {
            this.material = new Material(system.simulationShader); // or hold the keyword
            this.scene = scene;

            g_textures = new Texture2D[]{system.noiseTex};

            // 0 : settings, x : numParticles
            uvb[0][1] = scene.emitters.Length;  // y : numEmitters
            uvb[0][2] = textures[0].width;      // z : field width
            uvb[0][3] = textures[0].height;     // w : field height

            DomainTransform = scene.domain.transform;   // 2 : origin, 3: extents
            uvb[3][3] = system.transform.lossyScale.y;

            InitSceneData();

            particles = new ParticlesBuffers(this, scene);
            target = new(particles.Positions);

            InitMaterial();
            RegisterSceneObjects();
        }

        public void InitSceneData()
        {
            CheckRecreateUVB();

            umb = new Matrix4x4[scene.fields.Length * 2 + scene.emitters.Length];
            
            for(int i = 0; i < scene.fields.Length; i++)    SetFromField(scene.fields[i], i);
            for(int i = 0; i < scene.emitters.Length; i++)  SetFromEmitter(scene.emitters[i], i);
        }

        public void SetFromField(ParticlesForceField field, int bufferIndex)
        {
            // vectors
            uvb[fieldsIndex + bufferIndex][0] = field.transform.lossyScale.y;
            uvb[fieldsIndex + bufferIndex][1] = field.transform.lossyScale.z;
            uvb[fieldsIndex + bufferIndex][2] = field.FieldTextureID;
            // matrices
            umb[bufferIndex * 2]       = field.transform.worldToLocalMatrix; // (data need invert transpose matrices for scales)
            umb[bufferIndex * 2 + 1]   = field.transform.localToWorldMatrix;
        }

        public void SetFromEmitter(ParticlesEmitter emitter, int bufferIndex)
        {
            int index = scene.fields.Length * 2 + bufferIndex;
            umb[index] = emitter.transform.localToWorldMatrix;
        }

        public void CheckRecreateUVB()
        {
            int arrLength = fieldsIndex + scene.fields.Length;
            
            if(uvb.Length != arrLength)
            {
                Vector4[] vectors = new Vector4[arrLength];
                Array.Copy(uvb, 0, vectors, 0, Mathf.Min(arrLength, uvb.Length));
                uvb = vectors;
            }
        }

        private void InitMaterial()
        {
            // enable simulation mode
            int indexMode = Array.IndexOf(Enum.GetValues(mode.GetType()), mode);
            string simKey = MateProps.k_names[ Mathf.Clamp( indexMode, 0, MateProps.k_names.Length)];
            material.EnableKeyword(simKey);

            // enable num fields
            int numFields = Mathf.Clamp(scene.fields.Length, 1, 4); // WARNING !, numField cached?
            material.EnableKeyword(MateProps.k_numFields[numFields - 1]);
    
            // enable num emitters
            int numEmitts = Mathf.Clamp(scene.emitters.Length, 1, 4); // WARNING !, numField cached?
            material.EnableKeyword(MateProps.k_numEmitts[numEmitts - 1]);

            // enable field texture mode
            if(textures[0].dimension == TextureDimension.Tex2DArray){
                material.EnableKeyword(MateProps.k_fieldTexArray);
            }
            
            // Set Datas
            material.SetTexture(MateProps.g_textures[0], g_textures[0]);    // noises
            material.SetTexture(MateProps.textures[0], textures[0]);        // fields

            material.SetVector(MateProps.g_vectors, new Vector4( g_textures[0].width, g_textures[0].height, 0, 0)); // noise dimen
            
            material.SetMatrixArray(MateProps.umb, umb);
            material.SetVectorArray(MateProps.uvb, uvb);

            material.SetTexture(MateProps.buffers[0], particles.Positions);     // position
            material.SetTexture(MateProps.buffers[1], particles.Velocities);    // velocity
        }

        private void UpdateTimes()
        {
            uvb[1][0] = Time.fixedTime;
            uvb[1][1] = Time.fixedDeltaTime;
        }

        public void Run()
        {
            UpdateTimes();
            material.SetVectorArray(MateProps.uvb, uvb); // update vectors each frame

            if(hasMatrixUpdate)
            {
                material.SetMatrixArray(MateProps.umb, umb);
                hasMatrixUpdate = false;
            }

            RenderTexture.active = target;            
            Graphics.Blit(null, target, material, 0);
            Graphics.Blit(target, particles.Velocities);
            Graphics.Blit(null, target, material, 1);
            Graphics.Blit(target, particles.Positions);
        }
        
        public override void Dispose()
        {
            UnregisterSceneObjects();
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
            SetFromEmitter(emitter, bufferIndex);
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
    
#if UNITY_EDITOR
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
#endif
    }
}







    // //-- Particles Buffer
    // public class ParticlesHandler : ParticlesBufferHandler
    // {
    //     public struct Data // particule data, should fit in 2 textures (2*vector4)
    //     {
    //         public Vector3 vel;
    //         public float zone;

    //         public Vector3 pos;
    //         public float life; // life, zone
            
    //         public static int Size{get=>Marshal.SizeOf<Data>();}
    //     }

    //     public ParticlesHandler(ParticlesSimulation simulation, ParticlesSceneObjects scene)
    //     {
    //         buffer = new ComputeBuffer(simulation.MaxCount, Data.Size);
    //         buffer.SetData(InitData(scene.emitters));
    //     }

    //     private Array InitData(ParticlesEmitter[] emitters)
    //     {
    //         Data[] data = new Data[buffer.count];

    //         for(int i = 0; i < buffer.count; i++)
    //         {
    //             Vector3 randPos = VectorUtils.Random3() * 0.5f;
    //             Transform trs = emitters[i%emitters.Length].transform;
    //             randPos = trs.localToWorldMatrix.MultiplyPoint(randPos);
                
    //             data[i] = new()
    //             {
    //                 // vel = VectorUtils.Random3().normalized,
    //                 pos = randPos,
    //                 vel = Vector3.zero,
    //                 life = 1.0f,
    //                 zone = -1.0f,
    //             };
    //         }
    //         return data;
    //     }
    // }


        // public struct Data // particule data, should fit in 2 textures (2*vector4)
        // {
        //     public Vector4 velocities;
        //     public Vector4 positions;

        //     // public Vector3 vel;
        //     // public float zone;
        //     // public Vector3 pos;
        //     // public float life; // life, zone
        // }


        // public void CheckCreateUVB()
        // {
        //     int arrLength = fieldsIndex + scene.fields.Length;
            
        //     if(uvb.Length != arrLength)
        //     {
        //         Vector4[] vectors = new Vector4[arrLength];
        //         Array.Copy(uvb, 0, vectors, 0, Mathf.Min(arrLength, uvb.Length));
        //         uvb = vectors;
        //     }
        // }

        // public void CheckCreateUMB()
        // {
        //     int arrLength = scene.fields.Length * 2 + scene.emitters.Length;
            
        //     umb = new Matrix4x4[arrLength];
            
        //     if(uvb.Length != arrLength)
        //     {
        //         Vector4[] vectors = new Vector4[arrLength];
        //         Array.Copy(uvb, 0, vectors, 0, Mathf.Min(arrLength, uvb.Length));
        //         uvb = vectors;
        //     }

        //     umb = new Matrix4x4[fields.Length * 2];
        // }

    // //-- Particles Buffer, TODO : FallBack to texture buffer
    // public class EmittersHandler : ParticlesBufferHandler
    // {
    //     public struct Data // particule data
    //     {
    //         public Matrix4x4 txx;
    //         public static int Size{get=>Marshal.SizeOf<Data>();}
    //     }

    //     Data[] data;

    //     public void SetFromEmitter(ParticlesEmitter emitter, int id)
    //     {
    //         data[id].txx = emitter.transform.localToWorldMatrix;
    //         buffer.SetData(data, id, id, 1);
    //     }
        
    //     public EmittersHandler(ParticlesSimulation _, ParticlesSceneObjects scene)
    //     {
    //         buffer = new ComputeBuffer(scene.emitters.Length, Data.Size, ComputeBufferType.Constant);
    //         buffer.SetData(InitData(scene.emitters));
    //     }

    //     private Array InitData(ParticlesEmitter[] emitters)
    //     {
    //         data = new Data[emitters.Length];
    //         for(int i = 0; i < data.Length; i++){
    //             data[i].txx = emitters[i].transform.localToWorldMatrix;
    //         }
    //         return data;
    //     }
    // }




        // [SerializeField] private MapType mapType = MapType.TopoGravity;
        // public MapType MapType{get => mapType; set => mapType = value;}