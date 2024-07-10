using System;
using UnityEngine;
using UnityEngine.Rendering;


namespace Custom.Particles.PlaneField
{
    // used for gui
    [Flags] public enum RendererModes : short
    {
        DistanceToClosestZone = 1,
        DistanceToOrigin = 2,
        Velocity = 4,
        BillBoard = 8,
        Velocity_Cam = 16,
        Velocity_Yup = 32,
    };

    //////////////////////////////////////////////////////////////////////
    [Serializable] public class PlaneFieldRenderer : ParticlesRenderer
    {
        static class MateProps
        {
            internal static readonly int buffer         = Shader.PropertyToID("_Particles");
            
            internal static readonly int[] buffers      = {
                Shader.PropertyToID("_Positions"),
                Shader.PropertyToID("_Velocities"),
            };

            internal static readonly int sprites        = Shader.PropertyToID("_Sprites");

            internal static readonly int[] maps        = {Shader.PropertyToID("_BumpMap")};

            internal static readonly string[] k_modes   = {"_ALTI_PATH", "_GRAV_TOPO", "_PLATEFORM"};
            
            //----------------------- Vector Buffer
            internal static readonly int gvb      = Shader.PropertyToID("_GV"); // constant
            internal static readonly int uvb      = Shader.PropertyToID("_UVB");
        }


        private const int uvb_length = 16;

        [SerializeField] private Texture[] textures = new Texture[1];
        [SerializeField] protected Texture2DArray spriteCollection;

        protected Mesh mesh;

        public PlaneFieldRenderer()
        {
            uvb ??= (new Vector4[uvb_length]);
            uvb = (uvb.Length != uvb_length) ? new Vector4[uvb_length] : uvb;
        }

        public void Init(ParticlesSystem system, ParticlesSceneObjects scene)
        {
            mesh = MeshUtils.CreateQuad();

            DomainTransform = scene.domain.transform;   // 2 : origin, 3: extents
            uvb[2][3] = system.transform.lossyScale.y;

            renderParams = InitRendererParams(system);
        }

        public RenderParams InitRendererParams(ParticlesSystem system)
        {
            PlaneFieldSimulation simulation = system.Simulation as PlaneFieldSimulation;
            Material i_material = GameObject.Instantiate(material);

            i_material.EnableKeyword(MateProps.k_modes[simulation.Mode.GetIndex()]);

            RenderParams rp = new(i_material)
            {
                layer = system.gameObject.layer,
                worldBounds = new Bounds(simulation.Origin, simulation.Extents * 2),
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                matProps = new()
            };


            rp.matProps.SetTexture(MateProps.buffers[0], simulation.Buffers[0]); // position
            rp.matProps.SetTexture(MateProps.buffers[1], simulation.Buffers[1]); // velocity

            rp.matProps.SetTexture(MateProps.sprites, spriteCollection);

            if(textures[0] != null) rp.matProps.SetTexture(MateProps.maps[0], textures[0]);

            rp.matProps.SetVector(
                MateProps.gvb, 
                new Vector4(simulation.MaxCount, spriteCollection.depth) 
            );

            rp.matProps.SetVectorArray(MateProps.uvb, uvb);

            return rp;
        }

        public RenderParams Update(ParticlesSimulation simulation)
        {
            renderParams.matProps.SetTexture(MateProps.buffers[0], simulation.Buffers[0]); // position
            renderParams.matProps.SetTexture(MateProps.buffers[1], simulation.Buffers[1]); // velocity
            renderParams.matProps.SetVectorArray(MateProps.uvb, uvb);
            
            return renderParams;
        }

        public void Draw(int drawCount)
        {
            Graphics.RenderMeshPrimitives(renderParams, mesh, 0, drawCount);
        }

        public override void Dispose(){}


#if UNITY_EDITOR
        public void RecreateSerializedVectors()
        {
            Debug.Log("recreating serialized object");
            Vector4[] vectors = new Vector4[uvb_length];

            Array.Copy(uvb, vectors, Mathf.Min(uvb.Length, uvb_length));

            int diff = uvb_length - uvb.Length;

            for(int u = 0; u < Mathf.Max(diff,0); u++)
            {
                vectors[uvb.Length + u] = Vector4.one * 0.5f;
            }
            uvb = (Vector4[])vectors.Clone();
        }
        public void RecreateSerializedTextures()
        {
            Texture[] m_textures = new Texture[1];

            Array.Copy(textures, m_textures, Mathf.Min(textures.Length, m_textures.Length));

            int diff = m_textures.Length - textures.Length;

            for(int u = 0; u < Mathf.Max(diff,0); u++)
            {
                m_textures[textures.Length + u] = Texture2D.blackTexture;
            }
            textures = (Texture[])m_textures.Clone();
        }
#endif
    }
}