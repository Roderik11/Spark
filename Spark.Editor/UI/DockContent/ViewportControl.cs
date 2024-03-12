using System;
using Squid;
using SharpDX;
using Point = Squid.Point;
using Rectangle = Squid.Rectangle;
using System.IO;

namespace Spark.Editor
{
    public class InnerViewport : ImageControl { }

    public class ViewportControl : Frame
    {
        public RenderView View { get; private set; }

        private Frame toolbar;
        private InnerViewport image;
        private readonly string textureId = System.IO.Path.GetRandomFileName();

        public ViewportControl()
        {
            toolbar = new Frame
            {
                Style = "frame",
                Size = new Point(16, 20),
                Dock = DockStyle.Top,
                Margin = new Margin(0, 0, 0, 1)
            };

            image = new InnerViewport
            {
                Texture = textureId,
                Dock = DockStyle.Fill,
            };

            //Controls.Add(toolbar);
            Controls.Add(image);

            image.GetElements().Add(new Frame
            {
                Size = new Point(100, 100),
                Style = "viewport",
                Dock = DockStyle.Fill
            });

            View = new RenderView(image.Size.x, image.Size.y)
            {
                Pipeline = new DeferredRenderer(),
                Enabled = true
            };

            var rend = Gui.Renderer as RendererSlimDX;
            rend.InsertTexture(textureId, View.BackBufferTexture);

            View.OnResized += View_OnResized;
            SizeChanged += ViewportControl_SizeChanged;

            image.AllowDrop = true;
            image.DragEnter += ViewportControl_DragEnter;
            image.DragLeave += ViewportControl_DragLeave;
            image.DragDrop += Image_DragDrop;

            var camera = Entity.GetActiveCameras()[0].Target = View;
        }

        private Entity dragEntity;
        private float distance;
        private SharpDX.BoundingBox bounds;

        private void ViewportControl_DragEnter(Control sender, DragDropEventArgs e)
        {
            if(e.DraggedControl.Tag is AssetInfo assetInfo)
            {
                if(assetInfo.AssetType == typeof(Mesh))
                {
                    e.DraggedControl.Opacity = 0;

                    //dragEntity.EditorFlags |= EditorFlags.AlwaysVisible;

                    var mesh = Engine.Assets.Load<Mesh>(assetInfo.FullPath);
                    dragEntity = new Entity(mesh.Name);
                    dragEntity.Ghost = true;
                    var com = dragEntity.AddComponent<StaticMeshRenderer>();
                    com.StaticMesh = CreateStaticMesh(mesh);

                    //var com = dragEntity.AddComponent<MeshRenderer>();
                    //com.Mesh = mesh;

                    //for (int i = 0; i < com.Mesh.MeshParts.Count; i++)
                    //    com.Materials.Add(Engine.DefaultMaterial);

                    bounds = mesh.BoundingBox;
                    var scale = bounds.Size.Length();
                    if (scale > 100)
                    {
                        dragEntity.Transform.Scale = Vector3.One * (100 / scale);
                        com.UpdateBounds();
                        bounds = com.BoundingBox;
                    }
                    distance = bounds.Size.Length() * 2;
                }
            }
        }

        private StaticMesh CreateStaticMesh(Mesh mesh)
        {
            var staticMesh = new StaticMesh { Name = mesh.Name, Mesh = mesh, LODGroup = LODGroups.LargeProps };
            var allLOds = mesh.MeshParts.FindAll((m) => m.Name.Contains("LOD_"));

            var material = Engine.DefaultMaterial;

            var texturepath = Path.GetDirectoryName(mesh.Path) + @"\Textures\";
            var absolutePath = Path.Combine(Engine.ResourceDirectory, texturepath);

            if (Directory.Exists(absolutePath))
            {
                var defaultDiffuse = Engine.Assets.Load<Texture>("checker_grey.dds");
                var defaultNormal = Engine.Assets.Load<Texture>("DefaultNormalMap.dds");
                var defaultData = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");
                var aoc = Engine.Assets.Load<Texture>("white.dds");

                var shader = new Effect("mesh_opaque");
                material = new Material(shader);
                material.Name = mesh.Name;
                material.UseInstancing = true;
                material.SetValue("Albedo", defaultDiffuse);
                material.SetValue("Normal", defaultNormal);
                material.SetValue("Occlusion", aoc);
                material.SetValue("Data", defaultData);
                material.SetValue("sampData", Samplers.WrappedAnisotropic);

                foreach (var file in Directory.EnumerateFiles(absolutePath))
                {
                    if (file.EndsWith(".psd")) continue;
                    var relativePath = file.Replace(Engine.ResourceDirectory, "");

                    var withoutExtension = Path.GetFileNameWithoutExtension(file);
                    if (withoutExtension.EndsWith("_Dif"))
                    {
                        var diffuse = Engine.Assets.Load<Texture>(relativePath);
                        material.SetValue("Albedo", diffuse);
                    }

                    if (withoutExtension.EndsWith("_Nor"))
                    {
                        var diffuse = Engine.Assets.Load<Texture>(relativePath);
                        material.SetValue("Normal", diffuse);
                    }

                    if (withoutExtension.EndsWith("_Aoc"))
                    {
                        aoc = Engine.Assets.Load<Texture>(relativePath);
                        material.SetValue("Occlusion", aoc);
                    }
                }
            }

            // no lods
            if (mesh.MeshParts.Count - allLOds.Count <= 0)
            {
                for (int i = 0; i < mesh.MeshParts.Count; i++)
                {
                    staticMesh.MeshParts.Add(new MeshElement
                    {
                        Mesh = mesh,
                        Material = material,
                        MeshPart = i
                    });
                }
                return staticMesh;
            }

            for (int i = 0; i < mesh.MeshParts.Count; i++)
            {
                var part = mesh.MeshParts[i];
                var name = part.Name;

                int index = name.IndexOf("_LOD_");
                if (index < 0)
                {
                    staticMesh.MeshParts.Add(new MeshElement
                    {
                        Mesh = mesh,
                        Material = material,
                        MeshPart = i
                    });
                }
                else
                {
                    int endIndex = index + 6 < name.Length ? 2 : 1;
                    int lvl = Convert.ToInt32(name.Substring(index + 5, endIndex)) - 1;
                    while (staticMesh.LODs.Count < lvl + 1)
                        staticMesh.LODs.Add(new StaticMeshLOD { Mesh = mesh });

                    staticMesh.LODs[lvl].MeshParts.Add(new MeshElement
                    {
                        Mesh = mesh,
                        Material = material,
                        MeshPart = i
                    });
                }
            }

            return staticMesh;
        }

        private void Image_DragDrop(Control sender, DragDropEventArgs e)
        {
            if (dragEntity != null)
            {
                dragEntity.Ghost = false;
                dragEntity = null;
                MessageDispatcher.Send(Msg.RefreshExplorer);
            }
        }

        private void ViewportControl_DragLeave(Control sender, DragDropEventArgs e)
        {
            if(dragEntity != null)
            {
                dragEntity.Destroy();
                dragEntity = null;

                e.DraggedControl.Opacity = 1;
            }
        }

        protected override void OnUpdate()
        {
            if(dragEntity != null)
            {
                var ray = Camera.MainCamera.MouseRay();
                if (Physics.Raycast(ray, out RaycastResult result))
                    dragEntity.Transform.Position =  result.hitPoint - bounds.Center + Vector3.Up * bounds.Height / 2;
                else
                    dragEntity.Transform.Position = ray.Position + ray.Direction * distance - bounds.Center;
            }

            View.Offset = new SharpDX.Point(image.Location.x, image.Location.y);
            base.OnUpdate();
        }

        private void View_OnResized()
        {
            var rend = Gui.Renderer as RendererSlimDX;
            rend.Replace(textureId, View.BackBufferTexture);
            image.TextureRect = new Rectangle();
        }

        private void ViewportControl_SizeChanged(Control sender)
        {
            View.Resize(image.Size.x, image.Size.y);
        }
    }

}
