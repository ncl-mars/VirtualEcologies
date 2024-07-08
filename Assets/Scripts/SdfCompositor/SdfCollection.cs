/*
    Helper class for the compositor texture arrays
    DESTROYED AT RUNTIME

    This class check for texture changes, and recreate texture array if needed
    Texture array are holded by the compositors

    https://learn.microsoft.com/fr-fr/dotnet/standard/design-guidelines/choosing-between-class-and-struct

*/
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Custom;
using Custom.Generators.Modules;

namespace Custom.Compositor
{
    //////////////////////////////////////////////////////////////////////////////
    [ExecuteInEditMode]
    public class SdfCollection : MonoBehaviour
    {
        [SerializeField] private Vector2Int targetResolution = new(512,512);

        private Dictionary<SdfTextureType, SdfTextureRegistration[]> texRegister;

        //////////////////////////////////////////////////////////////////////////////
        public class SdfTextureRegistration
        {
            public Texture texture;
            public List<SdfElement> users;

            public SdfTextureRegistration(Texture tex, List<SdfElement> elements){texture = tex; users = elements;}
            public SdfTextureRegistration(Texture tex, SdfElement element){texture = tex; users = new List<SdfElement>(){element};}
        }

        private enum SdfBuildType
        {
            Create      = 0,
            Replace     = 1,
            Add         = 2,
            Remove      = 3,
        }

        private class SdfBuildArgs
        {
            public SdfTextureType typeToBuild;
            public SdfBuildType type;
            public int slice;

            public SdfBuildArgs(){}
            public SdfBuildArgs(SdfTextureType tb, SdfBuildType bt, int slc){typeToBuild = tb; type = bt; slice = slc;}
        }

        //////////////////////////////////////////////////////////////////////////////
        // Event for hierarchy is set here to call the parent compositor
        // meant to be disableable in the editor while keeping the renderer & comp enabled
        private void OnEnable()
        {
            if(!Application.isPlaying)
            {
                if(SdfTools.TryGetFirstInParents(this, out SdfCompositor comp, true))
                    EditorApplication.hierarchyChanged += comp.OnHierarchyChanged;
            }
            else  
                if(SdfTools.TryGetFirstInParents(this, out SdfCompositor comp, true)) 
                    EditorApplication.hierarchyChanged -= comp.OnHierarchyChanged;

        }

        private void OnDisable()
        {
            if(SdfTools.TryGetFirstInParents(this, out SdfCompositor comp, true))
                EditorApplication.hierarchyChanged -= comp.OnHierarchyChanged;
        }

        //////////////////////////////////////////////////////////////////////////////
        private int[] GetMaxIds(SdfElement[] elements, int typeCount)
        {
            int[] maxIds = new int[typeCount];

            // get max ids to get length for reconstructing dictionnary
            for(int e = 0; e < elements.Length; e++){
                for(int i = 0; i < maxIds.Length; i++){
                    maxIds[i] = Mathf.Max(maxIds[i], elements[e].GetTextureSliceIDs()[i]);
                }
            }
            return maxIds;
        }

        // since dictionary cannot be serialized, we have to reload it from the elements
        private void ReloadRegister()
        {
            // Debug.Log("--------- Reloading register ---------");
            SdfElement[] elements = GetComponentsInChildren<SdfElement>();
            SdfTexture[] texZero = elements[0].Textures;

            int typeCount = texZero.Length;
            int[] maxIds = GetMaxIds(elements, typeCount); 
            
            // case no ids, all zero, recreate from scratch
            int sum = 0;
            for(int i = 0; i < maxIds.Length; i++)sum += maxIds[i];
            if (sum == 0){
                RecreateRegister(elements); 
                return;
            }

            texRegister = new();

            for(int t = 0; t < typeCount; t++) texRegister.Add(texZero[t].type, new SdfTextureRegistration[maxIds[t] + 1]);

            for(int e = 0; e < elements.Length; e++)
            {
                SdfTexture[] textures = elements[e].GetTextures(out int[] ids); // discard ids since recreating

                for (int t = 0; t < textures.Length; t++)
                {
                    if(textures[t].texture == null) continue;
                    SdfTextureType type = textures[t].type;

                    if(texRegister[type][ids[t]] == null) texRegister[type][ids[t]] = new(textures[t].texture, elements[e]);
                    else texRegister[type][ids[t]].users.Add(elements[e]);
                }
            }
        }

        private void RecreateRegister(SdfElement[] elements = null)
        {
            // Debug.Log("--------- Recreating register ---------");
            elements ??= GetComponentsInChildren<SdfElement>();
            texRegister = new();

            for(int e = 0; e < elements.Length; e++)
            {
                SdfTexture[] textures = elements[e].GetTextures(out _); // discard ids since recreating

                for (int t = 0; t < textures.Length; t++)
                {
                    if(textures[t].texture == null) continue;

                    SdfTextureType type = textures[t].type;
                    int sliceId = 0;

                    if(texRegister.ContainsKey(type))
                    {
                        if(TryFindInRegister(textures[t], out var id)) // add element to user if tex exists
                        {
                            sliceId = id;
                            texRegister[type][id].users.Add(elements[e]);
                        }
                        else sliceId = AddTextureToRegister(textures[t], elements[e]); // create new textureUsers with this element
                    }
                    // create register entry with new texture users containing this element
                    else texRegister.Add(type, new SdfTextureRegistration[]{new(textures[t].texture, elements[e])});
                    
                    elements[e].SetTextureSliceID(t,sliceId);
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        // called from elements and compositor
        public void OnTextureChanged(SdfElement element)
        {
            // Debug.Log("On Texture changed");
            if(SdfTools.TryGetFirstInParents(this, out SdfCompositor compositor))
            {
                if(texRegister == null) ReloadRegister();

                if(IsRegisterToBuild(element, out SdfBuildArgs buildArgs))
                {
                    SdfTexture texture =  BuildRegister(buildArgs, compositor);
                    compositor.OnCollectionChanged(texture);
                    
                    // SdfTexture texture;
                    // if(TryFindCollectionMatch(buildArgs.typeToBuild, out SdfTexture textureMatch)) texture = textureMatch;
                    // else texture = BuildRegister(buildArgs, compositor);
                    // compositor.OnCollectionChanged(texture);
                };
            }
        }

        public SdfTexture[] CreateNewRegisterBuild(SdfCompositor compositor)
        {
            RecreateRegister();
            SdfTexture[] textures = compositor.TexturesBuffer;

            for(int t = 0; t < textures.Length; t++)
            {
                textures[t] = BuildRegister(new(textures[t].type, SdfBuildType.Create, 0), compositor);
            }
            
            return textures;
        }
        
        public void UnregisterElement(SdfElement element)
        {
            SdfTexture[] textures = element.GetTextures(out int[] ids);

            if(SdfTools.TryGetFirstInParents<SdfCompositor>(this, out var comp))
            {
                for(int t = 0; t < textures.Length; t++)
                {
                    SdfTextureType type = textures[t].type;

                    texRegister[type][ids[t]].users.Remove(element);

                    if(TryRemoveUnused(type, t, ids[t]))
                    {
                        SdfTexture tex = BuildRegister(new(type, SdfBuildType.Remove, ids[t]), comp);
                        comp.OnCollectionChanged(tex);
                    }
                }
                comp.Init();
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        // Update register and outputs args to build it into texture array
        private int AddTextureToRegister(SdfTexture sdfTexture, SdfElement element)
        {
            int id = texRegister[sdfTexture.type].Length;
            SdfTextureRegistration registration = new(sdfTexture.texture, element);
            List<SdfTextureRegistration> registrations = new(texRegister[sdfTexture.type]){ registration };
            texRegister[sdfTexture.type] = registrations.ToArray();
            return id;
        }

        private bool TryFindInRegister(SdfTexture sdfTexture, out int id)
        {
            id = -1;
            for(int r = 0; r < texRegister[sdfTexture.type].Length; r++)
            {
                if(texRegister[sdfTexture.type][r].texture == sdfTexture.texture)
                {
                    id = r;
                    return true;
                }
            }
            return false;
        }

        private bool TryRemoveUnused(SdfTextureType type, int texId, int id)
        {
            if(texRegister[type][id].users.Count == 0)
            {
                List<SdfTextureRegistration> registrations = new(texRegister[type]);
                registrations.RemoveAt(id);
                texRegister[type] = registrations.ToArray();

                for(int r = 0; r < texRegister[type].Length; r++)
                {
                    SdfTextureRegistration reg = texRegister[type][r];
                    for (int u = 0; u < reg.users.Count; u++) reg.users[u].SetTextureSliceID(texId, r);
                }
                return true;
            }
            else return false;
        }

        private bool IsRegisterToBuild(SdfElement element, out SdfBuildArgs buildArgs)
        {
            buildArgs = null;
            SdfTexture[] textures = element.GetTextures(out int[] pIds);
            
            for(int t = 0; t < textures.Length; t++)
            {
                if(textures[t].texture == null)continue;
                SdfTextureType type = textures[t].type;

                if(TryFindInRegister(textures[t], out int nId)) // texture exists in register
                {
                    if(pIds[t] == nId) continue; // no change for this type
                    else // register to matching texture and check if prev is unsued
                    {
                        texRegister[type][pIds[t]].users.Remove(element);
                        texRegister[type][nId].users.Add(element);
                        element.SetTextureSliceID(t,nId);

                        if(TryRemoveUnused(type, t, pIds[t])) // register has an unsued texture
                        {
                            buildArgs = new(type, SdfBuildType.Remove, pIds[t]);
                            return true;
                        }
                        else return false; // new texture is already registered and the previous one is still being used
                    }
                }
                else // texture does not exist in register, remove from prev and replace||add registration
                {
                    texRegister[type][pIds[t]].users.Remove(element); // remove from previous texture users

                    if(texRegister[type][pIds[t]].users.Count == 0) // replace old registration if it does not have users anymore
                    {
                        texRegister[type][pIds[t]] = new SdfTextureRegistration(textures[t].texture, element);
                        buildArgs = new(type, SdfBuildType.Replace, pIds[t]);
                    }
                    else // append new registration
                    {
                        nId = AddTextureToRegister(textures[t], element);
                        // Debug.Log("adding new registration at id : " + nId);

                        element.SetTextureSliceID(t, nId);
                        buildArgs = new(type, SdfBuildType.Add, nId);
                    }
                    return true;
                }
            }
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////
        // this build a textureArray from the register (texRegister)
        // Texture2D[] textures = texRegister[buildArgs.typeToBuild].Select(x=>(Texture2D)x.texture).ToArray();
        private SdfTexture BuildRegister(SdfBuildArgs build, SdfCompositor comp)
        {
            SdfTextureRegistration[] reg = texRegister[build.typeToBuild];
            Texture2DArray texArray = null;
            SdfTexture res = new(build.typeToBuild);

            bool halfDistances = true;

            if(build.type != SdfBuildType.Create) 
                texArray = (Texture2DArray)comp.GetBufferTexture(build.typeToBuild).texture;

            if(build.type == SdfBuildType.Remove) // early exit
            {
                res.texture = build.typeToBuild switch
                {
                    SdfTextureType.Sdf => halfDistances ?
                        TexUtils.RemoveSlice<short>(ref texArray, build.slice):
                        TexUtils.RemoveSlice<float>(ref texArray, build.slice),
                    _=> TexUtils.RemoveSlice<Color32>(ref texArray, build.slice),
                };
                return res;
            }

            SdfGenerator gen = new(){
                Textures = new(),
                TargetResolution = targetResolution,
                ChannelMode = ChannelMode.AlphaThresh,
                Thresh = 0,
                BlurIterations = 20,
                KeepFieldExact = false,
                ComputeNormals = false,
                HalfDistances = halfDistances,
            };

            if(build.type == SdfBuildType.Create) for(int i=0; i<reg.Length; i++) gen.Textures.Add((Texture2D)reg[i].texture);
            else gen.Textures.Add((Texture2D)reg[build.slice].texture); // add only the changing texture

            if(build.typeToBuild == SdfTextureType.Sdf)
            {
                if(!gen.GenerateSdf(out float[][] sdfs , out _)) return res;
                res.texture = build.type switch
                {
                    SdfBuildType.Replace => TexUtils.WriteSlice(ref texArray, sdfs[0], build.slice, halfDistances),
                    SdfBuildType.Add     => TexUtils.AddSlice(ref texArray, sdfs[0], halfDistances),
                    _=> TexUtils.CreateTexArray(sdfs, targetResolution.x, targetResolution.y, halfDistances, FilterMode.Point, TextureWrapMode.Clamp),
                };
            }
            else
            {
                if(!gen.ConvertList(out Color[][] colors)) return res;
                res.texture = build.type switch
                {
                    SdfBuildType.Replace => TexUtils.WriteSlice(ref texArray, colors[0], build.slice),
                    SdfBuildType.Add     => TexUtils.AddSlice(ref texArray, colors[0]),
                    _=> TexUtils.CreateTexArray(colors, targetResolution.x, targetResolution.y, TextureFormat.RGBA32, FilterMode.Point, TextureWrapMode.Mirror),
                };
            }

            DestroyImmediate(gen);
            return res;
        }


        ////////////////////////////////////////////////////////////////////////////// TEMP
        // Compare all collection for list match, and returns the textureArray 
        // from its parent compositor
        private bool TryFindCollectionMatch(SdfTextureType type, out SdfTexture texture)
        {
            texture = null;
            SdfCollection[] allCollections = FindObjectsOfType<SdfCollection>(true);

            for(int c = 0; c < allCollections.Length; c++)
            {
                if(allCollections[c] == this)continue;
                
                if(allCollections[c].CompareRegister(type, texRegister[type], out var comp)) // match
                {
                    texture = comp.GetBufferTexture(type);
                    return true;
                }
            }
            return false;
        }

        // TODO : Returns an SDFTexture + IDS
        // Ask dialog if found resolution != targetResolution
        public bool CompareRegister(SdfTextureType type, SdfTextureRegistration[] registrations, out SdfCompositor compositor)
        {
            compositor = null;

            if(texRegister.ContainsKey(type))
            {
                if(texRegister[type].Length == registrations.Length)
                {
                    for(int t = 0; t < registrations.Length; t++)
                    {
                        if(registrations[t].texture != texRegister[type][t].texture)return false;
                    }
                    if(SdfTools.TryGetFirstInParents<SdfCompositor>(this, out var comp, true))
                    {
                        compositor = comp;
                        return true;
                    }
                    else return false;
                } 
                else return false;
            } 
            else return false;
        }
    }
}
