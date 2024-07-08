using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;


// https://forum.unity.com/threads/save-rendertexture-or-texture2d-as-image-file-utility.1325130/
namespace Custom
{
    // unity didn't bother to add the type but allows it for SetPixelData()...
    public struct HalfVector2
    {
        public ushort x;
        public ushort y;
    }

    //-- utilitary methods for compute shader
    public static class ComputeUtils
    {
        // get counter value, incremented by the GPU, thanks to the AppendStructuredBuffer type
        public static int GetAppendCount(ComputeBuffer appendBuffer) 
        {
            ComputeBuffer countBuffer = new(1, sizeof(int), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(appendBuffer, countBuffer, 0);

            int[] counter = new int[1] { 0 };
            countBuffer.GetData(counter);
            countBuffer.Release();
            countBuffer.Dispose();
            return counter[0];
        }

        // get the number of blocks to chain up
        public static int Get1DWarpCount(ComputeShader compute, int kernel, int bufferLength)
        {
            uint[] grSize = new uint[3];
            compute.GetKernelThreadGroupSizes(kernel, out grSize[0], out grSize[1], out grSize[2]);
            return Mathf.CeilToInt(bufferLength / (float)grSize[0]);
        }

        // upload buffers to the GPU
        public static void SendBuffers(ComputeShader compute, int kernel, int[] ids, ComputeBuffer[] buffers)
        {
            for(int b = 0; b < buffers.Length; b++) compute.SetBuffer(kernel, ids[b], buffers[b]);
        }

        // Release and destroy the buffers
        public static void DisposeBuffers(ComputeBuffer[] buffers)
        {
            for(int b = 0; b < buffers.Length; b++){
                buffers[b].Release();
                buffers[b].Dispose();
            }
        }

        public static void Set2DWarpCount(ref int[] warpCnt, int width, int height, uint numThrX, uint numThrY)
        {
            warpCnt[0] = Mathf.CeilToInt(width / (float)numThrX);
            warpCnt[1] = Mathf.CeilToInt(height /(float)numThrY);
        }
    }

    public static class ArrayTools
    {
        //-- Flatten jagged arrays -> full or half
        public static T[] GetFlattenArray<T>(float[][] values)
        {
            T[] flatten = new T[values.Length * values[0].Length];
            
            if(typeof(T)==typeof(ushort))
            for(int a = 0; a < values.Length; a++)Array.Copy(GetHalfArray(values[a]), 0, flatten, a*values[0].Length,values[a].Length);
            
            else if (typeof(T)==typeof(float))
            for(int a = 0; a < values.Length; a++)Array.Copy(values[a], 0, flatten, a*values[0].Length,values[a].Length);

            else Debug.Log("get flatten result can only work for float or ushort types");

            return flatten;
        }

        public static T[] GetFlattenArray<T>(Vector2[][] vectors)
        {
            T[] flatten = new T[vectors.Length * vectors[0].Length];
            
            if(typeof(T)==typeof(HalfVector2))
            for(int a = 0; a < vectors.Length; a++)Array.Copy(GetHalfArray(vectors[a]), 0, flatten, a*vectors[0].Length,vectors[a].Length);
            
            else if (typeof(T)==typeof(Vector2))
            for(int a = 0; a < vectors.Length; a++)Array.Copy(vectors[a], 0, flatten, a*vectors[0].Length,vectors[a].Length);

            else Debug.Log("get flatten result can only work for float or ushort types");

            return flatten;
        }

        //-- convert float to half arrays
        public static ushort[] GetHalfArray(float[] result)
        {
            ushort[] hResult = new ushort[result.Length];
            for(int r = 0; r < result.Length; r++) hResult[r] = Mathf.FloatToHalf(result[r]);
            return hResult;
        }

        public static HalfVector2[] GetHalfArray(Vector2[] result)
        {
            HalfVector2[] hResult = new HalfVector2[result.Length];

            for(int r = 0; r < result.Length; r++) 
            {
                hResult[r] = new HalfVector2
                {
                    x = Mathf.FloatToHalf(result[r].x),
                    y = Mathf.FloatToHalf(result[r].y)
                };
            }
            return hResult;
        }
    }

    public static class SdfTools
    {
        public static bool TryGetFirstInParents<T>(Component child, out T comp, bool includeInactive = false) where T:Component
        {
            T[] comps = child.GetComponentsInParent<T>(includeInactive);
            
            if(comps.Length > 0)
            {
                comp = comps[0];
                return true;
            }
            else
            { 
                comp = null;
                return false;
            }
        }

        public static bool TryGetFirstInChilren<T>(Component parent, out T comp, bool includeInactive = false) where T:Component
        {
            T[] comps = parent.GetComponentsInChildren<T>(includeInactive);
            
            if(comps.Length > 0)
            {
                comp = comps[0];
                return true;
            }
            else
            { 
                comp = null;
                return false;
            }
        }
    }

    // there is surely a way to factorize all this... at least at texture creation
    public static class TexUtils
    {
        public static void SaveTextureAsset(Texture tex, string pathWithoutExt)
        {
            string path = pathWithoutExt + ".asset";

            // AssetDatabase.CreateAsset(tex,  path);
            // AssetDatabase.Refresh();
            // Debug.Log("texture asset saved at path : " + path);

            
            Texture existing = AssetDatabase.LoadAssetAtPath<Texture>(path);
            // edit the asset however
            if(existing != null)
            {
                Debug.LogFormat("Overwrite texture asset at path : {0}", path);
                tex.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.CopySerialized(tex, existing);
            }
            else
            {
                Debug.Log("Create texture asset at path : " + path);
                AssetDatabase.CreateAsset(tex,  path);
                AssetDatabase.Refresh();
            }
        }

        public static Texture2D CreateTex2D(float[] values, int width, int height, bool half = false,
        FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2D tex = new( 
            width, height,
            half ? TextureFormat.RHalf : TextureFormat.RFloat,
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            if(half) tex.SetPixelData(ArrayTools.GetHalfArray(values),0,0);
            else tex.SetPixelData(values,0,0);
            tex.Apply();
            
            return tex;
        }

        public static Texture2D CreateTex2D(Vector2[] vectors, int width, int height, bool half, 
        FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2D tex = new( 
            width, height,
            half ? TextureFormat.RGHalf : TextureFormat.RGFloat,
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            if(half) tex.SetPixelData(ArrayTools.GetHalfArray(vectors),0,0);
            else tex.SetPixelData(vectors,0,0);
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateTex2D(Color[] colors, int width, int height,
        TextureFormat format = TextureFormat.RGBA32, FilterMode filter = FilterMode.Point, 
        TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2D tex = new(width, height, format, false)
            {
                filterMode = filter,
                wrapMode = wrap,
            };

            // yes setPixels is slower but it handles more conversions
            tex.SetPixels(colors, 0);
            tex.Apply();
            return tex;
        }


        public static Texture2DArray CreateTexArray(float[][] values, int width, int height, bool half,
        FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2DArray tex = new(   
            width, height,
            values.Length,
            half ? TextureFormat.RHalf : TextureFormat.RFloat, 
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            for(int t = 0; t < values.Length; t++){
                if(half) tex.SetPixelData(ArrayTools.GetHalfArray(values[t]), 0, t, 0);
                else tex.SetPixelData(values[t], 0, t, 0);
            }
            tex.Apply();
            return tex;
        }

        public static Texture2DArray CreateTexArray(Vector2[][] values, int width, int height, bool half,
        FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2DArray tex = new(   
            width, height, 
            values.Length,
            half ? TextureFormat.RGHalf : TextureFormat.RGFloat, 
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            for(int t = 0; t < values.Length; t++){
                if(half) tex.SetPixelData(ArrayTools.GetHalfArray(values[t]), 0, t, 0);
                else tex.SetPixelData(values[t], 0, t, 0);
            }
            tex.Apply();
            return tex;
        }

        public static Texture2DArray CreateTexArray(Color[][] colors, int width, int height,
        TextureFormat format = TextureFormat.RGBA32, FilterMode filter = FilterMode.Point, 
        TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture2DArray tex = new(   
            width, height, 
            colors.Length,
            format,
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            // yes setPixels is slower but it handles more conversions
            for(int t = 0; t < colors.Length; t++) tex.SetPixels(colors[t], t, 0);
            tex.Apply();
            return tex;
        }


        public static Texture3D CreateTex3D(float[][]values, int width, int height, bool half,
        FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture3D tex = new(   
            width, height,
            values.Length,
            half ? TextureFormat.RHalf : TextureFormat.RFloat, 
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            // flatten arrays
            if(half) tex.SetPixelData(ArrayTools.GetFlattenArray<ushort>(values), 0, 0);
            else tex.SetPixelData(ArrayTools.GetFlattenArray<float>(values), 0, 0);

            tex.Apply();
            return tex;
        }

        public static Texture3D CreateTex3D(float[]values, int width, int height, int depth, bool half,
        FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture3D tex = new(   
            width, height, depth,
            half ? TextureFormat.RHalf : TextureFormat.RFloat, 
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            // flatten arrays
            if(half) tex.SetPixelData(ArrayTools.GetHalfArray(values), 0, 0);
            else tex.SetPixelData(values, 0, 0);

            tex.Apply();
            return tex;
        }

        public static Texture3D CreateTex3D(Vector2[][]values, int width, int height, bool half,
        FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            Texture3D tex = new(   
            width, height,
            values.Length,
            half ? TextureFormat.RGHalf : TextureFormat.RGFloat, 
            false){
                filterMode = filter,
                wrapMode = wrap,
            };

            // flatten arrays
            if(half) tex.SetPixelData(ArrayTools.GetFlattenArray<HalfVector2>(values), 0, 0);
            else tex.SetPixelData(ArrayTools.GetFlattenArray<Vector2>(values), 0, 0);

            tex.Apply();
            return tex;
        }

        //------------------------------------------------------------------------------------------- ARRAYS
        public static Texture2DArray RemoveSlice<T>(ref Texture2DArray tex, int slice, bool mipChain = false) where T:struct
        {
            Texture2DArray ntex = new(tex.width, tex.height, tex.depth - 1, tex.format, mipChain);

            int curSlice = 0;
            for(int s = 0; s < tex.depth; s++)
            {
                if(s == slice)continue;

                ntex.SetPixelData(tex.GetPixelData<T>(0,s), 0, curSlice, 0);
                curSlice++;
            }
            ntex.Apply();

            return ntex;
        }

        public static Texture2DArray WriteSlice(ref Texture2DArray tex, float[] values, int slice, bool half = false)
        {
            if(half) tex.SetPixelData(ArrayTools.GetHalfArray(values), 0, slice, 0);
            else tex.SetPixelData(values, 0, slice, 0);
            tex.Apply();
            return tex;
        }

        public static Texture2DArray AddSlice(ref Texture2DArray tex, float[] values, bool half = false)
        {
            Texture2DArray texture = new(tex.width, tex.height, tex.depth + 1, tex.format, false);
            
            for(int i = 0; i < tex.depth; i++) {
                if(half)texture.SetPixelData(tex.GetPixelData<short>(0,i), 0, i, 0);
                else texture.SetPixelData(tex.GetPixelData<float>(0,i), 0, i, 0);
            }

            if(half) texture.SetPixelData(ArrayTools.GetHalfArray(values), 0, tex.depth, 0);
            else texture.SetPixelData(values, 0, tex.depth, 0);

            texture.Apply();

            return texture;
        }

        public static Texture2DArray AddSlice(ref Texture2DArray tex, Color[] colors)
        {
            Texture2DArray texture = new(tex.width, tex.height, tex.depth + 1, tex.format, false);
            
            for(int i = 0; i < tex.depth; i++) texture.SetPixels(tex.GetPixels(i), i, 0);

            texture.SetPixels(colors, tex.depth, 0);
            texture.Apply();

            return texture;
        }

        public static Texture2DArray WriteSlice(ref Texture2DArray tex, Color[] colors, int slice)
        {
            tex.SetPixels(colors, slice, 0);
            tex.Apply();
            return tex;
        }

    }

    public static class MeshUtils
    {
        public static Mesh CreateQuad(float width = 1, float height = 1)
        {
            Mesh mesh = new();

            Vector3[] vertices = new Vector3[4]
            {
                new(-width/2, -height/2, 0),
                new(width/2, -height/2, 0),
                new(-width/2, height/2, 0),
                new(width/2, height/2, 0)
            };

            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                0, 2, 1,    // lower left triangle
                2, 3, 1     // upper right triangle
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                new(0, 0),
                new(1, 0),
                new(0, 1),
                new(1, 1)
            };
            mesh.uv = uv;

            return mesh;
        }

        //https://discussions.unity.com/t/how-to-generate-a-subdivided-plane-mesh/247495
        public static Mesh CreateSubdivideQuad ( int numVertX = 16 , int numVertY = 16 )
        {
            int vertCount = numVertX * numVertY;

            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            
            List<int> indices = new();

            for( int y=0 ; y<numVertY ; y++ )
            for( int x=0 ; x<numVertX ; x++ )
            {
                int index = x + y * numVertX;

                float tx = x / (float)(numVertX - 1);
                float ty = y / (float)(numVertX - 1);

                vertices[index] =  new Vector3(-0.5f+tx,-0.5f+ty,0);
                normals[index] = -Vector3.forward;

                uvs[index] = new Vector2(tx,ty);

                if( (x<numVertX-1) && (y<numVertY-1) )
                {
                    indices.Add(index);
                    indices.Add(index+numVertX);
                    indices.Add(index+numVertX+1);
                    indices.Add(index);
                    indices.Add(index+numVertX+1);
                    indices.Add(index+1);
                }
            }
            
            var mesh = new Mesh();
            mesh.name = $"SubDivQuad {mesh.GetHashCode()}";
            mesh.vertices = vertices;
            mesh.triangles = indices.ToArray();
            mesh.uv = uvs;
            mesh.normals = normals;

            return mesh;
        }
    }

    public static class VectorUtils
    {
        public static Vector3 Random3(float min = -1, float max = 1)
        {
            Vector3 rand = new(
                UnityEngine.Random.Range(min, max),
                UnityEngine.Random.Range(min, max),
                UnityEngine.Random.Range(min, max)
            );
            return rand;
        }

        public static float MinComp(Vector3 vec)
        {
            return Mathf.Min(Mathf.Min(vec.x, vec.y), vec.z);
        }
    }

    public static class GizmosUtils
    {
        public static void DrawGizmosMatrix(float size = 1)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, Vector3.right * size);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, Vector3.up * size);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * size);
        }

        public static void DrawGizmosBounds(Transform transform)
        {
            Gizmos.matrix *= Matrix4x4.Translate(transform.position);
            Gizmos.matrix *= Matrix4x4.Scale(transform.lossyScale);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }

    
    // public class CPUHashes
    // {
    //     public static Vector3 Hash31( float p ) 
    //     {
    //         Vector3 p3 = Frac(p) * new Vector3(.1031f, .1030f, .0973f);
    //         p3 += Vector3.one*Vector3.Dot(p3, new Vector3(p3.y, p3.z, p3.x)+Vector3.one*33.33f);

    //         return new Vector3( 
    //             Frac((p3.x+p3.y)*p3.z), 
    //             Frac((p3.x+p3.z)*p3.y), 
    //             Frac((p3.y+p3.z)*p3.x));
    //     }

    //     private static float Frac(float p)
    //     {
    //         return p - MathF.Floor(p);
    //     }
    // }
}

