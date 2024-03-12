using System;
using SharpDX.Direct3D11;

namespace Spark
{
    public class Texture : Asset, IDisposable
    {
        private int _instanceId = _instanceCount++;

        private static volatile int _instanceCount;

        private Texture2D _resource;

        public Texture2D Resource
        {
            get { return _resource; }
            set { _resource = value;
                Description = value.Description;
            }
        }

        public ShaderResourceView View;
        public RenderTargetView Target;
        public Texture2DDescription Description;
        public UnorderedAccessView UnorderedAccess;

        public int GetInstanceId()
        {
            return _instanceId;
        }

        public void Dispose()
        {
            Disposer.SafeDispose(ref _resource);
            Disposer.SafeDispose(ref View);
            Disposer.SafeDispose(ref Target);
        }

        public static Texture2D FromFile(Device device, string filename, ImageLoadInformation description)
        {
            if (filename.ToLower().EndsWith(".dds"))
            {
                DDSTextureLoader.CreateDDSTextureFromFileEx(device, device.ImmediateContext, filename, 4096 * 2, ResourceUsage.Immutable, BindFlags.ShaderResource, CpuAccessFlags.None, ResourceOptionFlags.None, false, out var result, out var view, out var alpha);
                return result as Texture2D;
            }


            return LoadTexture(device, filename, description);
        }

        public static Texture2D FromFile(Device device, string filename, ResourceUsage usage = ResourceUsage.Default)
        {
            var desc = new ImageLoadInformation
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Usage = usage,
            };

            if (filename.ToLower().EndsWith(".dds"))
            {
                DDSTextureLoader.CreateDDSTextureFromFileEx(device, device.ImmediateContext, filename, 4096 * 2, usage, BindFlags.ShaderResource, CpuAccessFlags.None, ResourceOptionFlags.None, false, out var result, out var view, out var alpha);
             
                return result as Texture2D;
            }

            return LoadTexture(device, filename, desc);
        }

        static SharpDX.WIC.BitmapSource LoadBitmap(SharpDX.WIC.ImagingFactory2 factory, string filename, ImageLoadInformation desc)
        {
            var bitmapDecoder = new SharpDX.WIC.BitmapDecoder(
                factory,
                filename,
                SharpDX.WIC.DecodeOptions.CacheOnDemand
                );

            var formatConverter = new SharpDX.WIC.FormatConverter(factory);

            formatConverter.Initialize(
                bitmapDecoder.GetFrame(0),
                SharpDX.WIC.PixelFormat.Format32bppPRGBA,
                SharpDX.WIC.BitmapDitherType.None,
                null,
                0.0,
                SharpDX.WIC.BitmapPaletteType.Custom);

            if (desc.Width > 0 && desc.Height > 0 && (formatConverter.Size.Width != desc.Width || formatConverter.Size.Height != desc.Height))
            {
                var scaler = new SharpDX.WIC.BitmapScaler(factory);
                scaler.Initialize(formatConverter, desc.Width, desc.Height, SharpDX.WIC.BitmapInterpolationMode.Fant);
                return scaler;
            }

            return formatConverter;

        }

        static Texture2D LoadTexture(Device device, string filename, ImageLoadInformation desc)
        {
            SharpDX.WIC.ImagingFactory2 factory = new SharpDX.WIC.ImagingFactory2();
            var bitmap = LoadBitmap(factory, filename, desc);
            var result = CreateTexture2DFromBitmap(device, bitmap);
            bitmap.Dispose();
            return result;
        }

        static Texture2D CreateTexture2DFromBitmap(Device device, SharpDX.WIC.BitmapSource bitmapSource)
        {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true))
            {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);

                return new Texture2D(device, new Texture2DDescription()
                {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    Usage = ResourceUsage.Immutable,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, new SharpDX.DataRectangle(buffer.DataPointer, stride));
            }
        }
    }

    public struct ImageLoadInformation
    {
        public int Width;
        public int Height;
        public SharpDX.DXGI.Format Format;
        public int MipLevels;
        public CpuAccessFlags CpuAccessFlags;
        public ResourceOptionFlags OptionFlags;
        public ResourceUsage Usage;
        public BindFlags BindFlags;
    }
}