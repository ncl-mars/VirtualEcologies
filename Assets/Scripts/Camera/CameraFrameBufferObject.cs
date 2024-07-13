using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//////////////////////////////////////////////////////////////////////////
[CreateAssetMenu(fileName = "CameraFrameBufferObject", menuName = "ScriptableObjects/Camera/FrameBuffer", order = 1)]
public class CameraFrameBufferObject : ScriptableObject
{
    private Camera cam;
    public Camera Cam{get=>cam;} 

    public Material material; // frame buffer material
    [SerializeField] private Texture2D noise;

    // [SerializeField] private Vector4 uvb;
    [SerializeField] [Range(0.0f, 1.0f)] private float mixBuffer = 0.5f;

    [SerializeField] private int layer = 3;
    public int Layer{get=>layer;}

    private RenderTexture target;


    public RenderTexture Render()
    {
        cam.Render();
        Graphics.Blit(cam.activeTexture, target);
        Graphics.Blit(target, cam.activeTexture, material);
        return target;
    }

    private void OnValidate()
    {
        material.SetFloat(Shader.PropertyToID("_MixBuffer"), mixBuffer);
    }

    public void Init(Camera rootCamera)
    {
        GameObject camObj = new("Frame Buffer Camera");

        material.SetFloat(Shader.PropertyToID("_MixBuffer"), mixBuffer);

        cam = camObj.AddComponent<Camera>();
        camObj.transform.parent = rootCamera.transform;

        cam.CopyFrom(rootCamera);

        cam.clearFlags = CameraClearFlags.Nothing;
        cam.cullingMask = 1 << layer; 
        cam.enabled = false;

        CreateTargets(rootCamera);
    }

    public void CreateTargets(Camera rootCamera)
    {
        cam.targetTexture = new RenderTexture(rootCamera.pixelWidth, rootCamera.pixelHeight, 0, RenderTextureFormat.ARGB32);
        target = new(cam.targetTexture);
        target.Create();
    }

    public void ReinitializeTargets(Camera rootCamera)
    {
        cam.targetTexture.Release();
        target.Release();

        CreateTargets(rootCamera);
    }
}







        // material.SetTexture(Shader.PropertyToID("_BufferTex"), target);
        // material.SetTexture(Shader.PropertyToID("_BufferTex"), target);

    // // set layer masks here
    // public void AddLayerMask(LayerMask layerMask)
    // {
    //     // cam.cullingMask = (1 << layerMask) | cam.cullingMask;
    // }