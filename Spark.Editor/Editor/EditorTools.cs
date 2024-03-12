using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

namespace Spark.Editor
{
    [ExecuteInEditor]
    public class EditorTools : Component, IDraw, IUpdate
    {
        private bool mouseWasDown;

        private TransformScale scale;
        private TransformRotate rotate;
        private TransformTranslate translate;
        private TerrainBrush terrainBrush;

        private ToolBase activeTool;

        protected override void Awake()
        {
            translate = new TransformTranslate();
            translate.Initialize();

            rotate = new TransformRotate();
            rotate.Initialize();

            scale = new TransformScale();
            scale.Initialize();

            terrainBrush = new TerrainBrush();
            terrainBrush.Initialize();

            activeTool = translate;
        }

        public void Update()
        {
            if (Input.IsKeyDown(System.Windows.Forms.Keys.F1))
                activeTool = translate;
            if (Input.IsKeyDown(System.Windows.Forms.Keys.F2))
                activeTool = rotate;
            if (Input.IsKeyDown(System.Windows.Forms.Keys.F3))
                activeTool = scale;
            if (Input.IsKeyDown(System.Windows.Forms.Keys.F4))
                activeTool = terrainBrush;

            activeTool.Update(Camera.MainCamera);
            if (activeTool.MouseCaptured) return;

            bool down = Input.IsMouseDown(0);
            bool ctrl = Input.IsKeyDown(System.Windows.Forms.Keys.LControlKey);

            if (!Input.IsMouseDown(1) && !EditorUI.MouseCaptured && down && !mouseWasDown)
            {
                Ray ray = Camera.MainCamera.MouseRay();

                if (Physics.Raycast(ray, out RaycastResult result))
                {
                    if (Input.Shift)
                        Selector.SelectOrDeselect(result.entity);
                    else
                        Selector.SelectedEntity = result.entity;
                }
                else
                    Selector.SelectedEntity = null;
            }

            mouseWasDown = down;
        }

        public void Draw()
        {
            Selector.Draw();

            if (Selector.Selection.Count < 1) return;
            activeTool.Render();
        }
    }
}
