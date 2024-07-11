using UnityEngine;
using System;


namespace Custom.Particles
{
    ///////////////////////////////////////////////////////////////////////////////////
    public class ParticlesSceneObjects
    {
        public ParticlesDomain domain;
        public ParticlesForceField[] fields;
        public ParticlesEmitter[] emitters;
    }


    ///////////////////////////////////////////////////////////////////////////////////
    public abstract class ParticlesSimulation
    {
        protected Material material;
        [SerializeField] protected Vector4[] uvb; // all params encapsulated in a vector array

        //--------------------------------------------------------
        public virtual int MaxCount     { get => (int)uvb[0].x; set => uvb[0].x = value;}
        public virtual Vector4 Origin   { get => uvb[2]; } // domain center
        public virtual Vector4 Extents  { get => uvb[3]; } // domain Extents
        public virtual Transform DomainTransform
        {
            set 
            {
                for(int c = 0; c < 3; c++)
                {
                    uvb[2][c] = value.position[c];
                    uvb[3][c] = value.lossyScale[c] * 0.5f;
                };
            }
        }
    

        public abstract RenderTexture[] Buffers{get;}
        public Vector4[] UVB{get => uvb; set => uvb = value;}

        public abstract void Dispose();
    }

    ///////////////////////////////////////////////////////////////////////////////////
    public abstract class ParticlesRenderer
    {
        // public Camera netCam;
        protected Material material;
        [SerializeField] protected Vector4[] uvb;
        
        public abstract void Dispose();

        public virtual Vector4 Origin   { get => uvb[1]; } // domain center
        public virtual Vector4 Extents  { get => uvb[2]; } // domain Extents
        public virtual Transform DomainTransform
        {
            set 
            {
                for(int c = 0; c < 3; c++)
                {
                    uvb[1][c] = value.position[c];
                    uvb[2][c] = value.lossyScale[c] * 0.5f;
                };
            }
        }

        public Vector4[] UVB{get => uvb; set => uvb = value;}
    }

    ///////////////////////////////////////////////////////////////////////////////////
    public abstract class ParticlesSystem : MonoBehaviour
    {
        public abstract ParticlesSimulation Simulation{get;}
        public abstract ParticlesRenderer Renderer{get;}

        public T[] CheckCreateDefault<T>(bool includeInactive = true, string name = "Default", float scale = 1) where T : Component
        {
            T[] obj = GetComponentsInChildren<T>(includeInactive);
            if (obj.Length > 0) return obj;
            else return new T [] {CreateDefault<T>(name, scale)};
        }

        public T CreateDefault<T>(string name, float scale) where T : Component
        {
            T obj = new GameObject(name).AddComponent<T>();
            obj.transform.parent = transform;
            obj.transform.localScale = Vector3.one * scale;
            return obj;
        }

        public ParticlesSceneObjects GetParticlesSceneObjects()
        {
            return new(){
                domain = CheckCreateDefault<ParticlesDomain>(true, "Default Domain")[0],
                fields = CheckCreateDefault<ParticlesForceField>(false, "Default Field", 0.5f),
                emitters = CheckCreateDefault<ParticlesEmitter>(false, "Default Emit",0.1f)
            };
        }

        protected virtual void Dispose()
        {
            Simulation.Dispose();
            Renderer.Dispose();
        }
    }

}
