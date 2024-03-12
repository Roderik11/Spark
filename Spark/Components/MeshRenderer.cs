using System.Collections.Generic;
using SharpDX;

namespace Spark
{
    public class MeshRenderer : Component, ISpatial, IDraw, IMousePick, IDrawDebug
    {
        public Mesh Mesh;
        public List<Material> Materials;

        protected MaterialBlock Params;

        private Animator animator;
        private Matrix[] boneMatrices;
        private Vector3[] points = new Vector3[8];

        private int tick;
        private int frameFlip;
        private float range = 40 * 40;

        public BoundingBox BoundingBox { get; protected set; }
        public BoundingSphere BoundingSphere { get; protected set; }
        public Icoseptree SpatialNode { get; set; }

        public MeshRenderer()
        {
            Materials = new List<Material>();
            Params = new MaterialBlock();
        }

        protected override void Awake()
        {
            animator = Entity.GetComponent<Animator>();
        }

        public void UpdateBounds()
        {
            Mesh.BoundingBox.GetCorners(points, Mesh.RootRotation * Transform.Matrix);
            BoundingBox = BoundingBox.FromPoints(points);
            BoundingSphere = BoundingSphere.FromPoints(points);
        }

        public void Draw()
        {
            if (Mesh == null) return;
            
            bool rotated = !Mesh.RootRotation.IsIdentity;
            Params.SetParameter("World", rotated ? Mesh.RootRotation * Transform.Matrix : Transform.Matrix);
            
            if (Mesh.Bones == null)
            {
                Mesh.Render(Materials, Params);
                return;
            }

            if (animator == null) return;

            if (boneMatrices == null || boneMatrices.Length != Mesh.Bones.Length)
                boneMatrices = new Matrix[Mesh.Bones.Length];

            if (tick != Engine.TickCount)
            {
                tick = Engine.TickCount;

                var dist = Vector3.DistanceSquared(Transform.WorldPosition, Camera.Main.WorldPosition);
                float rate = dist / range;

                frameFlip++;

                if (frameFlip >= rate)
                {
                    frameFlip = 0;
                    animator.GetPose(Mesh.Bones, boneMatrices);
                }

                Params.SetParameter("Bones", boneMatrices);
            }

            //CommandBuffer.Enqueue(RenderPass.Overlay, DrawBones);
            Mesh.Render(Materials, Params);
        }

        void PoseByComputeShader()
        {
            // put animation data in structured buffers
            // 

            // on cpu
            // get bone position/rotation/scale from each active clip
            // write to 3 structured buffers
            // write bind pose matrices to another structured buffer
            // dispatch number of bones

            // in compute shader
            // 
        }

        public override void Destroy()
        {
            SpatialNode?.RemoveObject(this);
        }

        private void AutoRagDoll()
        {
            // create a capsule collider for each bone
            // minus the hands
        }

        private void DrawBones()
        {
            int count = boneMatrices.Length;
            var skeleton = Mesh.Bones;

            var world = Mesh.RootRotation * Transform.Matrix;

            for (int i = 0; i < count; i++)
            {
                if (skeleton[i].Parent < 0) continue;
                if (skeleton[skeleton[i].Parent].Parent < 0) continue;

                var matrix = Matrix.Invert(skeleton[i].OffsetMatrix) * boneMatrices[i] * world;
                var matrix2 = Matrix.Invert(skeleton[skeleton[i].Parent].OffsetMatrix) * boneMatrices[skeleton[i].Parent] * world;

                Vector3 from = matrix.TranslationVector;
                Vector3 to = matrix2.TranslationVector;

                float alpha = .5f;

                if (i == 1)
                    alpha = 1;

                Graphics.Lines.Draw(from, to, Vector4.One * alpha);
               // Engine.DrawText(skeleton[i].Name, from);
            }
        }

        public void DrawDebug()
        {
            if (Mesh == null) return;

            Vector4 color = new Vector4(0, 1f, .4f, 1f);
            // draw obb
            Graphics.Lines.Draw(Mesh.BoundingBox, Transform.Matrix, color);
            // draw abb        
            // Graphics.Lines.Draw(BoundingBox, green);
        }

        public bool Intersects(Ray ray, out RaycastResult result)
        {
            result = new RaycastResult();
            var mesh = Mesh;
            if (mesh == null) return false;

            return mesh.Intersects(ray, Entity.Transform.Matrix, out result);
        }
    }
}