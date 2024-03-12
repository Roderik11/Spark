using Squid;
using Spark;
using SharpDX;
using Point = Squid.Point;
using SharpDX.Direct3D11;
using System;
using System.Linq;
using System.Diagnostics.SymbolStore;

namespace Spark.Client
{
    [GUIInspector(typeof(AssetReader<Mesh>))]
    public class MeshInspector : GUIInspector
    {
        private static MeshPreview meshPreview;
        private static AssetManager contentManager;

        public MeshInspector(GUIObject target) : base(target)
        {
            AddCategory("Import Settings");

            foreach (var prop in target.GetProperties())
                AddProperty(prop);

            if(contentManager ==  null)
                contentManager = new AssetManager(Engine.Device)
                { BaseDirectory = Engine.ResourceDirectory };

            var reader = target.Target as AssetReader<Mesh>;
            //var mesh = reader.Import(reader.Filename);
            var mesh =  contentManager.Load<Mesh>(reader.Filepath);

            if (meshPreview == null)
            {
                meshPreview = new MeshPreview
                {
                    Dock = DockStyle.Fill,
                };
            }

            meshPreview.Mesh = mesh;

            var meshObj = new GUIObject(mesh);
            var parts = meshObj.GetProperty("MeshParts");
            var count = parts.GetArrayLength();
            var bounds = mesh.BoundingBox;
            var extents = mesh.BoundingBox.Size;
         
            AddCategory("Mesh Information");

            AddText("Vertices", mesh.Vertices?.Length.ToString());
            AddText("Indices", mesh.Indices?.Length.ToString());
            
            if(mesh.Bones?.Length > 0)
                AddText("Bones", mesh.Bones?.Length.ToString());
            
            AddText("Minimum", bounds.Minimum.ToString());
            AddText("Maximum", bounds.Maximum.ToString());
            AddText("Extents", extents.ToString());

            AddCategory($"{count} Mesh Parts");

            for (int i = 0; i < count; i++)
            {
                var element = parts.GetArrayElementAtIndex(i);
                var part = element.GetValue();
                var partObj = new GUIObject(part);
                var enabled = partObj.GetProperty("Enabled");
                AddProperty(enabled, mesh.MeshParts[i].Name);
            }
        }

        public override Control GetPreview()
        {
            return meshPreview;
        }
    }


    public class MeshPreview: Frame, IPreview
    {
        public Mesh Mesh { get; set; }

        private readonly Frame header;
        private readonly Label lblInfo;
        private readonly ImageControl viewport;
        private readonly RenderView renderView;
        private readonly Button btnWireframe;

        private readonly string textureId = System.IO.Path.GetRandomFileName();

        private Material material;
        private bool isRotating = false;
        private bool isWireframe;

        public MeshPreview()
        {
            header = new Frame
            {
                Style = "category",
                Size = new Point(100, 24),
                Dock = DockStyle.Top,
                Padding = new Margin(0, 0, 24, 0)
            };

            viewport = new ImageControl
            {
                Texture = textureId,
                Dock = DockStyle.Fill,
            };

            // decoration
            viewport.GetElements().Add(new Frame
            {
                Size = new Point(100, 100),
                Style = "viewport",
                Dock = DockStyle.Fill
            });

            lblInfo = new Label
            {
                Style = "",
                Size = new Point(100, 24),
                Dock = DockStyle.Bottom,
                TextAlign = Alignment.MiddleCenter
            };

            Controls.Add(header);
            Controls.Add(viewport);
            Elements.Add(lblInfo);

            renderView = new RenderView(viewport.Size.x, viewport.Size.y)
            {
                Enabled = false,
                OnRender = OnRender
            };

            var rend = Gui.Renderer as RendererSlimDX;
            rend.InsertTexture(textureId, renderView.BackBufferTexture);

            renderView.OnResized += View_OnResized;
            SizeChanged += ViewportControl_SizeChanged;

            viewport.MouseDown += Viewport_MouseDown;
            viewport.MouseUp += Viewport_MouseUp;

            btnWireframe = AddButton("Wireframe");
            btnWireframe.MouseClick += (s, e) => { isWireframe = !isWireframe; };
            header.Controls.Add(btnWireframe);
        }


        Button AddButton(string label)
        {
            var btn = new Button
            {
                Style = "prevtoolbutton",
                Dock = DockStyle.Right,
                Size = new Point(24, 24),
                Margin = new Margin(1, 0, 0, 0),
                Text = label,
                AutoSize = AutoSize.Horizontal,
            };
            header.Controls.Add(btn);
            return btn;
        }

        private void Viewport_MouseDown(Control sender, MouseEventArgs args)
        {
            isRotating = true;
        }

        private void Viewport_MouseUp(Control sender, MouseEventArgs args)
        {
            isRotating = false;
        }

        public void OnEnable()
        {
            meshRotation = Quaternion.Identity;
            finalRotation = Quaternion.Identity;

            renderView.Enabled = true;
            lblInfo.Text = $"Vertices: {Mesh.Vertices.Length}";
        }

        public void OnDisable()
        {
            renderView.Enabled = false;
        }

        private Quaternion meshRotation = Quaternion.Identity;
        private Quaternion finalRotation = Quaternion.Identity;

        void OnRender(RenderView viewport)
        {
            if (material == null)
            {
                var shader = new Effect("mesh_forward");
                material = new Material(shader);

                var texture = Engine.Assets.Load<Texture>("checker_grey.dds");
                material.SetParameter("Albedo", texture);
                material.SetParameter("linearSampler", Samplers.WrappedAnisotropic);
            }

            if (isRotating)
            {
                var mouse = new Vector2(-Input.MouseDelta.X, -Input.MouseDelta.Y) * 0.01f;//Time.SmoothDelta * 2f;
                finalRotation = Quaternion.RotationAxis(Vector3.Right, mouse.Y) * finalRotation;
                finalRotation = Quaternion.RotationAxis(Vector3.Up, mouse.X) * finalRotation;
            }

            meshRotation = Quaternion.Slerp(meshRotation, finalRotation, Time.SmoothDelta * 8);
            viewport.Prepare();

            //CommandBuffer.Push();

            Graphics.SetTargets(viewport.DepthBufferTarget, viewport.BackBufferTarget);
            Graphics.SetViewport(new ViewportF(0, 0, viewport.Size.X, viewport.Size.Y, 0.0f, 1.0f));
            Graphics.SetScissorRectangle(0, 0, (int)viewport.Size.X, (int)viewport.Size.Y);
            Graphics.ClearRenderTargetView(viewport.BackBufferTarget, new Color4(0, 0, 0, 1));
            Graphics.ClearDepthStencilView(viewport.DepthBufferTarget, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            Graphics.SetDepthStencilState(States.ZReadZWriteNoStencil);
            Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);


            var bounds = Mesh.BoundingBox;
            var distance = bounds.Size.Length();

            var camerapos = bounds.Center - Vector3.ForwardLH * distance * 1.25f;
            var view = Matrix.LookAtLH(camerapos, bounds.Center, Vector3.Up);
            var proj = Camera.ProjectionMatrix(new Vector2(Size.x, Size.y), 45, 0.1f, Math.Max(2000, distance * 2));
            var meshPosition = -bounds.Center;// Vector3.Zero;
          
            var rotatedOrigin = Vector3.TransformCoordinate(meshPosition, Matrix.RotationQuaternion(meshRotation));
            meshPosition = rotatedOrigin + bounds.Center;

            material.SetParameter("World", Matrix.RotationQuaternion(meshRotation)  * Matrix.Translation(meshPosition));
            material.SetParameter("MaterialColor", new Vector3(.8f, .8f, .8f));
            material.SetParameter("LightColor", new Vector3(1, 1, 1));
            material.SetParameter("LightDirection", Vector3.Normalize(new Vector3(1, -1, 1)));
            material.SetParameter("View", view);
            material.SetParameter("Projection", proj);
            material.SetParameter("CameraPosition", camerapos);
            material.Apply();


            if (isWireframe)
                Graphics.SetRasterizerState(States.Wireframe);

            foreach (var part in Mesh.MeshParts)
            {
                if(part.Enabled)
                    Mesh.DrawImmediate(part, material, Engine.Device.ImmediateContext);
            }

            //CommandBuffer.Execute(RenderPass.Opaque);
            //CommandBuffer.Clear();

            //CommandBuffer.Pop();
            viewport.Present();
        }

        protected override void OnUpdate()
        {
            renderView.Offset = new SharpDX.Point(viewport.Location.x, viewport.Location.y);
        }
        
        private void View_OnResized()
        {
            var rend = Gui.Renderer as RendererSlimDX;
            rend.Replace(textureId, renderView.BackBufferTexture);
            viewport.TextureRect = new Squid.Rectangle();
        }

        private void ViewportControl_SizeChanged(Control sender)
        {
            renderView.Resize(viewport.Size.x, viewport.Size.y);
        }
    }

}
