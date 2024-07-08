using UnityEngine;
using Custom;
using System;


namespace Custom.Particles
{
    [ExecuteInEditMode]
    public class ParticlesForceField : MonoBehaviour
    {
        public delegate void ChangeEventHandler(ParticlesForceField field, int index);
        public event ChangeEventHandler OnFieldChanged;

        [SerializeField] private int fieldTextureID;
        public int FieldTextureID{get => fieldTextureID; set => fieldTextureID = value;}

        public int BufferIndex{get;set;}


        protected virtual void Update()
        {
            if(transform.hasChanged)
            {
                transform.localScale = new Vector3(transform.localScale.y, transform.localScale.y, transform.localScale.z);
                transform.hasChanged = false;
                OnFieldChanged?.DynamicInvoke(this, BufferIndex);
            }
        }

        private void OnValidate()
        {
            OnFieldChanged?.DynamicInvoke(this, BufferIndex);
        }

        protected virtual void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            GizmosUtils.DrawGizmosMatrix(0.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}

