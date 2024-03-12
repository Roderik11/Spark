using System.Collections.Generic;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using System;
using System.IO;
using Assimp.Configs;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Spark
{
    public struct RectF
    {
        public float xMin;
        public float xMax;
        public float yMin;
        public float yMax;

        public float width
        {
            get { return xMax - xMin; }
        }

        public float height
        {
            get { return yMax - yMin; }
        }

        public RectF(float x, float y, float width, float height)
        {
            xMin = x;
            yMin = y;
            xMax = x + width;
            yMax = y + height;
        }
    }

    public enum DrawOperationType
    {
        None,
        Quad,
        Line
    }

    public struct DrawOperation
    {
        public DrawOperationType Type;
        public Texture Texture;
        public int Offset;
        public int DrawCount;
    }

    /// <summary>
    /// simple class to batch textured quads using geometry shader
    /// </summary>
    public class SpriteBatch
    {
        private int Stride;
        private BlendState Blend;
        private List<DrawOperation> Operations = new List<DrawOperation>();
        private Texture WhiteTexture;
        private InputLayout Layout;

        private Buffer buffer;
        private VertexBufferBinding bufferBinding;

        private int index;
        private Sprite[] sprites;
        private int Offset;

        private RenderTexture2D atlas;
        private RectanglePacker rpacker;
        private Effect atlasEffect;
        private DrawOperation lastDrawOp;

        private Effect spriteEffect;

        public SharpDX.Rectangle Scissor;
        public float LineWidth = 4;


        private struct Sprite
        {
            public Vector2 Position;
            public Vector2 Size;
            public Color4 Color;
            public Vector4 UVs;

            public static InputElement[] InputElements = new InputElement[]
                {
                        new InputElement("POSITION", 0, Format.R32G32_Float, 0),
                        new InputElement("SIZE", 0, Format.R32G32_Float, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32B32A32_Float, 0),
                };
        }

        public void SetWhiteTexture(Texture texture)
        {
            WhiteTexture = texture;
        }

        public SpriteBatch()
        {
            Stride = Utilities.SizeOf<Sprite>();
            sprites = new Sprite[1014];

            atlas = new RenderTexture2D(4096, 4096, Format.R8G8B8A8_UNorm, false);
            rpacker = new RectanglePacker(atlas.Description.Width, atlas.Description.Height);

            atlasEffect = new Effect("blit");
            spriteEffect = new Effect("spritebatch");
            Layout = spriteEffect.GetInputLayout(Sprite.InputElements);

            buffer = new Buffer(Engine.Device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = Stride * 4096 * 2,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });

            bufferBinding = new VertexBufferBinding(buffer, Stride, 0);

            BlendStateDescription blendDesc = new BlendStateDescription();
            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                IsBlendEnabled = true,
                SourceBlend = BlendOption.SourceAlpha,
                DestinationBlend = BlendOption.InverseSourceAlpha,
                DestinationAlphaBlend = BlendOption.Zero,
                SourceAlphaBlend = BlendOption.Zero,
                AlphaBlendOperation = BlendOperation.Add,
                BlendOperation = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue
            };

            blendDesc.AlphaToCoverageEnable = false;
            blendDesc.IndependentBlendEnable = false;

            Blend = new BlendState(Engine.Device, blendDesc);

            DataStream stream = new DataStream(4, true, true);
            stream.Write(new byte[4] { 255, 255, 255, 255 }, 0, 4);
            DataRectangle rect = new DataRectangle(stream.DataPointer, 4);

            Texture2D texture = new Texture2D(Engine.Device, new Texture2DDescription
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                Usage = ResourceUsage.Immutable,
                Width = 1,
                Height = 1,
                ArraySize = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }, rect);

            WhiteTexture = new Texture { Resource = texture, View = new ShaderResourceView(Engine.Device, texture) };
        }

        void IncrementIndex()
        {
            index++;
            if (index >= sprites.Length)
                ResizeBuffers();
        }

        public void DrawLine(int x, int y, int x2, int y2, Color4 color)
        {
            Point p1 = new Point(x, y);
            Point p2 = new Point(x2, y2);

            if (!MathHelper.ClipSegment(ref p1, ref p2, Scissor))
                return;

            x = p1.X; y = p1.Y;
            x2 = p2.X; y2 = p2.Y;

            sprites[index] = new Sprite
            {
                Position = new Vector2(x / RenderView.Active.Size.X * 2 - 1, -(y / RenderView.Active.Size.Y * 2 - 1)),
                Size = new Vector2(x2 / RenderView.Active.Size.X * 2 - 1, -(y2 / RenderView.Active.Size.Y * 2 - 1)),
                UVs = new Vector4(0, 0, 1, 1),
                Color = color
            };
            IncrementIndex();

            if (Offset == 0)
            {
                lastDrawOp = new DrawOperation
                {
                    Type = DrawOperationType.Line,
                    Texture = WhiteTexture,
                    DrawCount = 1,
                    Offset = 0
                };
            }
            else if (lastDrawOp.Type == DrawOperationType.Line)
            {
                lastDrawOp.DrawCount++;
            }
            else
            {
                Operations.Add(lastDrawOp);

                lastDrawOp = new DrawOperation
                {
                    Type = DrawOperationType.Line,
                    Texture = WhiteTexture,
                    DrawCount = 1,
                    Offset = Offset
                };
            }

            Offset++;
        }

        public void SaveAtlas(string path)
        {
            atlas.Save(path);
        }

        void CopyToAtlas(Texture texture, int width, int height, Point point)
        {
            // set atlas as target
            Graphics.SetTargets(atlas.Target);
            Graphics.SetViewport(new ViewportF(point.X, point.Y, width, height, 0.0f, 1.0f));
            //Engine.Device.ImmediateContext.Rasterizer.SetViewport(new ViewportF(0, 0, atlas.Description.Width, atlas.Description.Height, 0.0f, 1.0f));
            Graphics.SetScissorRectangle(0, 0, atlas.Description.Width, atlas.Description.Height);
            Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);

            atlasEffect.DepthStencilState = States.ZReadZWriteOff;
            atlasEffect.BlendState = States.BlendNone;

            atlasEffect.SetParameter("MainTexture", texture);
            Graphics.DrawFullscreenQuad(atlasEffect);

            Graphics.SetTargets(RenderView.Active.BackBufferTarget);
            Graphics.SetViewport(new ViewportF(0, 0, RenderView.Active.Size.X, RenderView.Active.Size.Y, 0.0f, 1.0f));
            Graphics.SetScissorRectangle(0, 0, (int)RenderView.Active.Size.X, (int)RenderView.Active.Size.Y);
        }

        struct AtlasElement
        {
            public Point point;
            public Texture texture;
        }

        private bool isAtlasFull;
        private Dictionary<int, AtlasElement> atlasElements = new Dictionary<int, AtlasElement>();
        private Dictionary<int, Stack<AtlasElement>> freeElements = new Dictionary<int, Stack<AtlasElement>>();
        private Dictionary<int, float> slotTime = new Dictionary<int, float>();

        private bool TryToPack(Texture texture, out AtlasElement element)
        {
            int id = texture.GetInstanceId();
            int tw = texture.Description.Width;
            int th = texture.Description.Height;

            if (atlasElements.TryGetValue(id, out element))
            {
                slotTime[id] = Time.TotalTime;
                return true;
            }

            bool result = false;

            if (isAtlasFull)
            {
                int sizeHash = (tw, th).GetHashCode();
                if (freeElements.TryGetValue(sizeHash, out var stack) && stack.Count > 0)
                {
                    element = stack.Pop();
                    int other = element.texture.GetHashCode();
                    atlasElements.Remove(other);
                    element.texture = texture;
                    result = true;
                }
            }
            else if (rpacker.Pack(tw, th, out var point))
            {
                element = new AtlasElement { texture = texture, point = point };
                result = true;
            }
            else
            {
                isAtlasFull = true;
            }

            if (result == true)
            {
                CopyToAtlas(texture, tw, th, element.point);
                atlasElements.Add(id, element);
                slotTime[id] = Time.TotalTime;
            }

            return result;
        }

        public void Draw(int x, int y, int width, int height, RectF rect, Color4 color, Texture texture)
        {
            if (texture == null)
                texture = WhiteTexture;

            int tw = texture.Description.Width;
            int th = texture.Description.Height;

            rect.xMax = Math.Min(tw, rect.xMax);
            rect.yMax = Math.Min(th, rect.yMax);

            // --- Auto Atlas Start --
            if (tw <= 128 && th <= 128)
            {
                bool atlased = TryToPack(texture, out var element);
                
                if(atlased)    
                {
                    texture = atlas;
                    tw = texture.Description.Width;
                    th = texture.Description.Height;

                    rect.xMin += element.point.X;
                    rect.xMax += element.point.X;
                    rect.yMin += element.point.Y;
                    rect.yMax += element.point.Y;
                }
            }
            // --- Auto Atlas End --


            // --- Scissor Start --
            int diff;
            float fx = rect.width / width;
            float fy = rect.height / height;

            // Calculate adjustments for x-axis
            diff = Math.Max(Scissor.Left - x, 0);
            x += diff;
            width = Math.Max(width - diff, 0);
            rect.xMin += diff * fx;

            diff = Math.Max(x + width - Scissor.Right, 0);
            width = Math.Max(width - diff, 0);
            rect.xMax -= diff * fx;

            // Calculate adjustments for y-axis
            diff = Math.Max(Scissor.Top - y, 0);
            y += + diff;
            height = Math.Max(height - diff, 0);
            rect.yMin += diff * fy;

            diff = Math.Max(y + height - Scissor.Bottom, 0);
            height = Math.Max(height - diff, 0);
            rect.yMax -= diff * fy;

            // Check if either width or height is less than 1
            if (width < 1 || height < 1) return;
            // --- Scissor End --

            float oneOverW = 1f / tw;
            float oneOverH = 1f / th;

            rect.xMin *= oneOverW;
            rect.xMax *= oneOverW;

            rect.yMin *= oneOverH;
            rect.yMax *= oneOverH;

            //rect.xMin = MathUtil.Clamp(rect.xMin, 0, 1);
            //rect.xMax = MathUtil.Clamp(rect.xMax, 0, 1);
            //rect.yMin = MathUtil.Clamp(rect.yMin, 0, 1);
            //rect.yMax = MathUtil.Clamp(rect.yMax, 0, 1);

            float vx = RenderView.Active.Size.X;
            float vy = RenderView.Active.Size.Y;

            float oneOverVX = 1f / vx * 2;
            float oneOverVY = 1f / vy * 2;

            ref var sprite = ref sprites[index];
            sprite.Position = new Vector2(x * oneOverVX - 1, -(y * oneOverVY - 1));
            sprite.Size = new Vector2((float)(width + .5f) * oneOverVX, (float)(height + .5f) * oneOverVY);
            sprite.UVs = new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
            sprite.Color = color;

            IncrementIndex();

            if (Offset == 0)
            {
                lastDrawOp = new DrawOperation
                {
                    Type = DrawOperationType.Quad,
                    Texture = texture,
                    DrawCount = 1,
                    Offset = 0
                };
            }
            else if (lastDrawOp.Type == DrawOperationType.Quad && lastDrawOp.Texture == texture)
            {
                lastDrawOp.DrawCount++;
            }
            else
            {
                Operations.Add(lastDrawOp);

                lastDrawOp = new DrawOperation
                {
                    Type = DrawOperationType.Quad,
                    Texture = texture,
                    DrawCount = 1,
                    Offset = Offset
                };
            }

            Offset++;
        }

        public void Start() { }


        private void ResizeBuffers()
        {
            int stride = Utilities.SizeOf<Matrix>();

            Array.Resize(ref sprites, sprites.Length * 2);

            Disposer.SafeDispose(ref buffer);

            buffer = new Buffer(Engine.Device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = stride * sprites.Length,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = stride
            });

            bufferBinding.Buffer = buffer;
        }

        private List<int> slots = new List<int>();

        public void End()
        {
            Engine.Device.ImmediateContext.MapSubresource(buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out DataStream stream);
            stream.WriteRange(sprites, 0, index);
            Engine.Device.ImmediateContext.UnmapSubresource(buffer, 0);

            Engine.Device.ImmediateContext.InputAssembler.InputLayout = Layout;
            Engine.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, bufferBinding);
            Engine.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
            //Engine.Device.ImmediateContext.OutputMerger.SetBlendState(Blend, null, 0xfffffff);

            spriteEffect.SetParameter("LineWidth", LineWidth);
            spriteEffect.SetParameter("ScreenSize", RenderView.Active.Size);
            spriteEffect.SetParameter("linearSampler", Samplers.WrappedPoint2D);
            spriteEffect.BlendState = Blend;

            Operations.Add(lastDrawOp);

            foreach (DrawOperation op in Operations)
            {
                spriteEffect.Pass = (int)op.Type - 1;
                spriteEffect.SetParameter("Albedo", op.Texture);
                spriteEffect.Apply();

                Engine.Device.ImmediateContext.Draw(op.DrawCount, op.Offset);
            }

            Engine.Device.ImmediateContext.GeometryShader.Set(null);

            Operations.Clear();
            Offset = 0;
            index = 0;

            if (!isAtlasFull) return;

            foreach(var pair in slotTime)
            {
                var time = pair.Value;
                if (Time.TotalTime - time > .1f)
                {
                    var id = pair.Key;
                    var elm = atlasElements[id];
                    int tw = elm.texture.Description.Width;
                    int th = elm.texture.Description.Height;
                    int sizeHash = (tw, th).GetHashCode();
                    
                    if(!freeElements.TryGetValue(sizeHash, out var stack))
                    {
                        stack = new Stack<AtlasElement>();
                        freeElements[sizeHash] = stack;
                    }

                    // CopyToAtlas(WhiteTexture, tw, th, elm.point);

                    atlasElements.Remove(id);
                    stack.Push(elm);
                    slots.Add(id);
                }
            }

            if(slots.Count > 0)
            {
                foreach (var slot in slots)
                    slotTime.Remove(slot);
                slots.Clear();
            }
        }
    }
}