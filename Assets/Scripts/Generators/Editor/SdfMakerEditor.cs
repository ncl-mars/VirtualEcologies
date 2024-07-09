using UnityEditor;
using UnityEngine.UIElements;

using Custom.Generators.Makers;

namespace Custom.Generators.GUI
{
    // Inspector GUI
    [CustomEditor(typeof(SdfMaker))][CanEditMultipleObjects]
    public class SdfMakerEditor : MakerEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new();
            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Styles/Editor.uss");
            inspector.styleSheets.Add(uss);

            FillInspectorContent(inspector, true);
            inspector.AddToClassList("custom-inspector");

            return inspector;
        }
    }

    // Inspector GUI
    [CustomEditor(typeof(MeshMaker))][CanEditMultipleObjects]
    public class MeshMakerEditor : MakerEditor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement inspector = new();
            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Styles/Editor.uss");
            inspector.styleSheets.Add(uss);

            FillInspectorContent(inspector, true);
            inspector.AddToClassList("custom-inspector");

            return inspector;
        }
    }
}