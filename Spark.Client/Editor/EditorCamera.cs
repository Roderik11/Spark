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
    [ExecuteInEditor]
    public class EditorCamera : Component, IUpdate
    {
        private Vector2 angle;
        private Vector2 newAngle;
        private float smoothness = 10;

        protected override void Awake()
        {
            MessageDispatcher.AddListener(Msg.FocusEntity, OnFocusEntity);
        }

        public void Update()
        {
            if (EditorUI.Modal) return;

            if (!EditorUI.MouseCaptured)
            {
                if (Input.IsMouseDown(1))
                    newAngle += new Vector2(Input.MouseDelta.X, Input.MouseDelta.Y) * Time.Delta;
            }

            angle += (newAngle - angle) * Time.Delta * smoothness;
            Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(angle.X, angle.Y, 0); ;
            
            if (!EditorUI.MouseCaptured)
                Entity.Transform.Move(Input.MouseWheelDelta * Time.Delta * 4, 0, 0);

            if (EditorUI.KeyboardCaptured) return;

            float speed = 3;

            if (Input.Shift)
                speed = Math.Max(20, Entity.Transform.Position.Y);

            speed *= Time.Delta;

            if (Input.IsKeyDown(Keys.W))
                Entity.Transform.Move(speed, 0, 0);
            if (Input.IsKeyDown(Keys.A))
                Entity.Transform.Move(0, 0, -speed);
            if (Input.IsKeyDown(Keys.S))
                Entity.Transform.Move(-speed, 0, 0);
            if (Input.IsKeyDown(Keys.D))
                Entity.Transform.Move(0, 0, speed);
        }

        void OnFocusEntity(Message message)
        {
            if (!(message.Data is Entity entity))
                return;

            float distance = 3;
            var center = entity.Transform.WorldPosition;

            foreach (var comp in entity.GetComponents())
            {
                if (comp is ISpatial spatial)
                {
                    distance = spatial.BoundingSphere.Radius * 2;
                    center = spatial.BoundingSphere.Center;
                    break;
                }
            }

            Transform.Position = center - Transform.Forward * distance;
        }
    }
}
