using UnityEditor;
using UnityEngine;

using UnityEditor.UIElements;
using UnityEngine.UIElements;

using Custom.GUIEditor;

namespace Custom.Generators.GUI
{
    public abstract class MakerEditor : GUIInspector
    {
        protected virtual void FillInspectorContent(VisualElement inspector,  bool generateButton)
        {
            base.FillInspectorContent(inspector);

            if(generateButton)
            {
                IGenerator maker = (IGenerator)target;
                VisualElement bigButton = new Button(maker.Generate){text = "Generate"};
                inspector.Add(bigButton);
            }   
        }
    }
}
