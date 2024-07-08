using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Custom.Particles
{
    public class ParticlesEmitter : MonoBehaviour
    {
        public delegate void ChangeEventHandler(ParticlesEmitter field, int index);
        public event ChangeEventHandler OnEmitterChange;

        public int BufferIndex{get;set;}


        protected virtual void Update()
        {
            if(transform.hasChanged)
            {
                // transform.localScale = new Vector3(transform.localScale.y, transform.localScale.y, transform.localScale.z);
                transform.hasChanged = false;
                OnEmitterChange?.DynamicInvoke(this, BufferIndex);
            }
        }

        private void OnValidate()
        {
            // OnEmitterChange?.DynamicInvoke(this, BufferIndex);
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

        
        // private void Update()
        // {
        //     if(transform.hasChanged)
        //     {
        //         transform.hasChanged = false;
        //     }
        // }
    }
}
