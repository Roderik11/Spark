using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Spark.Editor
{
    public class TransformRotate : ToolBase
    {
        private RotateAxis Axis;
        private Plane pickplane;
        private Vector3 pickpoint;
        private Vector3 dragpoint;
        private Quaternion dragQuat;
        private Quaternion pickQuat;
        private int Highlight = -1;

        private Mesh mesh;
        private MaterialBlock block;
        private List<Material> materials = new List<Material>();
        private Vector4[] colors;

        public int Snap = 20;

        public override void Initialize()
        {
            var tex = Engine.Assets.Load<Texture>("white.dds");
            mesh = Engine.Assets.Load<Mesh>("Meshes/transform_rotate.obj");

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
                materials.Add(new Material ( eff ));
            }

            block = new MaterialBlock();
            block.SetParameter("Albedo", tex);
        }

        public override void Disable()
        {
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

            MousePick(camera, matrix);

            mesh.Render(materials, block);
        }

        private void MousePick(Camera camera, Matrix matrix)
        {
            if (Input.MouseOutside) return;

            Ray ray = camera.MouseRay();

            bool collided = mesh.Intersects(ray, matrix, out RaycastResult result);
            bool down = Input.IsMouseDown(0);

            if (collided && !Input.IsMouseDown(1) && !down)
                Highlight = result.meshPart;

            Vector3 position = matrix.TranslationVector;

            if (!Input.IsMouseDown(1) && !EditorUI.MouseCaptured && down)
            {
                if (!IsClicked)
                {
                    if (collided)
                    {
                        IsClicked = true;
                        Axis = (RotateAxis)Highlight;

                        if (Axis == RotateAxis.Y)
                            pickplane = new Plane(position, matrix.Up);
                        else if (Axis == RotateAxis.X)
                            pickplane = new Plane(position, matrix.Right);
                        else if (Axis == RotateAxis.Z)
                            pickplane = new Plane(position, matrix.Forward);

                        Collision.RayIntersectsPlane(ref ray, ref pickplane, out pickpoint);
                        pickQuat = Selector.SelectedEntity.Transform.Rotation;
                        dragQuat = Quaternion.Identity;
                    }
                }
                else
                {
                    Collision.RayIntersectsPlane(ref ray, ref pickplane, out dragpoint);
                    var a = Vector3.Normalize(dragpoint - position);
                    var b = Vector3.Normalize(pickpoint - position);
                    var d = MathExtensions.ClockwiseAngle(a, b, pickplane.Normal);
                    dragQuat = Quaternion.RotationAxis(pickplane.Normal, -d);

                    Selector.SelectedEntity.Transform.Rotation = dragQuat * pickQuat;
                }
            }
            else if (IsClicked)
            {
                //if (ProtocolItem != null)
                //{
                //    ProtocolItem.Update();
                //    Editor.Protocol.Add(ProtocolItem);
                //    ProtocolItem = null;
                //}

                IsClicked = false;
            }

            MouseCaptured = collided ? true : IsClicked;

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
    }

}
