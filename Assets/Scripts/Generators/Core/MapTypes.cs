/*
--- MapType Enum ---
  0 - 0
  1 - Color
  2 - Plateform
  3 - Color, Plateform
  4 - HighPath
  5 - Color, HighPath
  6 - Plateform, HighPath
  7 - Color, Plateform, HighPath
  8 - FlatPath
  9 - Color, FlatPath
 10 - Plateform, FlatPath
 11 - Color, Plateform, FlatPath
 12 - HighPath, FlatPath
 13 - Color, HighPath, FlatPath
 14 - Plateform, HighPath, FlatPath
 15 - Color, Plateform, HighPath, FlatPath
 16 - TopoGravity
 17 - Color, TopoGravity
 18 - Plateform, TopoGravity
 19 - Color, Plateform, TopoGravity
 20 - HighPath, TopoGravity
 21 - Color, HighPath, TopoGravity
 22 - Plateform, HighPath, TopoGravity
 23 - Color, Plateform, HighPath, TopoGravity
 24 - FlatPath, TopoGravity
 25 - Color, FlatPath, TopoGravity
 26 - Plateform, FlatPath, TopoGravity
 27 - Color, Plateform, FlatPath, TopoGravity
 28 - HighPath, FlatPath, TopoGravity
 29 - Color, HighPath, FlatPath, TopoGravity
 30 - Plateform, HighPath, FlatPath, TopoGravity
 31 - Color, Plateform, HighPath, FlatPath, TopoGravity
*/

using UnityEngine;

using System;

namespace Custom
{
    [Flags] // max = long : 64, 6 modes total
    public enum MapType : short
    {
        Color = 1,
        Plateform = 2,
        HighPath = 4,
        FlatPath = 8,
        Topography = 16,

        // ... = [32, 64, ...]
    }

    public interface IGenerator
    {
        public void Generate();
    }

    //-----------------------------------------------------------------------------------
    public static class MapTypeExtensions
    {
        public static int GetIndex<T>(this T value) where T : Enum
        {
            return Array.IndexOf(Enum.GetValues(typeof(T)), value);
        }
        public static T GetFlag<T>(this int index) where T : Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            return  values[index];
        }

        public static TextureFormat ToTextureFormat(this MapType mapType, bool half = false)
        {
            return mapType switch
            {
                MapType.Color           => TextureFormat.RGBA32,

                // MapType.Plateform       => half ? TextureFormat.RGHalf : TextureFormat.RGFloat,
                MapType.Plateform       => half ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat,
                
                MapType.FlatPath        => half ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat,
                MapType.HighPath        => half ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat,
                MapType.Topography      => half ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat,
                
                _ => TextureFormat.RGBA32,
            };
        }

        public static T ToEnum<T>(this string value, T defaultValue) where T : struct 
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return Enum.TryParse(value, out T result) ? result : defaultValue;
        }
    }

}