using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark.Editor
{
    public class DebugWindow : EditorWindow
    {
        private bool expandTimers;
        private bool expandDrawCalls;
        private int scroll;

        protected override void OnGUI()
        {
            IMGUI.BeginArea(position.x, position.y, position.w, position.h, scroll, windowColor);

            IMGUI.LeftRight("FPS: "+ Time.FPS.ToString(), "");
            IMGUI.LeftRight("Delta: " + Time.DeltaMilliseconds.ToString("#.00"), "");

            Engine.Settings.VSync = IMGUI.CheckBox("VSYNC", Engine.Settings.VSync); 
            Engine.Settings.VisibilityTree = IMGUI.CheckBox("CULL", Engine.Settings.VisibilityTree);
            Engine.Settings.PhysXShapes = IMGUI.CheckBox("PHYSX", Engine.Settings.PhysXShapes);
            Engine.Settings.SSAO = IMGUI.CheckBox("SSAO", Engine.Settings.SSAO);

            // button
            if (IMGUI.Button("Timers", true))
                expandTimers = !expandTimers;

            if (expandTimers)
            {
                DrawEntries(Profiler.main, false);
            }

            // button
            if (IMGUI.Button("Draw Calls", true))
                expandDrawCalls = !expandDrawCalls;

            if (expandDrawCalls)
            {
                foreach (RenderPass pass in Enum.GetValues(typeof(RenderPass)))
                {
                    if (pass == RenderPass.None) continue;
                    int nonInstanced = CommandBuffer.GetNonInstanced(pass);
                    int instanced = CommandBuffer.GetInstanced(pass);
                    int numInstances = CommandBuffer.GetTotalInstances(pass);

                    IMGUI.LeftRight(pass.ToString(), $"{nonInstanced}/{instanced}/{numInstances}");
                }
            }

            scroll = IMGUI.EndArea();
        }

        void DrawEntries(Profiler.ProfilerEntry parent, bool indent = false)
        {
            if(indent)
                IMGUI.Indent();

            foreach (var entry in parent.items)
            {
                IMGUI.LeftRight(entry.name, entry.timeElapsed.ToString("0.00"));

                if (entry.items.Count > 0)
                    DrawEntries(entry, true);
            }

            if(indent)
                IMGUI.Unindent();
        }
    }
}
