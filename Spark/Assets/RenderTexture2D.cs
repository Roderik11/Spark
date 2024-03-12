using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Collections.Generic;

namespace Spark
{
    public class RenderTexture2D : Texture
    {
        private static Dictionary<int, Stack<RenderTexture2D>> temporary = new Dictionary<int, Stack<RenderTexture2D>>();

        private Stack<RenderTexture2D> myStack;

        public static RenderTexture2D GetTemporary(int width, int height, Format format, bool useMultiSampling)
        {
            int hash = (width, height, format, useMultiSampling).GetHashCode();

            if (!temporary.TryGetValue(hash, out var stack))
            {
                stack = new Stack<RenderTexture2D>();
                temporary[hash] = stack;
            }

            if (stack.Count == 0)
            {
                var result = new RenderTexture2D(width, height, format, useMultiSampling)
                {
                    myStack = stack
                };

                return result;
            }
            else
            {
                var result = stack.Pop();
                result.myStack = stack;
                return result;
            }
        }
        
        public void Release()
        {
            if (myStack == null) return;
            myStack.Push(this);
            myStack = null;
        }

        public RenderTexture2D(int width, int height, Format format, bool useMultiSampling = false, bool randomWrite = false)
        {
            Description = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = useMultiSampling ? 1 : 0,
                ArraySize = 1,
                Format = format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            if (randomWrite)
            {
             //   Description.BindFlags &= ~BindFlags.RenderTarget;
                Description.BindFlags |= BindFlags.UnorderedAccess;
            }

            Resource = new Texture2D(Engine.Device, Description);

            View = new ShaderResourceView(Engine.Device, Resource);
            
            if (randomWrite)
                UnorderedAccess = new UnorderedAccessView(Engine.Device, Resource);

            RenderTargetViewDescription viewDesc = new RenderTargetViewDescription
            {
                Format = format,
                Dimension = useMultiSampling ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D,
            };

            Target = new RenderTargetView(Engine.Device, Resource, viewDesc);
        }
    }

    public class DepthTexture2D : Texture
    {
        public DepthStencilView DepthStencilView;

        public DepthTexture2D(int width, int height)
        {
            Description = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format =  Format.R32_Typeless,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            var depthview = new DepthStencilViewDescription
            {
                Dimension = DepthStencilViewDimension.Texture2D,
                Format = Format.D32_Float,
            };
            depthview.Texture2D.MipSlice = 0;
        
            var viewdesc = new ShaderResourceViewDescription
            {
                Format = Format.R32_Float,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D
            };
            viewdesc.Texture2D.MipLevels = Description.MipLevels;
            viewdesc.Texture2D.MostDetailedMip = 0;
           
            Resource = new Texture2D(Engine.Device, Description);
            DepthStencilView = new DepthStencilView(Engine.Device, Resource, depthview);
            View = new ShaderResourceView(Engine.Device, Resource, viewdesc);
        }
    }


    public class DepthTextureArray2D : Texture
    {
        public List<DepthStencilView> DepthStencilViews;

        public DepthTextureArray2D(int width, int height, int slices)
        {
            DepthStencilViews = new List<DepthStencilView>();

            Description = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = slices,
                Format = Format.R32_Typeless,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            Resource = new Texture2D(Engine.Device, Description);

            for (int i = 0; i < slices; i++)
            {
                var depthview = new DepthStencilViewDescription
                {
                    Dimension = DepthStencilViewDimension.Texture2DArray,
                    Format = Format.D32_Float,
                };
                depthview.Texture2DArray.MipSlice = 0;
                depthview.Texture2DArray.ArraySize = 1;
                depthview.Texture2DArray.FirstArraySlice = i;
                DepthStencilViews.Add(new DepthStencilView(Engine.Device, Resource, depthview));
            }

            var viewdesc = new ShaderResourceViewDescription
            {
                Format = Format.R32_Float,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DArray,
            };

            viewdesc.Texture2DArray.FirstArraySlice = 0;
            viewdesc.Texture2DArray.MostDetailedMip = 0;
            viewdesc.Texture2DArray.MipLevels = 1;
            viewdesc.Texture2DArray.ArraySize = slices;

            View = new ShaderResourceView(Engine.Device, Resource, viewdesc);
        }
    }
}