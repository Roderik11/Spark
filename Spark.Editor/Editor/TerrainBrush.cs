using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Spark.Editor
{
    public class TerrainBrush : ToolBase
    {
        private Effect sculptEffect;
        private Texture brushTexture;
        private RenderTexture2D sculptBuffer;

        public float BrushSize = 128;
        public float BrushStrength = 0.001f;

        public override void Initialize()
        {
            sculptEffect = new Effect("sculpt");
            brushTexture = Engine.Assets.Load<Texture>("Brushes/Circle Mountain.dds");
        }

        public override void Update(Camera camera)
        {
            MouseCaptured = false;

            if (!Input.IsMouseDown(0)) return;
            if (EditorUI.MouseCaptured) return;

            if (Selector.SelectedEntity == null) return;
            if (!Selector.SelectedEntity.HasComponent<Terrain>()) return;

            var ray = Camera.MainCamera.MouseRay();
            var terrain = Selector.SelectedEntity.GetComponent<Terrain>();
            var heightmap = terrain.Heightmap;

            if (!Physics.Raycast(ray, terrain.Entity, out RaycastResult result)) return;

            MouseCaptured = true;

            sculptBuffer = RenderTexture2D.GetTemporary(heightmap.Description.Width, heightmap.Description.Height, SharpDX.DXGI.Format.R16_Float, false);

            var dimx = terrain.HeightField.GetLength(0) - 1;
            var dimy = terrain.HeightField.GetLength(1) - 1;

            var fx = dimx / terrain.TerrainSize.X;
            var fy = dimy / terrain.TerrainSize.Y;

            var p = result.hitPoint;
            p -= terrain.Transform.Position;
            p.X *= fx;
            p.Z *= fy;
            p /= heightmap.Description.Width;

            sculptEffect.SetParameter("brushPosition", new Vector2(p.X, p.Z));
            sculptEffect.SetParameter("MainTexture", terrain.Heightmap);
            sculptEffect.SetParameter("brushScale", BrushSize / heightmap.Description.Width);
            sculptEffect.SetParameter("brushStrength", BrushStrength);
            sculptEffect.SetParameter("brushTexture", brushTexture);

            Graphics.SetViewport(0, 0, heightmap.Description.Width, heightmap.Description.Height);
            Graphics.Blit(heightmap, sculptBuffer.Target, sculptEffect);
            Graphics.Blit(sculptBuffer, heightmap.Target);

            sculptBuffer.Release();
        }
    }
}
