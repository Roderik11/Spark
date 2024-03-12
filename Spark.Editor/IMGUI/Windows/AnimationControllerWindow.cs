using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark.Editor
{
    public class AnimationControllerWindow : EditorWindow
    {
        private bool expandTimers;
        private bool expandDrawCalls;
        private int scroll;

        protected override void OnGUI()
        {
            IMGUI.BeginArea(position.x, position.y, position.w, position.h, scroll);

            IMGUI.LeftRight("FPS: "+ Time.FPS.ToString(), "");
            IMGUI.LeftRight("Delta: " + Time.DeltaMilliseconds.ToString("#.00"), "");

            // button
            if (IMGUI.Button("Timers", true))
                expandTimers = !expandTimers;

            if (expandTimers)
            {
                //foreach (KeyValuePair<string, System.Diagnostics.Stopwatch> pair in Profiler.Timers)
                //{
                //    int comp = Profiler.GetIndent(pair.Key);

                //    for (int i = 0; i < comp; i++)
                //        IMGUI.Indent();

                //    IMGUI.LeftRight(pair.Key, Profiler.GetTimespanAvg(pair.Key).ToString("0.00"));

                //    for (int i = 0; i < comp; i++)
                //        IMGUI.Unindent();
                //}
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
    }
}
