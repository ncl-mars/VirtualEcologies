// /*
//     TODO :
//         + Lessen calls to GPU in SetCompute()
//         Tex size in vector ? Uniform Buffer ? CommandBuffer ?

//     PORTABILITY :
//         fallback on texture + pix shader for switch in place of computes (shader + buffer)
//         need to check compatibility for half type on switch => encode 8 components in RGBAFloat

//     TECHNOS :
//         - full gpu with args and variable instances count
//         https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshIndirect.html

//         - full gpu without args and fixed instances count
//         https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshPrimitives.html
// */

using UnityEngine;
// using System;
using UnityEditor;


namespace Custom.Particles
{
    using System.Collections.Generic;
    using PlaneField;

    public class PlaneFieldSystem : ParticlesSystem
    {
        // theses are constants to be imported from mono script in inspector
        [HideInInspector] public ComputeShader compute;
        public Texture2D noiseTex; // common hash ?

        [SerializeField] private PlaneFieldSimulation simulation;
        public override ParticlesSimulation Simulation{get=>simulation;}

        [SerializeField] new private PlaneFieldRenderer renderer;
        public override ParticlesRenderer Renderer{get=>renderer;}

        private void OnEnable() { Init(); }
        private void OnDisable(){ Dispose();}

        private void Init()
        {
            ParticlesSceneObjects scene = GetParticlesSceneObjects();
            simulation.Init(this, scene);
            renderer.Init(this, scene);
        }

        private void FixedUpdate()
        {
            simulation.Run();
            renderer.Update(simulation);
        }

        private void Update()
        {
            renderer.Draw(simulation.MaxCount);
        }

        //---------------------------------------------------------------------------------------
        [ContextMenu("Recreate serialized vectors")]
        private void RecreateSerialized()
        {
            simulation.RecreateSerializedVectors();
            
            renderer.RecreateSerializedVectors();
            renderer.RecreateSerializedTextures();

            AssetDatabase.ForceReserializeAssets(new List<string>(){AssetDatabase.GetAssetPath(this)});
        }

        [ContextMenu("Log Renderer Enum")]
        private void LogRendererEnum()
        {
            string sb = "--- Renderer Modes Enum ---";
            for(int val = 0; val <= 64; val++ ) sb += "\n"+ string.Format("{0,3} - {1:G}", val, (RendererModes)val);
            Debug.Log(sb);
        }
    }
}
