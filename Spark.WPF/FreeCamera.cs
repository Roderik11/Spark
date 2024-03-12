using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpDX;
using Spark;
using Component = Spark.Component;

namespace ED8000
{
    [ExecuteInEditor]
    public class FreeCamera : Component, IUpdate
    {
        public void Update()
        {
            //Entity.Camera.Active = Engine.View.Tag == Entity.Tag;
           // if (!Entity.Camera.Active) return;

            float speed = 10 * Time.Delta;

            if (Input.IsMouseDown(1))
            {
                Entity.Transform.RotateLocal(Vector3.UnitY, Input.MouseDelta.X * Time.Delta * 50);
                Entity.Transform.Rotate(Vector3.UnitX, Input.MouseDelta.Y * Time.Delta * 50);
            }

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
