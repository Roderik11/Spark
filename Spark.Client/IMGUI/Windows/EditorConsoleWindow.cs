using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark.Client
{
    public class EditorConsoleWindow : EditorWindow
    {
        private int scroll;

        protected override void OnGUI()
        {
            IMGUI.BeginArea(position.x, position.y, position.w, position.h, scroll);

            foreach (var entry in Debug.Lines)
            {
                if (IMGUI.Item(entry.Message, true))
                {

                }
            }

            scroll = IMGUI.EndArea();
        }
    }
}
