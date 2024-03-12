using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Editor
{
    [ExecuteInEditor]
    public class JointPick : Component, IDraw, IUpdate
    {
        private bool WasDown;

        public void Update()
        {
            bool down = Input.IsMouseDown(0);

            if (!Input.IsMouseDown(1) && down && !WasDown)
            {
                if (Input.MouseOutside) return;

                Ray ray = Camera.MainCamera.MouseRay();

                if (Physics.Raycast(ray, out RaycastResult result))
                    Selector.SelectedObject = result.entity;
                else
                    Selector.SelectedObject = null;
            }

            WasDown = down;
        }

        public void Draw()
        {
            Selector.Draw();
        }
    }
}
