using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpDX;
using Spark;

using Component = Spark.Component;

namespace Spark.Client
{
    public class WowCamera : Component, IUpdate
    {
        private float Zoom = 2;
        public Entity Target;
        public Vector3 Offset = Vector3.UnitY * 1.85f;

        public static float angleY;

        private Vector2 angle;
        private Vector2 newAngle;
        private float newZoom = 2;
        private float smoothness = 10;

        public void Update()
        {
            float speed = 1 * Time.SmoothDelta;
            var lim = MathUtil.DegreesToRadians(90);

            //if (Input.IsMouseDown(1))
            newAngle += new Vector2(Input.MouseDelta.X, Input.MouseDelta.Y) * Time.SmoothDelta * 1f;
            newAngle.Y = MathUtil.Clamp(newAngle.Y, -lim, lim);

            if (Input.MouseWheelDelta != 0 && !IMGUI.MouseCaptured)
                newZoom -= (newZoom * .2f) * (float)Input.MouseWheelDelta / 50;

            if (newZoom < 3)
                newZoom = 3;

            angle += (newAngle - angle) * Math.Min(1, Time.SmoothDelta * smoothness);
            Zoom += (newZoom - Zoom) * Math.Min(1, Time.SmoothDelta * smoothness);

            angleY = angle.X;

            Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(angle.X, angle.Y, 0);
            Entity.Transform.Position = Target.Transform.Position + Offset + Entity.Transform.Forward * -Zoom + Entity.Transform.Right * .3f;
        }
    }
}
