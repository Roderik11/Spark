using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jitter;
using Jitter.LinearMath;

namespace Spark
{
    public class JitterDebugDraw : IDebugDrawer
    {
        static SharpDX.Vector4 color = new SharpDX.Vector4(.5f, .5f, .5f, 1);
        public static JitterDebugDraw Instance = new JitterDebugDraw();

        public void DrawLine(JVector start, JVector end)
        {
            Graphics.Lines.Draw(start, end, color);
        }

        public void DrawPoint(JVector pos)
        {
        }

        public void DrawTriangle(JVector pos1, JVector pos2, JVector pos3)
        {
            DrawLine(pos1, pos2);
            DrawLine(pos2, pos3);
            DrawLine(pos3, pos1);
        }
    }
}
