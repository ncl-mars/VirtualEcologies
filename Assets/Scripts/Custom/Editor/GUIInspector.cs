using UnityEditor;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

using UnityEditor.Search;
using System;
using System.Reflection;
using System.Collections.Generic;


namespace Custom.GUIEditor
{
    public abstract class GUIInspector : Editor
    {
        protected void IterateLevel(VisualElement container, SerializedProperty parentProperty)
        {
            SerializedProperty property = parentProperty.Copy(); 
            var endOfChildren = property.GetEndProperty();

            property.NextVisible(true);
            do
            {
                SerializedProperty temp = property.Copy();
                int numChildren = temp.CountInProperty();
                bool isArray = property.Copy().isArray; // filter class/struct instance from array

                if(isArray)
                {
                    AddArrayProperty(container, property);
                    continue;
                }

                if(numChildren > 1) // class / struct
                {
                    AddObjectProperty(container, property);
                    continue;
                }
                
                AddProperty(container, property);

            } while(property.NextVisible(false) && !SerializedProperty.EqualContents(property, endOfChildren));
        }

        //------------------------------------------------------------------------------------
        // protected virtual void FillInspectorContent(VisualElement inspector, bool generateButton = true)
        protected virtual void FillInspectorContent(VisualElement inspector)
        {
            inspector.Clear();
            SerializedProperty property = serializedObject.GetIterator();
            property.Next(true);

            IterateLevel(inspector, property);

            inspector.Bind(serializedObject);
        }

        protected virtual void AddProperty(VisualElement container, SerializedProperty property)
        {
            if(property.name == "m_Script") return;
            container.Add(new PropertyField(property){name = property.name, bindingPath = property.propertyPath});
        }

        protected virtual void AddArrayProperty(VisualElement container, SerializedProperty property)
        {
            AddProperty(container, property);
        }

        protected virtual void AddObjectProperty(VisualElement container, SerializedProperty parentProperty)
        {
            VisualElement group = new Foldout() { text = SplitCamelCase(parentProperty.name) };
            IterateLevel(group, parentProperty);
            if(group.childCount>0)container.Add(group);
        }

        //------------------------------------------------------------------------------------
        protected virtual VisualElement CreatePartialEnum<T>(T flagToShow, SerializedProperty property, string value = "", EventCallback<ChangeEvent<string>> callback = default)
        where T : Enum
        {
            T[] enumValues = (T[])Enum.GetValues(typeof(T));
            List<string> choices = new();

            for(int i = 0; i < enumValues.Length; i++)
            {
                if( flagToShow.HasFlag(enumValues[i]) ) choices.Add(enumValues[i].ToString());
            }

            PopupField<string> mapEnum = new(choices, choices[0].ToString())
            {
                label = SplitCamelCase(property.name),
                name = property.name
            };
            mapEnum.SetValueWithoutNotify(value);
            mapEnum.RegisterValueChangedCallback(callback);
            mapEnum.AddToClassList("unity-base-field__aligned");

            return mapEnum;
        }

        protected virtual PopupField<string> CreatePartialEnum<T>(T flagToShow, string label, string value = "")
        where T : Enum
        {
            T[] enumValues = (T[])Enum.GetValues(typeof(T));
            List<string> choices = new();

            for(int i = 0; i < enumValues.Length; i++)
            {
                if( flagToShow.HasFlag(enumValues[i]) ) choices.Add(enumValues[i].ToString());
            }

            PopupField<string> mapEnum = new(choices, choices[0].ToString())
            {
                label = label,
            };

            mapEnum.SetValueWithoutNotify(value);
            mapEnum.AddToClassList("unity-base-field__aligned");

            return mapEnum;
        }

        protected virtual PopupField<string> CreateEnum(string label, List<string> choices, string value = "")
        {
            PopupField<string> mapEnum = new(choices, choices[0].ToString())
            {
                label = label,
            };

            mapEnum.SetValueWithoutNotify(value);
            mapEnum.AddToClassList("unity-base-field__aligned");

            return mapEnum;
        }
        //------------------------------------------------------------------------------------
        protected bool TryGetFieldInfoFromProperty(SerializedProperty property, out FieldInfo fieldInfo)
        {
            Type parentType = serializedObject.targetObject.GetType();

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance;

            string[] path = property.propertyPath.Split(".");

            if(path.Length > 1)
            {
                for(int i = 0; i < path.Length - 1; i++) parentType = parentType.GetField(path[i], bindingFlags).FieldType;
                fieldInfo = parentType.GetField(path[^1], bindingFlags);
            }

            else fieldInfo = parentType.GetField(path[0], bindingFlags);

            return fieldInfo != null;
        }

        protected bool TryGetFlagAttributeFromProperty(SerializedProperty property, out FlagAttribute flagAttr)
        {
            flagAttr = null;

            if(TryGetFieldInfoFromProperty(property, out FieldInfo fieldInfo))
            {
                flagAttr = (FlagAttribute)fieldInfo.GetCustomAttribute(typeof(FlagAttribute));
                return flagAttr != null;
            }
            else return false;
        }

        //------------------------------------------------------------------------------------
        protected void SetLinkStyle(VisualElement inspector, bool isLinked)
        {
            if(isLinked)inspector.AddToClassList("linked");
            else inspector.RemoveFromClassList("linked");
        }

        protected string SplitCamelCase(string propertyName)
        {
            string label = propertyName[0].ToString().ToUpper() + propertyName[1..]; // set first capital
            return string.Join(" ", SearchUtils.SplitCamelCase(label));
        }
    }

    public abstract class GUIBufferInspector : GUIInspector
    {
        //---------------------------------------------------------------------- Positional Patterns
        protected struct BufferComponentID
        {
            public int I { get; }   // element index
            public int C { get; }   // component index
            public int F { get; set;}   // flag

            public BufferComponentID(int i, int c, int f) => (I, C, F) = (i, c, f);
            public void Deconstruct(out int i, out int c, out int f) => (i, c, f) = (I, C, F);
        }

        protected readonly struct BufferElementID
        {
            public string P { get; }    // parent
            public string N { get; }    // name

            public BufferElementID(string p, string n) => (P, N) = (p, n);
            public void Deconstruct(out string p, out string n) => (p,n) = (P,N);
        }

        //------------------------------------------------------------------------------------
        protected VisualElement CreateFloatSliderEntry(SerializedProperty property, float min, float max, string label = "")
        {
            property = property.Copy();

            Slider slider = new(min, max, SliderDirection.Horizontal){
                label = label,
                bindingPath = property.propertyPath
            };
            slider.AddToClassList("unity-base-field__aligned");

            FloatField ffield = new(){
                bindingPath = property.propertyPath
            };

            ffield.AddToClassList("unity-base-field__aligned");

            slider.Add(ffield);
            return slider;
        }

        protected VisualElement CreateIntEntry(SerializedProperty property, string label = "")
        {
            property = property.Copy();

            IntegerField el = new(label){ value = (int)property.floatValue };

            el.RegisterCallback<ChangeEvent<int>>((i) =>
            {
                property.floatValue = i.newValue; 
                property.serializedObject.ApplyModifiedProperties();
            });

            el.AddToClassList("unity-base-field__aligned");
            return el;
        }

        protected void IterateBufferArray(VisualElement container, SerializedProperty arrProperty, Action<VisualElement, SerializedProperty, int, int> addHandler)
        {
            for(int i = 0; i < arrProperty.arraySize; i++)
            {
                SerializedProperty elProperty = arrProperty.GetArrayElementAtIndex(i);
                int s = 0;

                var enumerator = elProperty.Copy().GetEnumerator();

                if(enumerator.MoveNext())
                {
                    do
                    {
                        SerializedProperty compProperty = enumerator.Current as SerializedProperty;
                        addHandler.DynamicInvoke(container, compProperty, i, s);
                        s++;
                    } while(enumerator.MoveNext() );
                }
    
                else
                {
                    addHandler.DynamicInvoke(container, elProperty, i, -1);
                }
            }
        }
    
        protected bool GetPropertyParent(SerializedProperty property, out SerializedProperty parentProperty)
        {
            List<string> parts = new(property.propertyPath.Split("."));
            parts.RemoveAt(parts.Count-1);
            string path = string.Join(".", parts.ToArray());

            parentProperty = serializedObject.FindProperty(path);

            return parentProperty != null;
        }
    }
}







//___________________________________________________________________________________
// SerializedProperty elProperty = arrProperty.GetArrayElementAtIndex(i).Copy();

// if(elProperty.Copy().CountInProperty() > 1)
// {
//     SerializedProperty compProperty = elProperty.Copy();

//     var endOfChildren = compProperty.GetEndProperty();
//     int s = 0;

//     compProperty.NextVisible(true);
//     do
//     {
//         addHandler.DynamicInvoke(container, compProperty, i, s);
//         s++;
//     } while(compProperty.NextVisible(true) && !SerializedProperty.EqualContents(compProperty, endOfChildren));
// }
// else
// {
//     addHandler.DynamicInvoke(container, elProperty, i, -1);
// }