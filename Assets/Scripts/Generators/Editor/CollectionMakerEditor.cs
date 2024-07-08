using System;

using UnityEditor;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

using Custom.Generators.Makers;

namespace Custom.Generators.GUI
{
    // Inspector GUI
    [CustomEditor(typeof(CollectionMaker))][CanEditMultipleObjects]
    public class CollectionMakerEditor : TextureMakerEditor
    {
        private VisualElement inspector;
        private VisualElement enumLink;

        protected MapType MapTypeToShow
        {
            get=>(MapType)((target as CollectionMaker).Linked ? 22 : 23);
        }

        //------------------------------------------------------------------------------------ Constructors
        public override VisualElement CreateInspectorGUI()
        {
            UnregisterCallbacks();
            inspector = new();

            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Styles/Editor.uss");
            inspector.styleSheets.Add(uss);

            CollectionMaker maker = target as CollectionMaker; 
            FillInspectorContent(inspector, !maker.Linked);
            inspector.AddToClassList("custom-inspector");

            SetLinkStyle(inspector, maker.Linked);
            return inspector;
        }

        // called from TextureMaker
        protected override void AddProperty(VisualElement container, SerializedProperty property)
        {
            if(property.name == "linkMode")
            {
                LinkMode linkMode = (target as CollectionMaker).Link;
                enumLink = CreatePartialEnum((LinkMode)unchecked((byte)-1), property, linkMode.ToString(), OnLinkEnumChange);
                container.Add(enumLink);
            }
     
            else if(property.name == "mapType")
            {
                MapType mapType = (target as CollectionMaker).MapType;
                container.Add(CreatePartialEnum(MapTypeToShow, property, mapType.ToString(), OnMapEnumChange));
            }
            else
            {
                if(TryGetFlagAttributeFromProperty(property, out FlagAttribute flagAttr))
                {
                    bool linkMode = flagAttr.flag == 1;

                    if( linkMode == (target as CollectionMaker).Linked) // attribute has flag of fieldMap target mode 
                    {
                        PropertyField propField = new(property){name = property.name, bindingPath = property.propertyPath};
                        container.Add(propField);
                        return;
                    }
                }
                else base.AddProperty(container, property);
            }
        }

        //------------------------------------------------------------------------------------ Events
        public void UnregisterCallbacks(){ (enumLink as PopupField<string>)?.UnregisterValueChangedCallback(OnLinkEnumChange); }

        public void OnLinkEnumChange(ChangeEvent<string> linkChange)
        {
            UnregisterCallbacks();
            CollectionMaker maker = target as CollectionMaker;

            LinkMode linkMode = linkChange.newValue.ToEnum(maker.Link);
            bool doLink = linkMode != LinkMode.Unlinked;

            SetLinkStyle(inspector, doLink);

            maker.Link = linkMode; //
            FillInspectorContent(inspector, !doLink);
        }

        public void OnMapEnumChange(ChangeEvent<string> evt)
        {
            CollectionMaker maker = target as CollectionMaker;
            maker.MapType = evt.newValue.ToEnum(maker.MapType);
        }
    }
}

