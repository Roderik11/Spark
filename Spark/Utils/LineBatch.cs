using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Spark
{
    public class LineBatch
    {
        private List<LineBuffer> Buffers = new List<LineBuffer>();
        private int BufferIndex;
        private Effect Effect;
        private int Stride;

        public static int LinesPerBatch = 4096;

        private class LineBuffer
        {
            public VertexColor[] verts;
            public Buffer buffer;
            public int lines;

            public LineBuffer()
            {
                int stride = Utilities.SizeOf<VertexColor>();

                verts = new VertexColor[LinesPerBatch * 2];

                buffer = new Buffer(Engine.Device, new BufferDescription()
                {
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    OptionFlags = ResourceOptionFlags.None,
                    SizeInBytes = stride * (LinesPerBatch * 2),
                    Usage = ResourceUsage.Dynamic,
                    StructureByteStride = 0
                });
            }

            public void Update()
            {
                Engine.Device.ImmediateContext.MapSubresource(buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out DataStream stream);
                stream.WriteRange(verts);
                Engine.Device.ImmediateContext.UnmapSubresource(buffer, 0);
            }
        }

        public LineBatch()
        {
            Effect = new Effect("linebatch");
            Stride = Utilities.SizeOf<VertexColor>();

            Buffers = new List<LineBuffer>
            {
                new LineBuffer()
            };
        }

        public void Draw(Vector3 from, Vector3 to, Vector4 color)
        {
            var buffer = Buffers[BufferIndex];

            int v0 = buffer.lines * 2;
            int v1 = v0 + 1;

            buffer.verts[v0] = new VertexColor { Position = from, Color = color };
            buffer.verts[v1] = new VertexColor { Position = to, Color = color };

            buffer.lines++;

            if (buffer.lines >= LinesPerBatch - 2)
            {
                BufferIndex++;

                if (Buffers.Count < (BufferIndex + 1))
                    Buffers.Add(new LineBuffer());
            }
        }

        public void Draw(BoundingBox box, Vector4 color)
        {
            Vector3[] points = box.GetCorners();
            DrawBox(ref points, color);
        }

        public void Draw(BoundingBox box, Matrix matrix, Vector4 color)
        {
            Vector3[] points = box.GetCorners(matrix);
            DrawBox(ref points, color);
        }

        public void Draw(BoundingSphere sphere, Vector4 color)
        {
            DrawCircle(sphere.Center, Vector3.UnitY, Vector3.UnitX, sphere.Radius, color);
            DrawCircle(sphere.Center, Vector3.UnitZ, Vector3.UnitX, sphere.Radius, color);
            DrawCircle(sphere.Center, Vector3.UnitY, Vector3.UnitZ, sphere.Radius, color);
        }

        public void Draw(BoundingSphere sphere, Matrix matrix, Vector4 color)
        {
            Vector3 x = matrix.Right;
            Vector3 y = matrix.Up;
            Vector3 z = matrix.Forward;

            DrawCircle(sphere.Center, y, x, sphere.Radius, color);
            DrawCircle(sphere.Center, z, x, sphere.Radius, color);
            DrawCircle(sphere.Center, y, z, sphere.Radius, color);
        }

        public void DrawFrustum(Matrix matrix, Vector4 color)
        {
            Vector3[] v = new Vector3[8];
            v[0] = new Vector3(-1, -1, 0);
            v[1] = new Vector3(1, -1, 0);
            v[2] = new Vector3(-1, 1, 0);
            v[3] = new Vector3(1, 1, 0);
            v[4] = new Vector3(-1, -1, 1);
            v[5] = new Vector3(1, -1, 1);
            v[6] = new Vector3(-1, 1, 1);
            v[7] = new Vector3(1, 1, 1);

            Vector3.TransformCoordinate(v, ref matrix, v);

            Draw(v[0], v[1], color);
            Draw(v[2], v[3], color);
            Draw(v[4], v[5], color);
            Draw(v[6], v[7], color);
            Draw(v[0], v[4], color);
            Draw(v[1], v[5], color);
            Draw(v[2], v[6], color);
            Draw(v[3], v[7], color);
            Draw(v[0], v[2], color);
            Draw(v[1], v[3], color);
            Draw(v[2], v[3], color);
            Draw(v[4], v[6], color);
            Draw(v[5], v[7], color);
        }

        public void DrawCircle(Vector3 center, Vector3 up, Vector3 side, float radius, Vector4 color)
        {
            int numSegments = 128;
            float degreesPerSeg = 360.0f / numSegments;
            float radiansPerSeg = degreesPerSeg * (3.141592f / 180.0f);

            Vector3 firstPoint = center + side * radius;

            Vector3 lastPoint = firstPoint;
            Vector3 thisPoint;

            float thisRadian = 0;
            for (int seg = 0; seg < numSegments; seg++)
            {
                // Increment point
                thisRadian += radiansPerSeg;

                thisPoint = center + up * (float)Math.Sin(thisRadian) * radius;
                thisPoint += side * (float)Math.Cos(thisRadian) * radius;

                // Render
                Draw(lastPoint, thisPoint, color);
                lastPoint = thisPoint;
            }
        }

        private void DrawBox(ref Vector3[] v, Vector4 color)
        {
            Draw(v[0], v[1], color);
            Draw(v[1], v[2], color);
            Draw(v[2], v[3], color);
            Draw(v[3], v[0], color);

            Draw(v[4], v[5], color);
            Draw(v[5], v[6], color);
            Draw(v[6], v[7], color);
            Draw(v[7], v[4], color);

            Draw(v[0], v[4], color);
            Draw(v[1], v[5], color);
            Draw(v[2], v[6], color);
            Draw(v[3], v[7], color);
        }

        public void Start()
        {
        }

        public void End()
        {
            Profiler.Start("Draw Lines");

            Effect.Apply();

            Engine.Device.ImmediateContext.OutputMerger.SetBlendState(States.BlendNone, null, 0xfffffff);
            Engine.Device.ImmediateContext.OutputMerger.SetDepthStencilState(States.ZReadZWriteNoStencil, 0);
            Engine.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            Engine.Device.ImmediateContext.InputAssembler.InputLayout = Effect.GetInputLayout(VertexColor.InputElements);

            foreach (LineBuffer buffer in Buffers)
            {
                if (buffer.lines == 0)
                    break;

                buffer.Update();

                Engine.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(buffer.buffer, Stride, 0));
                Engine.Device.ImmediateContext.Draw(buffer.lines * 2, 0);
            }

            BufferIndex = 0;

            foreach (LineBuffer buffer in Buffers)
                buffer.lines = 0;

            Profiler.Stop();
        }
    }
}