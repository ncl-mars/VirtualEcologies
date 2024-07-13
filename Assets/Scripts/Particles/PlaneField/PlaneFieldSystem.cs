/*
*/

using UnityEngine;
using UnityEditor;
// using System;

namespace Custom.Particles
{
    using System.Collections.Generic;
    using PlaneField;

    public class PlaneFieldSystem : ParticlesSystem
    {
        // theses are constants to be imported from mono script in inspector
        [HideInInspector] public Shader simulationShader;
        [HideInInspector] public Shader rendererShader;
        
        public Texture2D noiseTex; // common hash ?

        [SerializeField] private PlaneFieldSimulation simulation;
        public override ParticlesSimulation Simulation{get=>simulation;}

        [SerializeField] new private PlaneFieldRenderer renderer;
        public override ParticlesRenderer Renderer{get=>renderer;}

        private void Start() { Init(); }
        private void OnDestroy(){ Dispose();}

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


#if UNITY_EDITOR
        //---------------------------------------------------------------------------------------
        [ContextMenu("Recreate serialized vectors")]
        private void RecreateSerialized()
        {
            // simulation.RecreateSerializedVectors();
            // renderer.RecreateSerializedVectors();
            // renderer.RecreateSerializedTextures();

            AssetDatabase.ForceReserializeAssets(new List<string>(){AssetDatabase.GetAssetPath(this)});
        }

        [ContextMenu("Log Renderer Enum")]
        private void LogRendererEnum()
        {
            string sb = "--- Renderer Modes Enum ---";
            for(int val = 0; val <= 64; val++ ) sb += "\n"+ string.Format("{0,3} - {1:G}", val, (RendererModes)val);
            Debug.Log(sb);
        }
#endif
    }
}
