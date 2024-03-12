using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using Spark;

namespace Spark.Editor
{
    public enum KeyBindAction
    {
        None,

        [KeyMapping(Keys.Control | Keys.X, Category = "Edit")]
        Cut,

        [KeyMapping(Keys.Control | Keys.C, Category = "Edit")]
        Copy,

        [KeyMapping(Keys.Control | Keys.V, Category = "Edit")]
        Paste,

        [KeyMapping(Keys.Control | Keys.Z, Category = "Edit")]
        Undo,

        [KeyMapping(Keys.Control | Keys.Y, Category = "Edit")]
        Redo,

        [KeyMapping(Keys.Control | Keys.F, Category = "Selection", DisplayName = "Set camera focus")]
        FocusSelection,

        [KeyMapping(Keys.Control | Keys.R, Category = "Selection", DisplayName = "Align to ground")]
        AlignSelection,

        [KeyMapping(Keys.Control | Keys.E, Category = "Selection", DisplayName = "Show")]
        ShowSelection,

        [KeyMapping(Keys.Control | Keys.Q, Category = "Selection", DisplayName = "Hide")]
        HideSelection,

        [KeyMapping(Keys.Control | Keys.Space, Category = "Selection", DisplayName = "Clear")]
        ClearSelection,

        [KeyMapping(Keys.Control | Keys.C, Category = "Selection", DisplayName = "Toggle show/hide")]
        ToggleSelection,

        [KeyMapping(Keys.Control | Keys.F, Category = "Selection", DisplayName = "Center around parent")]
        CenterSelection,

        [KeyMapping(Keys.Shift | Keys.E, Category = "Placement Brush", DisplayName = "Increase Rotation")]
        BrushIncreaseRotation,

        [KeyMapping(Keys.Shift | Keys.Q, Category = "Placement Brush", DisplayName = "Decrease Rotation")]
        BrushDecreaseRotation,

        [KeyMapping(Keys.Alt | Keys.E, Category = "Placement Brush", DisplayName = "Increase Scale")]
        BrushIncreaseScale,

        [KeyMapping(Keys.Alt | Keys.Q, Category = "Placement Brush", DisplayName = "Decrease Scale")]
        BrushDecreaseScale
    }

    public class KeyMappingAttribute : Attribute
    {
        public string Category;
        public string DisplayName;
        public Keys Key;
        public KeyMappingAttribute(Keys key) { Key = key; }
    }

    public static class KeyMapper
    {
        private static Dictionary<KeyBindAction, KeyBindData> Actions = new Dictionary<KeyBindAction, KeyBindData>();

        private class KeyBindData
        {
            public System.Windows.Forms.Keys Keys;
            public Action Callback;
        }

        static KeyMapper() { }

        public static void Register(KeyBindAction action, Action callback)
        {
            if (Actions.ContainsKey(action))
                Actions[action].Callback = callback;
            else
                Actions.Add(action, new KeyBindData { Callback = callback });
        }

        public static void Map(KeyBindAction action, System.Windows.Forms.Keys keys)
        {
            if (Actions.ContainsKey(action))
                Actions[action].Keys = keys;
            else
                Actions.Add(action, new KeyBindData { Keys = keys });
        }

        public static void Invoke(System.Windows.Forms.Keys keys)
        {
            KeyBindAction action = GetKeyBind(keys);
            if (action != KeyBindAction.None)
                Invoke(action);
        }

        public static void Invoke(KeyBindAction action)
        {
            if (Actions.ContainsKey(action))
            {
                if (Actions[action].Callback != null)
                    Actions[action].Callback.Invoke();
            }
        }

        public static string GetKeyString(KeyBindAction action)
        {
            System.Windows.Forms.Keys key = System.Windows.Forms.Keys.None;

            if (Actions.ContainsKey(action))
                key = Actions[action].Keys;

            return TypeDescriptor.GetConverter(key).ConvertToString(key);
        }

        public static KeyBindAction GetKeyBind(System.Windows.Forms.Keys key)
        {
            foreach (KeyBindAction i in Actions.Keys)
            {
                if (Actions[i].Keys == key)
                    return i;
            }

            return KeyBindAction.None;
        }
    }
}
