using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

namespace Spark.Editor
{
    /// ---------- IMGUI 
    public class EditorWindow
    {
        public Rect position;
        public string name;
        
        private bool firstShown;
        protected int windowColor;

        public void Update()
        {
            windowColor = IMGUI.RGBA(15, 10, 12, 150);

            if (!firstShown)
            {
                firstShown = true;
                OnEnable();
            }

            OnGUI();
        }

        protected virtual void OnEnable() { }
        protected virtual void OnGUI() { }

    }

    public class GUIMenu
    {
        public List<DropdownItem> items = new List<DropdownItem>();

        public void OnGUI()
        {
            IMGUI.DEFAULT_SPACING = 1;
            IMGUI.BUTTON_HEIGHT = 26;
            IMGUI.BeginGroup(0, 0, (int)RenderView.Active.Size.X, 28, IMGUI.RGBA(0, 0, 0, 155));
            IMGUI.BeginHorizontal();

            foreach (var item in items)
                item.OnGUI();

            IMGUI.EndHorizontal();
            IMGUI.EndGroup();

            foreach (var item in items)
            {
                if (item.IsOpen())
                {
                    item.OnDropdownGUI();
                    break;
                }
            }
            IMGUI.DEFAULT_SPACING = 4;
            IMGUI.BUTTON_HEIGHT = 22;
        }
    }

    public enum GUIWindowDock
    {
        Float,
        Left,
        Right,
        Top,
        Bottom,
        Fill
    }

    public class GUIWindow
    {
        private int selectedTab;
        public List<EditorWindow> tabs = new List<EditorWindow>();
        public Rect rect;

        public void OnGUI()
        {
            IMGUI.DEFAULT_SPACING = 1;
            IMGUI.BUTTON_HEIGHT = 26;

            IMGUI.BeginGroup(rect.x, rect.y, rect.w, 28);
            IMGUI.BeginHorizontal();

            for (int i = 0; i < tabs.Count; i++)
            {
                if (IMGUI.Button(tabs[i].name, true))
                    selectedTab = i;
            }

            IMGUI.EndHorizontal();
            IMGUI.EndGroup();

            IMGUI.DEFAULT_SPACING = 4;
            IMGUI.BUTTON_HEIGHT = 22;

            if (tabs.Count > selectedTab)
            {
                tabs[selectedTab].position = new Rect(rect.x, rect.y + 28, rect.w, rect.h - 28);
                tabs[selectedTab].Update();
            }
        }
    }

    public class GUIDropdown
    {
        public bool mouseOver;
        public Rect rect;
        public List<DropdownItem> items = new List<DropdownItem>();

        public bool IsOpen()
        {
            if (mouseOver) return true;

            foreach (var item in items)
            {
                if (item.IsOpen())
                    return true;
            }

            return false;
        }

        public void OnGUI()
        {
            mouseOver = rect.Contains(Input.MousePoint);

            IMGUI.BeginGroup(rect.x, rect.y, rect.w, rect.h, IMGUI.RGBA(0, 0, 0, 230));
            IMGUI.BeginVertical();

            foreach (var item in items)
                item.OnGUI();

            IMGUI.EndVertical();
            IMGUI.EndGroup();

            foreach (var item in items)
            {
                if (item.IsOpen())
                    item.OnDropdownGUI();
            }
        }
    }

    public class DropdownItem
    {
        public bool mouseoOver;
        public string name;
        public Action callback;
        private GUIDropdown dropdown = new GUIDropdown();
        private Rect rect;
        public bool primary;

        public List<DropdownItem> items
        {
            get { return dropdown.items; }
        }

        public bool IsOpen()
        {
            if (mouseoOver) return true;

            return dropdown.IsOpen();
        }

        public void OnGUI()
        {
            rect = IMGUI.GetNextRect();
            if (IMGUI.Button(rect, name, true))
            {
                if (callback != null)
                    callback();
            }

            if (items.Count > 0)
                mouseoOver = rect.Contains(Input.MousePoint);
        }

        public void OnDropdownGUI()
        {
            if (primary)
                dropdown.rect = new Rect(rect.x, rect.y + rect.h, 200, items.Count * (IMGUI.BUTTON_HEIGHT + IMGUI.DEFAULT_SPACING) + IMGUI.DEFAULT_SPACING);
            else
                dropdown.rect = new Rect(rect.x + rect.w, rect.y, 200, items.Count * (IMGUI.BUTTON_HEIGHT + IMGUI.DEFAULT_SPACING) + IMGUI.DEFAULT_SPACING);

            dropdown.OnGUI();
        }
    }

    public class CustomEditorAttribute : Attribute
    {
        public Type Type { get; private set; }

        public CustomEditorAttribute(Type type)
        {
            Type = type;
        }
    }

    public class PropertyDrawerAttribute : Attribute
    {
        public Type Type { get; private set; }

        public PropertyDrawerAttribute(Type type)
        {
            Type = type;
        }
    }

    public class PropertyDrawer
    {
        public virtual void OnGUI(GUIProperty property)
        {

        }
    }

    public class CustomEditor
    {
        protected object targetObject;
        protected object[] targetObjects;
        protected GUIObject guiObject;

        internal void SetTargets(params object[] value)
        {
            if (value == null) return;

            targetObjects = value;

            if (value.Length == 1)
                targetObject = value[0];

            guiObject = new GUIObject(targetObjects);
        }

        public virtual void OnEnable() { }
        public virtual void OnGUI() { }
    }

    public delegate T GUITypeHandler<T>(GUIProperty field, T value);

    public class GenericEditor : CustomEditor
    {
        private List<GUIProperty> properties = new List<GUIProperty>();
        private string changedProperty;
        private bool expanded = true;

        public override void OnEnable()
        {
            properties = guiObject.GetProperties();
        }

        public override void OnGUI()
        {
            if (IMGUI.Button(guiObject.Name, true))
                expanded = !expanded;

            if (expanded)
            {
                //IMGUI.Indent();
                foreach (var field in properties)
                    EditorIMGUI.PropertyField(field);
                //IMGUI.Unindent();
            }
        }
    }

    public static class EditorIMGUI
    {
        private static Dictionary<int, Delegate> delegates = new Dictionary<int, Delegate>();

        static EditorIMGUI()
        {
            RegisterType<int>(HandleInt);
            RegisterType<float>(HandleFloat);
            RegisterType<double>(HandleDouble);
            RegisterType<bool>(HandleBool);
            RegisterType<string>(HandleString);
            RegisterType<Vector3>(HandleVector3);
            RegisterType<Quaternion>(HandleQuaternion);
            RegisterType<Mesh>(HandleMesh);
            RegisterType<Material>(HandleMaterial);
            RegisterType<Texture>(HandleTexture);
            RegisterType<List<Material>>(HandleMaterials);
        }

        public static void PropertyField(GUIProperty field)
        {
            int hash = field.Type.GetHashCode();

            if (delegates.ContainsKey(hash))
            {
                bool mixedValue = field.HasMixedValue;
                object value = field.GetValue();

                try
                {
                    object result = delegates[hash].Method.Invoke(null, new object[2] { field, value });

                    if (!object.Equals(value, result))
                    {
                        field.SetValue(result);
                    }
                }
                catch { }
            }
        }

        public static void RegisterType<T>(GUITypeHandler<T> handler)
        {
            Type type = typeof(T);
            int hash = type.GetHashCode();

            if (!delegates.ContainsKey(hash))
                delegates.Add(hash, handler);
            else
                delegates[hash] = handler;
        }

        //private static int HandleEnum(PropertyProxy proxy, int value)
        //{
        //    if (proxy.Type.IsDefined(typeof(FlagsAttribute), false))
        //    {
        //        int result = 0;

        //        proxy.Expanded = EditorGUILayout.Foldout(proxy.Expanded, proxy.Name + string.Format(" ({0})", Enum.ToObject(proxy.Type, value)));

        //        if (proxy.Expanded)
        //        {
        //            EditorGUI.indentLevel++;

        //            foreach (Enum v in Enum.GetValues(proxy.Type))
        //            {
        //                int bit = Convert.ToInt32(v);

        //                bool b = (value & bit) == bit;
        //                b = EditorGUILayout.Toggle(v.ToString(), bit == 0 ? (value == 0) : b);
        //                if (b) result |= bit;
        //            }

        //            EditorGUI.indentLevel--;
        //        }
        //        else
        //            return value;

        //        if (result != value)
        //            ImmediateUndo = true;

        //        return result;
        //    }
        //    else
        //    {
        //        string[] names = Enum.GetNames(proxy.Type);
        //        int[] values = new int[names.Length];

        //        int j = 0;
        //        foreach (Enum e in Enum.GetValues(proxy.Type))
        //        {
        //            values[j] = Convert.ToInt32(e);
        //            j++;
        //        }

        //        int result = EditorGUILayout.IntPopup(proxy.Name, value, names, values);

        //        if (result != value)
        //            ImmediateUndo = true;

        //        return result;
        //    }
        //}

        private static int HandleInt(GUIProperty field, int value)
        {
            return Convert.ToInt32(IMGUI.TextField(field.Name, value.ToString()));
        }

        private static float HandleFloat(GUIProperty field, float value)
        {
            //if (proxy.Property.IsDefined(typeof(RangeAttribute), true))
            //{
            //    RangeAttribute range = GetAttribute<RangeAttribute>(proxy.Property);
            //    return EditorGUILayout.Slider(proxy.Name, value, range.Min, range.Max);
            //}

            try
            {
                return Convert.ToSingle(IMGUI.TextField(field.Name, value.ToString()));
            }
            catch
            {
                return value;
            }
        }

        private static double HandleDouble(GUIProperty field, double value)
        {
            try
            {
                return Convert.ToDouble(IMGUI.TextField(field.Name, value.ToString()));
            }
            catch
            {
                return value;
            }
        }

        private static string HandleString(GUIProperty field, string value)
        {
            return IMGUI.TextField(field.Name, value);
        }

        private static Mesh HandleMesh(GUIProperty field, Mesh value)
        {
            IMGUI.ButtonField(field.Name, value != null ? value.Name : string.Empty);
            return value;
        }

        private static Texture HandleTexture(GUIProperty field, Texture value)
        {
            IMGUI.ButtonField(field.Name, value != null ? value.Name : string.Empty);
            return value;
        }

        private static List<Material> HandleMaterials(GUIProperty field, List<Material> value)
        {
            foreach (var material in value)
                HandleMaterial(null, material);

            return value;
        }

        private static Material HandleMaterial(GUIProperty field, Material value)
        {
            IMGUI.ButtonField(field != null ? field.Name : "Material", value != null ? value.Name : string.Empty);

            HandleEffect(field, value.Effect);

            return value;
        }

        private static Effect HandleEffect(GUIProperty field, Effect value)
        {
            //IMGUI.Button(value.Description.Filename, true);
            //IMGUI.Indent();

            //IMGUI.ButtonField(field.Name, value != null ? value.Name : string.Empty);
            // value.UseInstancing = IMGUI.CheckBox("Instancing", value.UseInstancing);

            foreach (var parm in value.GetParams())
            {
                object obj = parm.Value.GetValue();
                string val = obj != null ? obj.ToString() : "null";

                IMGUI.TextField(parm.Key, val);
            }

            //IMGUI.Unindent();

            return value;
        }

        private static Vector3 HandleVector3(GUIProperty field, Vector3 value)
        {
            var rect = IMGUI.GetNextRect();
            var left = rect;
            var right = rect;
            int textwidth = 12;
            int spacing = 2;

            left.w = 80;

            int width = Math.Max(40, (rect.w - left.w - 3 * textwidth - 2 * spacing) / 3);

            IMGUI.Label(left, field.Name);

            right.x = rect.x + rect.w - (width + textwidth) * 3 - 2 * spacing;
            right.w = (width + textwidth) * 3 + 2 * spacing;

            IMGUI.Label(right, "X:");
            right.x += textwidth; right.w -= textwidth;
            value.X = Convert.ToSingle(IMGUI.Textbox(new Rect(right.x, right.y, width, right.h), value.X.ToString("N4"), 0, true), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            right.x += width; right.w -= width;

            right.x += spacing; right.w -= spacing;
            IMGUI.Label(right, "Y:");
            right.x += textwidth; right.w -= textwidth;
            value.Y = Convert.ToSingle(IMGUI.Textbox(new Rect(right.x, right.y, width, right.h), value.Y.ToString("N4"), 0, true), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            right.x += width; right.w -= width;

            right.x += spacing; right.w -= spacing;
            IMGUI.Label(right, "Z:");
            right.x += textwidth; right.w -= textwidth;
            value.Z = Convert.ToSingle(IMGUI.Textbox(new Rect(right.x, right.y, width, right.h), value.Z.ToString("N4"), 0, true), System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            right.x += width; right.w -= width;

            return value;
        }

        private static Quaternion HandleQuaternion(GUIProperty field, Quaternion value)
        {
            var vector = value.ToEuler();
            vector = HandleVector3(field, vector);
            return MathExtensions.QuaternionFromEuler(vector);
        }

        private static bool HandleBool(GUIProperty field, bool value)
        {
            return IMGUI.CheckBox(field.Name, value);
        }
    }

    [PropertyDrawer(typeof(int))]
    public class IntPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(GUIProperty property)
        {
            EditorIMGUI.PropertyField(property);
        }
    }

    [CustomEditor(typeof(Transform))]
    public class TransformEditor : CustomEditor
    {
        public override void OnGUI()
        {
            base.OnGUI();
        }
    }
}
