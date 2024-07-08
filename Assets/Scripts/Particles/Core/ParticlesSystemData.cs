using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace Custom.Particles
{
    [CreateAssetMenu(fileName = "ParticlesData", menuName = "ScriptableObjects/Particles/System Data", order = 1)]
    public class ParticlesSystemData : ScriptableObject
    {
        [Serializable] public class TransformData
        {
            [SerializeField] private Vector3[][] v_transforms;

            public void SetTransformID(Transform transform, int id)
            {
                transform.SetPositionAndRotation(v_transforms[id][0], Quaternion.Euler(v_transforms[id][1]));
                transform.localScale = v_transforms[id][2];
            }

            public int Length
            {
                get
                {
                    if(v_transforms == null) return 0;
                    else return v_transforms.Length;
                }
            }
            
            public TransformData(Component[] components)
            {
                v_transforms = new Vector3[components.Length][];

                for(int i = 0; i < components.Length; i++)
                {
                    v_transforms[i]    = new Vector3[3];
                    v_transforms[i][0] = components[i].transform.position;
                    v_transforms[i][1] = components[i].transform.rotation.eulerAngles;
                    v_transforms[i][2] = components[i].transform.localScale;
                }
            }
        }

        [Serializable] private class SystemData
        {
            [SerializeField] private TransformData fieldTransforms;
            [SerializeField] private TransformData emitterTransforms;

            [SerializeField] private Vector4[][] vectors;

            public SystemData(ParticlesSystem system)
            {
                ParticlesSceneObjects objects = system.GetParticlesSceneObjects();
                fieldTransforms = new(objects.fields);
                emitterTransforms = new(objects.emitters);

                vectors = new Vector4[2][];
                vectors[0] = system.Simulation.UVB;
                vectors[1] = system.Renderer.UVB;
            }

            public void TransferData(ParticlesSystem system)
            {
                if( fieldTransforms == null) return;

                ParticlesSceneObjects objects = system.GetParticlesSceneObjects();

                for(int i = 0; i < objects.fields.Length; i++)
                {
                    if(i >= fieldTransforms.Length)return;
                    fieldTransforms.SetTransformID(objects.fields[i].transform, i);
                }

                for(int i = 0; i < objects.emitters.Length; i++)
                {
                    if(i >= emitterTransforms.Length)return;
                    emitterTransforms.SetTransformID(objects.emitters[i].transform, i);
                }

                system.Simulation.UVB = vectors[0];
                system.Renderer.UVB = vectors[1];

                if(PrefabUtility.IsPartOfVariantPrefab(system))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(system);
                }
            }
        }

        private Dictionary<ParticlesSystemRecorder,SystemData> dataRegister;
   
        public void ReadSystem(ParticlesSystemRecorder recorder, ParticlesSystem system)
        {
            if(dataRegister == null) return;

            if(dataRegister.ContainsKey(recorder))
            {
                dataRegister[recorder].TransferData(system);
                dataRegister.Remove(recorder);
            }
        }

        public void WriteSystem(ParticlesSystemRecorder recorder, ParticlesSystem system)
        {
            // Debug.Log("writing system : " + system.name);
            dataRegister ??= new();

            if(!dataRegister.ContainsKey(recorder))
            {
                dataRegister.Add(recorder, new(system));
            }
        }
    }
}
