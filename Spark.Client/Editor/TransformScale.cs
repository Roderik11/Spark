using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Spark.Client
{
    public class TransformScale : ToolBase
    {
        private MoveAxis Axis;
        private Plane PickPlane;
        private Plane ConstrainPlane;
        private Vector3 PickOffset;
        private int Highlight = -1;

        private Dictionary<TransformAxis, Vector3> vectors = new Dictionary<TransformAxis, Vector3>();
        private Dictionary<MoveAxis, AxisData> Data = new Dictionary<MoveAxis, AxisData>();
        private Dictionary<Entity, Vector3> Cache = new Dictionary<Entity, Vector3>();
        private bool IsCloned;

        private Mesh mesh;
        private MaterialBlock block;
        private List<Material> materials = new List<Material>();
        private Vector4[] colors;

        public override void Initialize()
        {
            var tex = Engine.Assets.Load<Texture>("white.dds");
            mesh = Engine.Assets.Load<Mesh>("Meshes/transform_scale.obj");

            colors = new Vector4[4];
            colors[0] = Color.Yellow.ToVector4();
            colors[1] = Color.Red.ToVector4();
            colors[2] = Color.Blue.ToVector4();
            colors[3] = Color.Green.ToVector4();

            for (int i = 0; i < mesh.MeshParts.Count; i++)
            {
                var eff = new Effect("mesh_unlit")
                {
                    BlendState = States.BlendNone,
                    DepthStencilState = States.ZReadZWriteOff
                };

                eff.SetValue("Color", colors[i]);
                materials.Add(new Material (eff));
            }

            block = new MaterialBlock();
            block.SetParameter("Albedo", tex);

            Data.Add(MoveAxis.X, new AxisData { Axis1 = TransformAxis.LocalForward, Axis2 = TransformAxis.LocalUp });
            Data.Add(MoveAxis.Y, new AxisData { Axis1 = TransformAxis.LocalRight, Axis2 = TransformAxis.LocalForward });
            Data.Add(MoveAxis.Z, new AxisData { Axis1 = TransformAxis.LocalRight, Axis2 = TransformAxis.LocalUp });
            Data.Add(MoveAxis.XY, new AxisData { Axis1 = TransformAxis.LocalForward, Axis2 = TransformAxis.None });
            Data.Add(MoveAxis.ZY, new AxisData { Axis1 = TransformAxis.LocalRight, Axis2 = TransformAxis.None });
            Data.Add(MoveAxis.XZ, new AxisData { Axis1 = TransformAxis.LocalUp, Axis2 = TransformAxis.None });
            Data.Add(MoveAxis.Free, new AxisData { Axis1 = TransformAxis.CameraForward, Axis2 = TransformAxis.None });
        }

        public override void Update(Camera camera)
        {

        }

        public override void Render()
        {
            var camera = Camera.MainCamera;
            Vector3 center = Vector3.Zero;

            foreach (Entity e in Selector.Selection)
                center += e.Transform.WorldPosition;

            center /= Selector.Selection.Count;

            Matrix matrix = Selector.Selection.Count == 1 ? Selector.Selection[0].Transform.Matrix : Matrix.Translation(center);

            Vector3 cam = camera.Transform.WorldPosition;
            Vector3 cen = cam + Vector3.Normalize(center - cam) * 3;

            matrix.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);
            matrix = Matrix.Scaling(Vector3.One * .5f) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(cen);
            block.SetParameter("World", matrix);

            MousePick(camera, center, matrix);

            mesh.Render(materials, block);
        }

        public override void Disable()
        {
        }

        private void MousePick(Camera camera, Vector3 center, Matrix matrix)
        {
            if (Input.MouseOutside) return;

            Ray ray = camera.MouseRay();

            bool collided = mesh.Intersects(ray, matrix, out RaycastResult result);
            bool down = Input.IsMouseDown(0);

            if (collided && !Input.IsMouseDown(1) && !down)
                Highlight = result.meshPart;

            if (!Input.IsMouseDown(1) && !EditorUI.MouseCaptured && down)
            {
                if (!IsClicked)
                {
                    if (collided)
                    {
                        if (Input.Alt && !IsCloned)
                        {
                            IsCloned = true;
                            //Editor.DuplicateSelection();
                            return;
                        }

                        IsClicked = true;

                        Axis = (MoveAxis)Highlight;

                        if (Data.ContainsKey(Axis))
                        {
                            Vector3[] normals = GetPlaneNormals(camera, matrix);

                            PickPlane = new Plane(center, normals[0]);
                            ConstrainPlane = new Plane(center, normals[1]);
                            Collision.RayIntersectsPlane(ref ray, ref PickPlane, out PickOffset);

                            foreach (Entity e in Selector.Selection)
                                Cache.Add(e, e.Transform.Scale);

                            // ProtocolItem = new ProtocolTransform(Selector.Selection);
                        }
                    }
                }
                else
                {
                    if (Data.ContainsKey(Axis))
                    {
                        Collision.RayIntersectsPlane(ref ray, ref PickPlane, out Vector3 intersect);

                        Vector3 scale = Vector3.Zero;
                        if (Axis == MoveAxis.Free)
                            scale.Y = 1;
                        if (Axis == MoveAxis.Y)
                            scale.X = 1;
                        if (Axis == MoveAxis.X)
                            scale.Z = 1;
                        if (Axis == MoveAxis.Z)
                            scale = Vector3.One;

                        foreach (Entity e in Selector.Selection)
                        {
                            Vector3 dir = (intersect - PickOffset) * .1f;
                            e.Transform.Scale = scale * dir.Y + Cache[e];
                        }
                    }
                }
            }
            else if (IsClicked)
            {
                IsCloned = false;
                Cache.Clear();

                //if (ProtocolItem != null)
                //{
                //    ProtocolItem.Update();
                //    Editor.Protocol.Add(ProtocolItem);
                //    ProtocolItem = null;
                //}

                IsClicked = false;
            }

            MouseCaptured = collided || IsClicked;

            if (!collided && !down && !IsClicked)
                Highlight = -1;

            Graphics.Lines.Draw(matrix.TranslationVector, matrix.TranslationVector - matrix.Forward * 1f, Color.Red.ToVector4());
            Graphics.Lines.Draw(matrix.TranslationVector, matrix.TranslationVector + matrix.Up * 1f, Color.Green.ToVector4());
            Graphics.Lines.Draw(matrix.TranslationVector, matrix.TranslationVector + matrix.Right * 1f, Color.Blue.ToVector4());

            for (int i = 0; i < materials.Count; i++)
            {
                materials[i].SetValue("Color", colors[i]);
                if (Highlight == i)
                    materials[i].SetValue("Color", Color.White.ToVector4());
            }
        }


        private Vector3[] GetPlaneNormals(Camera camera, Matrix matrix)
        {
            vectors[TransformAxis.LocalForward] = matrix.Forward;
            vectors[TransformAxis.LocalRight] = matrix.Right;
            vectors[TransformAxis.LocalUp] = matrix.Up;
            vectors[TransformAxis.CameraForward] = camera.Entity.Transform.Forward;
            vectors[TransformAxis.None] =  Vector3.Zero;

            AxisData data = Data[Axis];

            return new Vector3[] { vectors[data.Axis1], vectors[data.Axis2] };
        }
    }
}
