using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowManager : MonoBehaviour 
{
    private static int[] prevDim;

    public delegate void WindowResizeEventHandler(int width, int height);
    public event WindowResizeEventHandler OnWindowResize;
    
    void Awake() 
    {
        prevDim = new int[2]{Screen.width,Screen.height};
    }

    void Update()
    {
        if( (prevDim[0] != Screen.width) || (prevDim[1] != Screen.height) ) 
        {
            prevDim = new int[2]{Screen.width,Screen.height};
            OnWindowResize?.DynamicInvoke(Screen.width, Screen.height);
        }
    }
}
