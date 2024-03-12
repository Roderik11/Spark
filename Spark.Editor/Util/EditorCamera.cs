using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpDX;
using Spark;

using Component = Spark.Component;

namespace Spark.Editor
{
    [ExecuteInEditor]
    public class EditorCamera : Component, IUpdate
    {
        private Vector2 angle;
        private Vector2 newAngle;
        private float smoothness = 10;

        public void Update()
        {
           // if (GameGui.KeyboardCaptured) return;

            float speed = 3;

            if (Input.Shift)
                speed = Math.Max(20, Entity.Transform.Position.Y);

            speed *= Time.Delta;

            //if (Input.IsMouseDown(1))
            //{
            //    Entity.Transform.RotateLocal(Vector3.UnitY, (float)Input.MouseDelta.X * Time.Delta * 50f);
            //    Entity.Transform.Rotate(Vector3.UnitX, (float)Input.MouseDelta.Y * Time.Delta * 50f);
            //}

            if (Input.IsMouseDown(1))
                newAngle += new Vector2(Input.MouseDelta.X, Input.MouseDelta.Y) * Time.Delta;

            angle += (newAngle - angle) * Time.Delta * smoothness;
            Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(angle.X, angle.Y, 0); ;

            if (Input.IsKeyDown(Keys.W))
                Entity.Transform.Move(speed, 0, 0);
            if (Input.IsKeyDown(Keys.A))
                Entity.Transform.Move(0, 0, -speed);
            if (Input.IsKeyDown(Keys.S))
                Entity.Transform.Move(-speed, 0, 0);
            if (Input.IsKeyDown(Keys.D))
                Entity.Transform.Move(0, 0, speed);
        }
    }
}
