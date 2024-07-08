using UnityEditor;
using UnityEngine.UIElements;

using Custom.Generators.Makers;
using Custom.Generators.Modules;

using UnityEditor.UIElements;


namespace Custom.Generators.GUI
{
    // Inspector GUI
    [CustomEditor(typeof(NoiseMaker))][CanEditMultipleObjects]
    public class NoiseMakerEditor : TextureMakerEditor
    {
        VisualElement inspector;
        protected NoiseTypes NoiseTypeToShow { get=>(NoiseTypes)63; }

        public override VisualElement CreateInspectorGUI()
        {
            inspector = new();
            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Styles/Editor.uss");
            inspector.styleSheets.Add(uss);

            FillInspectorContent(inspector, true);
            inspector.AddToClassList("custom-inspector");

            return inspector;
        }

        protected override void AddProperty(VisualElement container, SerializedProperty property)
        {
            if(property.name == "noiseType") // cache enumField to unregister callBack ?
            {
                container.Add( 
                    CreatePartialEnum(
                        NoiseTypeToShow, 
                        property, 
                        (target as NoiseMaker).NoiseType.ToString(), 
                        OnNoiseEnumChange
                    )
                );
                return;
            }

            if(TryGetFlagAttributeFromProperty(property, out FlagAttribute flagAttr))
            {
                NoiseTypes modalFlag = (NoiseTypes)flagAttr.flag;

                if( (modalFlag & (target as NoiseMaker).NoiseType) != 0) // attribute has flag of fieldMap target mode 
                {
                    PropertyField propField = new(property){name = property.name, bindingPath = property.propertyPath};
                    container.Add(propField);
                    return;
                }
                else return; // is hidden !
            }
            base.AddProperty(container, property);
        }

        public void OnNoiseEnumChange(ChangeEvent<string> evt)
        {
            NoiseMaker maker = target as NoiseMaker;
            maker.NoiseType = evt.newValue.ToEnum(maker.NoiseType);
            FillInspectorContent(inspector, true);
        }
    }
}