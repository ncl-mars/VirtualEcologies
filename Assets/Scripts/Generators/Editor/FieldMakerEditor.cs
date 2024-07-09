using UnityEngine;
using UnityEditor;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

using Custom.Generators.Makers;

namespace Custom.Generators.GUI
{
    [CustomEditor(typeof(FieldMaker))][CanEditMultipleObjects]
    public class FieldMakerEditor : MakerEditor
    {
        VisualElement inspector;
        protected MapType MapTypeToShow { get=>(MapType)22; }

        //------------------------------------------------------------------------------------
        public override VisualElement CreateInspectorGUI()
        {
            inspector = new();
            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Styles/Editor.uss");
            inspector.styleSheets.Add(uss);
            inspector.AddToClassList("custom-inspector");

            FillInspectorContent(inspector, true);
            SetLinkStyle(inspector, (target as FieldMaker).LinkedToCollection);

            return inspector;
        }

        protected override void AddArrayProperty(VisualElement container, SerializedProperty property)
        {
            FieldMaker maker = target as FieldMaker;

            bool filter = (maker.MapType == MapType.Plateform) && maker.LinkedToCollection;
            filter = filter && (property.name == "colorSources") && (property.arraySize > 0);

            if(filter)base.AddProperty(container, property.GetArrayElementAtIndex(0).FindPropertyRelative("texture"));
            else base.AddArrayProperty(container, property);
        }

        protected override void AddObjectProperty(VisualElement container, SerializedProperty parentProperty)
        {
            bool filterProp = parentProperty.name == "export";
            if( filterProp && (target as FieldMaker).LinkedToCollection ) return;
            base.AddObjectProperty(container, parentProperty);
        }

        protected override void AddProperty(VisualElement container, SerializedProperty property)
        {
            FieldMaker maker = target as FieldMaker;

            if(maker.LinkedToCollection)
            {
                string name = property.name;
                bool filterProp = (name == "mapType") | (name == "targetResolution");
                
                if(filterProp) return;
            }

            if(property.name == "mapType") // cache enumField to unregister callBack ?
            {
                container.Add( CreatePartialEnum(MapTypeToShow, property, maker.MapType.ToString(), OnMapEnumChange));
                return;
            }

            if(TryGetFlagAttributeFromProperty(property, out FlagAttribute flagAttr))
            {
                MapType modalFlag = (MapType)flagAttr.flag;

                if( (modalFlag & (target as FieldMaker).MapType) != 0) // attribute has flag of fieldMap target mode 
                {
                    PropertyField propField = new(property){name = property.name, bindingPath = property.propertyPath};
                    container.Add(propField);
                    return;
                }
                else return; // is hidden !
            }
            
            base.AddProperty(container, property);
        }

        //------------------------------------------------------------------------------------
        public void OnMapEnumChange(ChangeEvent<string> evt)
        {
            FieldMaker maker = target as FieldMaker;
            maker.MapType = evt.newValue.ToEnum(maker.MapType);
            FillInspectorContent(inspector, true);
        }
    }
}

