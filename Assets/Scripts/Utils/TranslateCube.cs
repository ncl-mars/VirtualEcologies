using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslateCube : MonoBehaviour
{
    int incr = 0;

    public float scale = 10;
    public float speed = 0.1f;
    // Update is called once per frame
    void FixedUpdate()
    {
        float y = scale;

        y -= (incr * speed) % (scale * 2);

        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        
        incr++;
    }
}
