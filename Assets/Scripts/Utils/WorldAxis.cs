using UnityEngine;

//YO YO
public class WorldAxis : MonoBehaviour
{
    public float size = 10.0f;

    void OnDrawGizmos ()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.right * size, Vector3.zero);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(Vector3.up * size, Vector3.zero);


        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.forward * size, Vector3.zero);
    }
}