/*
    https://docs.unity3d.com/ScriptReference/Texture2D.PackTextures.html

*/
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

using System;

using Custom.Generators.Modules;
using UnityEditor;
namespace Custom.Generators.Makers
{
    [ExecuteInEditMode]
    [CreateAssetMenu(fileName = "MeshMaker", menuName = "ScriptableObjects/Generators/MeshMaker", order = 3)]
    public class MeshMaker : Maker, IGenerator
    {
        public int numVertX = 16;
        public int numVertY = 16;

        public bool setUvs = true;
        public bool setNormals = true;

        public void Generate()
        {
            Mesh mesh = MeshUtils.CreateSubdivideQuad(numVertX, numVertY, setUvs, setNormals);
            MeshUtils.SaveMeshAsset(mesh, GetPathFromObject(this, "_SubPlane_" + numVertX.ToString() + "X" + numVertY.ToString()));
        }
    }
}
#endif

