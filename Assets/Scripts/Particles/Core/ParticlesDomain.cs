using UnityEngine;

namespace Custom.Particles
{
    public class ParticlesDomain : MonoBehaviour
    {
        public bool keepDraw = true;
        
        public void OnDrawGizmosSelected()
        { 
            GizmosUtils.DrawGizmosBounds(transform);
        }

        public void OnDrawGizmos()
        {
            if(keepDraw)
            {
                GizmosUtils.DrawGizmosBounds(transform);
            }
        }
    }
}
