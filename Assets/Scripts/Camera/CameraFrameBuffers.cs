/*
    https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/DefaultResourcesExtra/Compositing.shader
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CameraFrameBuffers : MonoBehaviour
{
    private Camera cam;
    private RenderTexture target;
    private Dictionary<CameraFrameBufferObject, List<object>> frameBuffers;

    public Material compositor; // compositor, merger

    [SerializeField] private bool active = true;
    public bool Active{get=>active;}


    private void Awake()
    {
        enabled = false;
    }

    private void Init()
    {
        cam = GetComponent<Camera>();

        ReinitializeTarget();

        frameBuffers = new();
        enabled = true;

        WindowManager wm = gameObject.AddComponent<WindowManager>();
        wm.OnWindowResize += OnWindowResize;
    }

    private void ReinitializeTarget()
    {
        target = new RenderTexture( cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGB32);
        compositor.SetTexture(Shader.PropertyToID("_SecondTex"), target);
    }

    private void Dispose()
    {
        frameBuffers = null;
        enabled = false;
    }

    //--------------------------------------------------------------------------- Rendering
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        foreach(CameraFrameBufferObject fbo in frameBuffers.Keys)
        {
            Graphics.Blit(source, target);   
            Graphics.Blit(fbo.Render(), source, compositor);
        }

        Graphics.Blit(source, destination);
    }

    private void OnWindowResize(int width, int height) // Call this with a proper event !!
    {
        ReinitializeTarget();
        
        // Debug.LogFormat("Resizing window with width : {0}, height : {1}", width, height);
        foreach(CameraFrameBufferObject fbo in frameBuffers.Keys)
        {
            fbo.ReinitializeTargets(cam);
        }
    }

    //--------------------------------------------------------------------------- Registrations
    public void RegisterFrameBuffer(CameraFrameBufferObject fbo, object user)
    {
        if(frameBuffers == null) Init();

        if(frameBuffers.ContainsKey(fbo))
        {
            if(frameBuffers[fbo].Contains(user))return;
            else frameBuffers[fbo].Add(user);
        }
        else
        {
            frameBuffers.Add(fbo, new(){user});
            fbo.Init(cam);
        }
    }

    public void UnregisterFrameBuffer(CameraFrameBufferObject fbo, object user)
    {
        if(frameBuffers == null) return;
        
        if(frameBuffers.ContainsKey(fbo))
        {
            if(frameBuffers[fbo].Contains(user))
            {
                frameBuffers[fbo].Remove(user);
                if(frameBuffers[fbo].Count == 0) frameBuffers.Remove(fbo);
            }
        }
        // if(frameBuffers.Count == 0) Dispose();
    }
}
