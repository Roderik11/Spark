using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark.Client
{
    public class InspectorWindow : EditorWindow
    {
        private Entity selectedEntity;
        private List<CustomEditor> editors = new List<CustomEditor>();
        private int scroll;

        void onSelectionChanged(Message msg)
        {
            selectedEntity = Selector.SelectedEntity;

            editors.Clear();

            if(selectedEntity != null)
            {
                foreach (var selected in selectedEntity.GetComponents())
                {
                    var editor = new GenericEditor();
                    editor.SetTargets(selected);
                    editor.OnEnable();
                    editors.Add(editor);
                }
            }
        }

        protected override void OnEnable()
        {
            MessageDispatcher.AddListener(Msg.SelectionChanged, onSelectionChanged);
        }

        protected override void OnGUI()
        {
            IMGUI.BeginArea(position.x, position.y, position.w, position.h, scroll, windowColor);

            if (selectedEntity != null)
            {
                foreach (var editor in editors)
                    editor.OnGUI();
            }

            scroll = IMGUI.EndArea();
        }
    }
}
