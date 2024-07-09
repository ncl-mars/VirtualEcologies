using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEditor;

namespace Custom.Generators.Makers
{
    public enum ExportType
    {
        Texture2D           = 0,
        Texture2DArray      = 1,
        Texture3D           = 2,
        Atlas               = 3,
    };

#if UNITY_EDITOR
    public abstract class Maker : ScriptableObject
    {
        protected string GetPathFromObject(UnityEngine.Object tex, string suffix)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            
            path = Path.Combine(
                Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)), // directory is where the scriptable instance is
                Path.GetFileNameWithoutExtension(path)
            );

            path += suffix;
            return path;
        }
    }
#endif

}