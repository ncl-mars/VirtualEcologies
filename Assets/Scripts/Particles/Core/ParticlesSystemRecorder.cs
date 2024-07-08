using System;
using UnityEngine;

namespace Custom.Particles
{
    [ExecuteInEditMode]
    public class ParticlesSystemRecorder : MonoBehaviour
    {
        [SerializeField] private ParticlesSystemData dataBase;

        private void OnEnable()
        {
            if(dataBase == null) return;

            if(TryGetComponent(out ParticlesSystem system))
            {
                dataBase.ReadSystem(this, system);
            }
        }

        private void OnDestroy()
        {
            if(dataBase == null) return;

            if(TryGetComponent(out ParticlesSystem system))
            {
                dataBase.WriteSystem(this, system);
            }
        }
    }
}
