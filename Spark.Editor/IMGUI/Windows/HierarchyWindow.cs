using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark.Editor
{
    public class HierarchyWindow : EditorWindow
    {
        private int scroll;
        int selectedColor = IMGUI.RGBA(255, 255, 255, 25);

        protected override void OnGUI()
        {
            IMGUI.BeginArea(position.x, position.y, position.w, position.h, scroll, windowColor);

            foreach (var entity in Entity.Entities)
            {
                if (entity.Transform.Parent != null)
                    continue;

                if (entity.Hidden)
                    continue;

                DoItem(entity);
            }

            scroll = IMGUI.EndArea();
        }

        void DoItem(Entity entity)
        {
            var size = IMGUI.GetTextSize(entity.Name, IMGUI.Font);
            var rect = IMGUI.GetNextRect(size);

            if (entity == Selector.SelectedEntity)
                IMGUI.Box(rect, selectedColor);

            var count = entity.Transform.GetChildCount();
            if (count > 0)
            {

                bool expanded = entity.EditorFlags.HasFlag(EditorFlags.Expanded);
                bool expandedNow = IMGUI.FoldOut(rect, entity.Name, expanded, out bool clicked);

                if (expanded != expandedNow)
                {
                    if (expandedNow)
                        entity.EditorFlags |= EditorFlags.Expanded;
                    else
                        entity.EditorFlags &= ~EditorFlags.Expanded;
                }

                if (clicked)
                    Selector.SelectedEntity = entity;

                if (expanded)
                {
                    IMGUI.Indent(16);

                    for (int i = 0; i < count; i++)
                        DoItem(entity.Transform.GetChild(i).Entity);

                    IMGUI.Unindent();
                }
            }
            else
            {
                rect.x += 16;rect.w -= 16;

                if (IMGUI.Item(rect, entity.Name))
                    Selector.SelectedEntity = entity;
            }
        }
    }
}
