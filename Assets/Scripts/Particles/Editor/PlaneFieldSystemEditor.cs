/*
  0 - 0
  1 - DistanceToClosestZone
  2 - DistanceToOrigin
  3 - DistanceToClosestZone, DistanceToOrigin
  4 - Velocity
  5 - DistanceToClosestZone, Velocity
  6 - DistanceToOrigin, Velocity
  7 - DistanceToClosestZone, DistanceToOrigin, Velocity
  8 - BillBoard
  9 - DistanceToClosestZone, BillBoard
 10 - DistanceToOrigin, BillBoard
 11 - DistanceToClosestZone, DistanceToOrigin, BillBoard
 12 - Velocity, BillBoard
 13 - DistanceToClosestZone, Velocity, BillBoard
 14 - DistanceToOrigin, Velocity, BillBoard
 15 - DistanceToClosestZone, DistanceToOrigin, Velocity, BillBoard
 16 - Velocity_Cam
 17 - DistanceToClosestZone, Velocity_Cam
 18 - DistanceToOrigin, Velocity_Cam
 19 - DistanceToClosestZone, DistanceToOrigin, Velocity_Cam
 20 - Velocity, Velocity_Cam
 21 - DistanceToClosestZone, Velocity, Velocity_Cam
 22 - DistanceToOrigin, Velocity, Velocity_Cam
 23 - DistanceToClosestZone, DistanceToOrigin, Velocity, Velocity_Cam
 24 - BillBoard, Velocity_Cam
 25 - DistanceToClosestZone, BillBoard, Velocity_Cam
 26 - DistanceToOrigin, BillBoard, Velocity_Cam
 27 - DistanceToClosestZone, DistanceToOrigin, BillBoard, Velocity_Cam
 28 - Velocity, BillBoard, Velocity_Cam
 29 - DistanceToClosestZone, Velocity, BillBoard, Velocity_Cam
 30 - DistanceToOrigin, Velocity, BillBoard, Velocity_Cam
 31 - DistanceToClosestZone, DistanceToOrigin, Velocity, BillBoard, Velocity_Cam
 32 - Velocity_Yup
 33 - DistanceToClosestZone, Velocity_Yup
 34 - DistanceToOrigin, Velocity_Yup
 35 - DistanceToClosestZone, DistanceToOrigin, Velocity_Yup
 36 - Velocity, Velocity_Yup
 37 - DistanceToClosestZone, Velocity, Velocity_Yup
 38 - DistanceToOrigin, Velocity, Velocity_Yup
 39 - DistanceToClosestZone, DistanceToOrigin, Velocity, Velocity_Yup
 40 - BillBoard, Velocity_Yup
 41 - DistanceToClosestZone, BillBoard, Velocity_Yup
 42 - DistanceToOrigin, BillBoard, Velocity_Yup
 43 - DistanceToClosestZone, DistanceToOrigin, BillBoard, Velocity_Yup
 44 - Velocity, BillBoard, Velocity_Yup
 45 - DistanceToClosestZone, Velocity, BillBoard, Velocity_Yup
 46 - DistanceToOrigin, Velocity, BillBoard, Velocity_Yup
 47 - DistanceToClosestZone, DistanceToOrigin, Velocity, BillBoard, Velocity_Yup
 48 - Velocity_Cam, Velocity_Yup
 49 - DistanceToClosestZone, Velocity_Cam, Velocity_Yup
 50 - DistanceToOrigin, Velocity_Cam, Velocity_Yup
 51 - DistanceToClosestZone, DistanceToOrigin, Velocity_Cam, Velocity_Yup
 52 - Velocity, Velocity_Cam, Velocity_Yup
 53 - DistanceToClosestZone, Velocity, Velocity_Cam, Velocity_Yup
 54 - DistanceToOrigin, Velocity, Velocity_Cam, Velocity_Yup
 55 - DistanceToClosestZone, DistanceToOrigin, Velocity, Velocity_Cam, Velocity_Yup

 56 - BillBoard, Velocity_Cam, Velocity_Yup
 
 57 - DistanceToClosestZone, BillBoard, Velocity_Cam, Velocity_Yup
 58 - DistanceToOrigin, BillBoard, Velocity_Cam, Velocity_Yup
 59 - DistanceToClosestZone, DistanceToOrigin, BillBoard, Velocity_Cam, Velocity_Yup
 60 - Velocity, BillBoard, Velocity_Cam, Velocity_Yup
 61 - DistanceToClosestZone, Velocity, BillBoard, Velocity_Cam, Velocity_Yup
 62 - DistanceToOrigin, Velocity, BillBoard, Velocity_Cam, Velocity_Yup
 63 - DistanceToClosestZone, DistanceToOrigin, Velocity, BillBoard, Velocity_Cam, Velocity_Yup
 64 - 64
*/
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;

using Custom.GUIEditor;

namespace Custom.Particles.PlaneField
{
    [CustomEditor(typeof(PlaneFieldSystem))][CanEditMultipleObjects]
    public class PlaneFieldSystemEditor : GUIBufferInspector
    {
        VisualElement inspector;

        public override VisualElement CreateInspectorGUI()
        {
            inspector = new();
            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Styles/Editor.uss");
            inspector.styleSheets.Add(uss);
            FillInspectorContent(inspector);
            inspector.AddToClassList("custom-inspector");

            return inspector;
        }

        protected override void AddObjectProperty(VisualElement container, SerializedProperty property)
        {
            if(property.name == "simulation")
            {
                VisualElement obj = new Foldout() { text = SplitCamelCase(property.name), name = property.name };                
                VisualElement settings = new() { name = "set"};
                VisualElement seq = new Foldout() { text = "Sequencer", name = "seq"};
                VisualElement forces = new Foldout() { text = "Forces", name = "for"};

                obj.Add(settings);
                obj.Add(seq);
                obj.Add(forces);

                VisualElement inForces = new Foldout() { text = "In Forces", name = "ifo"};
                VisualElement outForces = new Foldout() { text = "Out Forces", name = "ofo"};
                VisualElement worldForces = new Foldout() { text = "World", name = "wfo"};

                forces.Insert(0,inForces);
                forces.Insert(1,outForces);
                forces.Insert(2,worldForces);

                IterateLevel(obj, property);

                if(inForces.childCount == 0)    forces.Remove(inForces);
                if(outForces.childCount == 0)   forces.Remove(outForces);
                if(worldForces.childCount == 0) forces.Remove(worldForces);

                container.Add(obj);
            }
            else if(property.name == "renderer")
            {
                VisualElement obj = new Foldout() { text = SplitCamelCase(property.name), name = property.name };
                VisualElement settings = new() { name = "set"};
                
                //-- vertex stage
                VisualElement vertex = new Foldout() { text = "Vertex", name = "ver"};
                VisualElement transform = new Foldout() { text = "Transform", name = "vtr"};
                VisualElement palette = new Foldout() { text = "Palette", name = "vcl"};

                vertex.Add(transform);
                vertex.Add(palette);

                //-- frag
                VisualElement fragment = new Foldout() { text = "Fragment", name = "fra"};
                VisualElement mate = new Foldout() { text = "Material", name = "fma"};
                VisualElement lights = new Foldout() { text = "Lights", name = "fli"};
                fragment.Add(mate);
                fragment.Add(lights);

                obj.Add(settings);
                obj.Add(vertex);
                obj.Add(fragment);

                IterateLevel(obj, property);
                container.Add(obj);
            }

            else base.AddObjectProperty(container, property);
        }

        protected override void AddArrayProperty(VisualElement container, SerializedProperty parentProperty)
        {
            SerializedProperty property = parentProperty.Copy();

            string[] parts = property.propertyPath.Split(".");
            BufferElementID arrayElement = new(parts[^2], parts[^1]);

            switch (arrayElement)
            {
                case("simulation", "uvb")  : IterateBufferArray(container, property, AddSimulationVector); break;
                case("simulation", "textures") : IterateBufferArray(container, property, AddSimulationTexture); break;

                case("renderer", "uvb")    : IterateBufferArray(container, property, AddRendererVector); break;
                case("renderer", "textures")    : IterateBufferArray(container, property, AddRendererTexture); break;
                
                default: base.AddArrayProperty(container, property); break;
            }
        }

        protected override void AddProperty(VisualElement container, SerializedProperty property)
        {
            if(property.name == "mode") container.Insert(0,CreateSimulationModeEnum(property));

            else if(property.name == "material")            container.Q("set").Insert(0,new PropertyField(property));
            else if(property.name == "sprites")    container.Q("set").Insert(1,new PropertyField(property));
            
            else base.AddProperty(container, property);
        }

        protected void AddSimulationTexture(VisualElement ctn, SerializedProperty property, int id, int sub)
        {
            BufferComponentID comp = new(id, 0, 0);
            switch (comp)
            {
                case (0,0,0) : ctn.Q("set").Add(new PropertyField(property, "Field Map")); break;
                default : break;
            };
        }

        protected void AddRendererTexture(VisualElement ctn, SerializedProperty property, int id, int sub)
        {
            BufferComponentID comp = new(id, 0, 0);
            switch (comp)
            {
                case (0,0,0) : ctn.Q("fma").Add(new PropertyField(property, "Bump Map")); break;
                default : break;
            };
        }

        protected void AddRendererVector(VisualElement ctn, SerializedProperty property, int id, int sub)
        {
            // Debug.LogFormat("Adding renderer vector at id : {0}, sub : {1}", id, sub);
            BufferComponentID comp = new(id, sub, 0);
            switch (comp)
            {
                // settings
                case (0,0,0) : ctn.Q("set").Add(CreateFloatSliderEntry(property, 0.0f, 1, "Alpha Cutoff")); break;
                case (1,3,0) : ctn.Q("set").Add(CreateFloatSliderEntry(property, 0.0f, 10, "Scale Renderer")); break;

                // Palette
                case (3,0,0) : ctn.Q("vcl").Add(CreateVectorColor(property, "Pal A")); break;
                case (4,0,0) : ctn.Q("vcl").Add(CreateVectorColor(property, "Pal B")); break;
                case (5,0,0) : ctn.Q("vcl").Add(CreateVectorColor(property, "Pal C")); break;
                case (6,0,0) : ctn.Q("vcl").Add(CreateVectorColor(property, "Pal D")); break;
                case (7,0,0) : ctn.Q("vcl").Add(CreateFloatSliderEntry(property, 0, 10, "Scale")); break;
                case (7,1,0) : ctn.Q("vcl").Add(CreateFloatSliderEntry(property, 0, 10, "Base")); break;
                case (7,2,0) : ctn.Q("vcl").Add(CreateFloatSliderEntry(property, 0, 1, "Randomize")); break;
                case (7,3,0) : ctn.Q("vcl").Insert(0, CreateRendererEnum(property, "Modulator", 7)); break;

                // Transform
                case (8,0,0) : ctn.Q("vtr").Insert(0, CreateRendererEnum(property, "Rotate Mode", 56)); break;
                case (8,1,0) : ctn.Q("vtr").Add(CreateFloatSliderEntry(property, 0.0f, 10, "Size")); break;

                default : break;
            };
        }

        protected void AddSimulationVector(VisualElement ctn, SerializedProperty property, int id, int sub)
        {
            PlaneFieldSimulation simulation = (PlaneFieldSimulation)(target as PlaneFieldSystem).Simulation;
            int flag = simulation.Mode.GetIndex() + 1; // 0 for all

            BufferComponentID comp = new(id, sub, 0);

            // set flag for modal component
            switch (comp)
            {
                case (4,2,0) : comp.F = flag; break;
                case (4,3,0) : comp.F = flag; break;

                case (5,0,0) : comp.F = flag; break;
                case (5,1,0) : comp.F = flag; break;
                case (5,2,0) : comp.F = flag; break;
                case (5,3,0) : comp.F = flag; break;

                case (6,0,0) : comp.F = flag; break;
                case (6,1,0) : comp.F = flag; break;
                case (6,2,0) : comp.F = flag; break;
                case (6,3,0) : comp.F = flag; break;

                case (7,0,0) : comp.F = flag; break;
                default : break;
            };

            // Debug.Log("creating vector entry from comp : " + comp.I);

            // add property field 
            switch (comp)
            {
                // common
                case (0,0,0) : ctn.Q("set").Add(CreateIntEntry(property, "Max Count")); break;
                case (1,2,0) : ctn.Q("seq").Add(CreateFloatSliderEntry(property, 0.001f, 100, "Delay Start")); break;
                case (1,3,0) : ctn.Q("seq").Add(CreateFloatSliderEntry(property, 0.001f, 100, "Life Time")); break;

                case (2,3,0) : ctn.Q("for").Insert(0, CreateFloatSliderEntry(property, 0.001f, 10, "Scale Simulation")); break;

                case (4,0,0) : ctn.Q("for").Insert(0,CreateFloatSliderEntry(property, 0.001f, 10, "Max Speed")); break;
                case (4,1,0) : ctn.Q("for").Insert(1,CreateFloatSliderEntry(property, 0.001f, 2, "Max Force")); break;


                // path
                case (4,2,1) : ctn.Q("for").Insert(2, CreateFloatSliderEntry(property, 0.0f, 1, "Fields Attr")); break;

                case (5,0,1) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, -1, 1, "Ground Attraction")); break;
                case (5,1,1) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, 0, 1, "Randomize Height")); break;
                case (5,2,1) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, -1, 1, "Path Flow")); break;
                case (5,3,1) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, 0, 1, "Randomize Flow")); break;
        
                case (6,0,1) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, -1, 1, "Path Center")); break;
                // case (6,1,1) : ctn.Q("ofo").Add(CreateFloatSliderEntry(property, 0.0f, 1, "Fields Attr")); break;
                


                // gravity
                case (5,0,2) : ctn.Q("for").Add(CreateFloatSliderEntry(property, -1, 1, "Gravity")); break;
                case (5,1,2) : ctn.Q("for").Add(CreateFloatSliderEntry(property, -1, 1, "Friction")); break;

                // plateforms
                case (4,2,3) : ctn.Q("for").Insert(2, CreateFloatSliderEntry(property, -1, 1, "Mix In/Out")); break;
                case (4,3,3) : ctn.Q("for").Insert(3, CreateFloatSliderEntry(property, 0.01f, 1, "Surf Thick")); break;
                
                case (5,0,3) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, -1, 1, "In Flow")); break;
                case (5,1,3) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, -1, 1, "Surf Friction")); break;
                case (5,2,3) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, 0.0f, 1.0f, "Field Att")); break;
                case (5,3,3) : ctn.Q("ifo").Add(CreateFloatSliderEntry(property, 0, 1, "Randomize")); break;

                case (6,0,3) : ctn.Q("ofo").Add(CreateFloatSliderEntry(property, 0.0f, 1.0f, "Out Flow")); break;
                case (6,1,3) : ctn.Q("ofo").Add(CreateFloatSliderEntry(property, -1.0f, 1.0f, "Field Attr")); break;
                case (6,2,3) : ctn.Q("ofo").Add(CreateFloatSliderEntry(property, 0.00f, 1.0f, "Field Dist")); break;
                case (6,3,3) : ctn.Q("ofo").Add(CreateFloatSliderEntry(property, 0.0f, 1.0f, "Randomize")); break;

                case (7,0,3) : ctn.Q("wfo").Add(CreateVectorEntry(property, "Wind")); break; // x4

                default : break;
            };
        }

        protected VisualElement CreateVectorEntry(SerializedProperty property, string label)
        {
            if(GetPropertyParent(property, out SerializedProperty parentProperty))
            {
                Vector4Field vf = new (label);

                Vector4 vv = parentProperty.vector4Value;
                vf.SetValueWithoutNotify(vv);
                vf.RegisterCallback<ChangeEvent<Vector4>>((c) =>
                {
                    parentProperty.vector4Value = c.newValue;
                    serializedObject.ApplyModifiedProperties();
                });
                return vf;
            }
            return null;
        }

        protected VisualElement CreateVectorColor(SerializedProperty property, string label)
        {
            if(GetPropertyParent(property, out SerializedProperty parentProperty))
            {
                ColorField cf = new (label);
                Vector4 vv = parentProperty.vector4Value;
                Color cv = new(vv.x, vv.y, vv.z, vv.w);

                cf.SetValueWithoutNotify(cv);

                cf.RegisterCallback<ChangeEvent<Color>>((c) =>
                {
                    parentProperty.vector4Value = (Vector4)c.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                cf.AddToClassList("unity-base-field__aligned");

                return cf;
            }
            return null;
        }

        //----------------------------------------------------------------------
        public void OnModeEnumChange(ChangeEvent<string> evt)
        {
            PlaneFieldSimulation simulation = (PlaneFieldSimulation)(target as PlaneFieldSystem).Simulation;
            simulation.Mode = evt.newValue.ToEnum(simulation.Mode);
            FillInspectorContent(inspector);
        }
        
        private VisualElement CreateSimulationModeEnum(SerializedProperty property)
        {
            PlaneFieldSimulation simulation = (PlaneFieldSimulation)(target as PlaneFieldSystem).Simulation;

            VisualElement enumMapType = CreatePartialEnum
            (
                (SimulationMode)unchecked((short)-1),
                property,
                simulation.Mode.ToString(), 
                OnModeEnumChange
            );
            return enumMapType;
        }    

        private VisualElement CreateRendererEnum(SerializedProperty property, string label, int showFlag = -1) 
        {
            RendererModes showFlags = showFlag > -0.5f ?  (RendererModes)showFlag : (RendererModes)unchecked((short)-1);

            property = property.Copy();

            int index = (int)property.floatValue;
            RendererModes mode = index.GetFlag<RendererModes>();

            PopupField<string> pModes = CreatePartialEnum
            (
                showFlags,
                label, 
                mode.ToString()
            );

            pModes.RegisterCallback<ChangeEvent<string>>((evt)=>
            {
                float newValue = evt.newValue.ToEnum(mode).GetIndex();
                Debug.LogFormat("setting renderer mode enum with index {0}, value {1}", newValue, evt.newValue);
                property.floatValue = newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            return pModes;
        }
    }
}