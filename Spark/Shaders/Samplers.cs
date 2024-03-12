using SharpDX.Direct3D11;

namespace Spark
{
    public static class Samplers
    {
        public static SamplerState WrappedPoint { get; set; }
        public static SamplerState WrappedBilinear { get; set; }
        public static SamplerState WrappedTrilinear { get; set; }
        public static SamplerState WrappedAnisotropic { get; set; }

        public static SamplerState ClampedPoint { get; set; }
        public static SamplerState ClampedBilinear { get; set; }
        public static SamplerState ClampedTrilinear { get; set; }
        public static SamplerState ClampedAnisotropic { get; set; }

        public static SamplerState WrappedBilinear2D { get; set; }
        public static SamplerState WrappedPoint2D { get; set; }

        public static SamplerState ClampedBilinear2D { get; set; }
        public static SamplerState ClampedPoint2D { get; set; }

        static Samplers()
        {
            WrappedPoint = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            WrappedBilinear = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagLinearMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            WrappedTrilinear = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            WrappedAnisotropic = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 16,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            ClampedPoint = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            ClampedBilinear = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagLinearMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            ClampedTrilinear = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            ClampedAnisotropic = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.Anisotropic,
                MaximumAnisotropy = 16,
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            WrappedBilinear2D = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagLinearMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = 0
            });

            ClampedBilinear2D = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagLinearMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = 0
            });

            WrappedPoint2D = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                Filter = Filter.MinMagMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = 0
            });

            ClampedPoint2D = new SamplerState(Engine.Device, new SamplerStateDescription
            {
                ComparisonFunction = Comparison.Always,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                Filter = Filter.MinMagMipPoint,
                MaximumAnisotropy = 0,
                MinimumLod = 0,
                MaximumLod = 0
            });
        }
    }
}