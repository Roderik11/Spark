// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// The following code is a port of DirectXTex http://directxtex.codeplex.com
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the 
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and 
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to 
// the software.
// A "contributor" is any person that distributes its contribution under this 
// license.
// "Licensed patents" are a contributor's patent claims that read directly on 
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the 
// license conditions and limitations in section 3, each contributor grants 
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and 
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a 
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution 
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any 
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that 
// you claim are infringed by the software, your patent license from such 
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all 
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you 
// may do so only under this license by including a complete copy of this 
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that 
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license 
// cannot change. To the extent permitted under your local laws, the 
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.
#if !WP8
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX.DXGI;
using SharpDX.WIC;
using SharpDX;
using SharpDX.Direct3D11;

namespace Spark
{
    /// <summary>
    /// Image file format used by <see cref="Image.Save(string,SharpDX.Toolkit.Graphics.ImageFileType)"/>
    /// </summary>
    public enum ImageFileType
    {
        /// <summary>
        /// A DDS file.
        /// </summary>
        Dds,

        /// <summary>
        /// A PNG file.
        /// </summary>
        Png,

        /// <summary>
        /// A GIF file.
        /// </summary>
        Gif,

        /// <summary>
        /// A JPG file.
        /// </summary>
        Jpg,

        /// <summary>
        /// A BMP file.
        /// </summary>
        Bmp,

        /// <summary>
        /// A TIFF file.
        /// </summary>
        Tiff,

        /// <summary>
        /// A WMP file.
        /// </summary>
        Wmp,

        /// <summary>
        /// A TGA File.
        /// </summary>
        Tga,

        /// <summary>
        /// A TKTX File.
        /// </summary>
        /// <remarks>
        /// This is a format available with this toolkit, similar to DDS, but It doesn't require any conversion and is a straight dump of the memory pixel buffers.
        /// </remarks>
        Tktx,
    }

    [Flags]
    internal enum WICFlags
    {
        None = 0x0,

        ForceRgb = 0x1,
        // Loads DXGI 1.1 BGR formats as DXGI_FORMAT_R8G8B8A8_UNORM to avoid use of optional WDDM 1.1 formats

        NoX2Bias = 0x2,
        // Loads DXGI 1.1 X2 10:10:10:2 format as DXGI_FORMAT_R10G10B10A2_UNORM

        No16Bpp = 0x4,
        // Loads 565, 5551, and 4444 formats as 8888 to avoid use of optional WDDM 1.2 formats

        FlagsAllowMono = 0x8,
        // Loads 1-bit monochrome (black & white) as R1_UNORM rather than 8-bit greyscale

        AllFrames = 0x10,
        // Loads all images in a multi-frame file, converting/resizing to match the first frame as needed, defaults to 0th frame otherwise

        Dither = 0x10000,
        // Use ordered 4x4 dithering for any required conversions

        DitherDiffusion = 0x20000,
        // Use error-diffusion dithering for any required conversions

        FilterPoint = 0x100000,
        FilterLinear = 0x200000,
        FilterCubic = 0x300000,
        FilterFant = 0x400000, // Combination of Linear and Box filter
        // Filtering mode to use for any required image resizing (only needed when loading arrays of differently sized images; defaults to Fant)
    };

    public class WICHelper
    {
        private static ImagingFactory _factory = new ImagingFactory();

        private static ImagingFactory Factory { get { return _factory ?? (_factory = new ImagingFactory()); } }

        //-------------------------------------------------------------------------------------
        // WIC Pixel Format Translation Data
        //-------------------------------------------------------------------------------------
        private struct WICTranslate
        {
            public WICTranslate(Guid wic, Format format)
            {
                this.WIC = wic;
                this.Format = format;
            }

            public readonly Guid WIC;
            public readonly Format Format;
        };

        private static readonly WICTranslate[] WICToDXGIFormats =
            {
                new WICTranslate(PixelFormat.Format128bppRGBAFloat, Format.R32G32B32A32_Float),

                new WICTranslate(PixelFormat.Format64bppRGBAHalf, Format.R16G16B16A16_Float),
                new WICTranslate(PixelFormat.Format64bppRGBA, Format.R16G16B16A16_UNorm),

                new WICTranslate(PixelFormat.Format32bppRGBA, Format.R8G8B8A8_UNorm),
                new WICTranslate(PixelFormat.Format32bppBGRA, Format.B8G8R8A8_UNorm), // DXGI 1.1
                new WICTranslate(PixelFormat.Format32bppBGR, Format.B8G8R8X8_UNorm), // DXGI 1.1

                new WICTranslate(PixelFormat.Format32bppRGBA1010102XR, Format.R10G10B10_Xr_Bias_A2_UNorm), // DXGI 1.1
                new WICTranslate(PixelFormat.Format32bppRGBA1010102, Format.R10G10B10A2_UNorm),
                new WICTranslate(PixelFormat.Format32bppRGBE, Format.R9G9B9E5_Sharedexp),

                new WICTranslate(PixelFormat.Format16bppBGRA5551, Format.B5G5R5A1_UNorm),
                new WICTranslate(PixelFormat.Format16bppBGR565, Format.B5G6R5_UNorm),

                new WICTranslate(PixelFormat.Format32bppGrayFloat, Format.R32_Float),
                new WICTranslate(PixelFormat.Format16bppGrayHalf, Format.R16_Float),
                new WICTranslate(PixelFormat.Format16bppGray, Format.R16_UNorm),
                new WICTranslate(PixelFormat.Format8bppGray, Format.R8_UNorm),

                new WICTranslate(PixelFormat.Format8bppAlpha, Format.A8_UNorm),

                new WICTranslate(PixelFormat.FormatBlackWhite, Format.R1_UNorm),

#if DIRECTX11_1
                new WICTranslate(PixelFormat.Format96bppRGBFloat,         Format.R32G32B32_Float ),
        #endif
            };

        //-------------------------------------------------------------------------------------
        // WIC Pixel Format nearest conversion table
        //-------------------------------------------------------------------------------------

        private struct WICConvert
        {
            public WICConvert(Guid source, Guid target)
            {
                this.source = source;
                this.target = target;
            }

            public readonly Guid source;
            public readonly Guid target;
        };

        private static readonly WICConvert[] WICConvertTable =
            {
                // Directly support the formats listed in XnaTexUtil::g_WICFormats, so no conversion required
                // Note target Guid in this conversion table must be one of those directly supported formats.

                new WICConvert(PixelFormat.Format1bppIndexed, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 
                new WICConvert(PixelFormat.Format2bppIndexed, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 
                new WICConvert(PixelFormat.Format4bppIndexed, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 
                new WICConvert(PixelFormat.Format8bppIndexed, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 

                new WICConvert(PixelFormat.Format2bppGray, PixelFormat.Format8bppGray), // Format.R8_UNorm 
                new WICConvert(PixelFormat.Format4bppGray, PixelFormat.Format8bppGray), // Format.R8_UNorm 

                new WICConvert(PixelFormat.Format16bppGrayFixedPoint, PixelFormat.Format16bppGrayHalf), // Format.R16_FLOAT 
                new WICConvert(PixelFormat.Format32bppGrayFixedPoint, PixelFormat.Format32bppGrayFloat), // Format.R32_FLOAT 

                new WICConvert(PixelFormat.Format16bppBGR555, PixelFormat.Format16bppBGRA5551), // Format.B5G5R5A1_UNorm 
                new WICConvert(PixelFormat.Format32bppBGR101010, PixelFormat.Format32bppRGBA1010102), // Format.R10G10B10A2_UNorm

                new WICConvert(PixelFormat.Format24bppBGR, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 
                new WICConvert(PixelFormat.Format24bppRGB, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 
                new WICConvert(PixelFormat.Format32bppPBGRA, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 
                new WICConvert(PixelFormat.Format32bppPRGBA, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 

                new WICConvert(PixelFormat.Format48bppRGB, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm
                new WICConvert(PixelFormat.Format48bppBGR, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm
                new WICConvert(PixelFormat.Format64bppBGRA, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm
                new WICConvert(PixelFormat.Format64bppPRGBA, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm
                new WICConvert(PixelFormat.Format64bppPBGRA, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm

                new WICConvert(PixelFormat.Format48bppRGBFixedPoint, PixelFormat.Format64bppRGBAHalf), // Format.R16G16B16A16_FLOAT 
                new WICConvert(PixelFormat.Format48bppBGRFixedPoint, PixelFormat.Format64bppRGBAHalf), // Format.R16G16B16A16_FLOAT 
                new WICConvert(PixelFormat.Format64bppRGBAFixedPoint, PixelFormat.Format64bppRGBAHalf), // Format.R16G16B16A16_FLOAT 
                new WICConvert(PixelFormat.Format64bppBGRAFixedPoint, PixelFormat.Format64bppRGBAHalf), // Format.R16G16B16A16_FLOAT 
                new WICConvert(PixelFormat.Format64bppRGBFixedPoint, PixelFormat.Format64bppRGBAHalf), // Format.R16G16B16A16_FLOAT 
                new WICConvert(PixelFormat.Format64bppRGBHalf, PixelFormat.Format64bppRGBAHalf), // Format.R16G16B16A16_FLOAT 
                new WICConvert(PixelFormat.Format48bppRGBHalf, PixelFormat.Format64bppRGBAHalf), // Format.R16G16B16A16_FLOAT 

                new WICConvert(PixelFormat.Format128bppPRGBAFloat, PixelFormat.Format128bppRGBAFloat), // Format.R32G32B32A32_FLOAT 
                new WICConvert(PixelFormat.Format128bppRGBFloat, PixelFormat.Format128bppRGBAFloat), // Format.R32G32B32A32_FLOAT 
                new WICConvert(PixelFormat.Format128bppRGBAFixedPoint, PixelFormat.Format128bppRGBAFloat), // Format.R32G32B32A32_FLOAT 
                new WICConvert(PixelFormat.Format128bppRGBFixedPoint, PixelFormat.Format128bppRGBAFloat), // Format.R32G32B32A32_FLOAT 

                new WICConvert(PixelFormat.Format32bppCMYK, PixelFormat.Format32bppRGBA), // Format.R8G8B8A8_UNorm 
                new WICConvert(PixelFormat.Format64bppCMYK, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm
                new WICConvert(PixelFormat.Format40bppCMYKAlpha, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm
                new WICConvert(PixelFormat.Format80bppCMYKAlpha, PixelFormat.Format64bppRGBA), // Format.R16G16B16A16_UNorm

#if DIRECTX11_1
                new WICConvert( PixelFormat.Format32bppRGB,              PixelFormat.Format32bppRGBA ), // Format.R8G8B8A8_UNorm
                new WICConvert( PixelFormat.Format64bppRGB,              PixelFormat.Format64bppRGBA ), // Format.R16G16B16A16_UNorm
                new WICConvert( PixelFormat.Format64bppPRGBAHalf,        PixelFormat.Format64bppRGBAHalf ), // Format.R16G16B16A16_FLOAT 
                new WICConvert( PixelFormat.Format96bppRGBFixedPoint,    PixelFormat.Format96bppRGBFloat ), // Format.R32G32B32_FLOAT 
#else
                new WICConvert(PixelFormat.Format96bppRGBFixedPoint, PixelFormat.Format128bppRGBAFloat), // Format.R32G32B32A32_FLOAT 
#endif

                // We don't support n-channel formats
            };

        /// <summary>
        /// Converts a WIC <see cref="PixelFormat"/> to a <see cref="Format"/>.
        /// </summary>
        /// <param name="guid">A WIC <see cref="PixelFormat"/> </param>
        /// <returns>A <see cref="Format"/></returns>
        private static Format ToDXGI(Guid guid)
        {
            for (int i = 0; i < WICToDXGIFormats.Length; ++i)
            {
                if (WICToDXGIFormats[i].WIC == guid)
                    return WICToDXGIFormats[i].Format;
            }

            return Format.Unknown;
        }

        /// <summary>
        /// Converts a <see cref="Format"/> to a a WIC <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="format">A <see cref="Format"/></param>
        /// <param name="guid">A WIC <see cref="PixelFormat"/> Guid.</param>
        /// <returns>True if conversion succeed, false otherwise.</returns>
        private static bool ToWIC(Format format, out Guid guid)
        {
            for (int i = 0; i < WICToDXGIFormats.Length; ++i)
            {
                if (WICToDXGIFormats[i].Format == format)
                {
                    guid = WICToDXGIFormats[i].WIC;
                    return true;
                }
            }

            // Special cases
            switch (format)
            {
                case Format.R8G8B8A8_UNorm_SRgb:
                    guid = PixelFormat.Format32bppRGBA;
                    return true;

                case Format.D32_Float:
                    guid = PixelFormat.Format32bppGrayFloat;
                    return true;

                case Format.D16_UNorm:
                    guid = PixelFormat.Format16bppGray;
                    return true;

                case Format.B8G8R8A8_UNorm_SRgb:
                    guid = PixelFormat.Format32bppBGRA;
                    return true;

                case Format.B8G8R8X8_UNorm_SRgb:
                    guid = PixelFormat.Format32bppBGR;
                    return true;
            }

            guid = Guid.Empty;
            return false;
        }

        /// <summary>
        /// Gets the number of bits per pixels for a WIC <see cref="PixelFormat"/> Guid.
        /// </summary>
        /// <param name="targetGuid">A WIC <see cref="PixelFormat"/> Guid.</param>
        /// <returns>The number of bits per pixels for a  If this method is failing to calculate the number of pixels, return 0.</returns>
        private static int GetBitsPerPixel(Guid targetGuid)
        {
            using (var info = new ComponentInfo(Factory, targetGuid))
            {
                if (info.ComponentType != ComponentType.PixelFormat)
                    return 0;

                var pixelFormatInfo = info.QueryInterfaceOrNull<PixelFormatInfo>();
                if (pixelFormatInfo == null)
                    return 0;

                int bpp = pixelFormatInfo.BitsPerPixel;
                pixelFormatInfo.Dispose();
                return bpp;
            }
        }


        //-------------------------------------------------------------------------------------
        // Returns the DXGI format and optionally the WIC pixel Guid to convert to
        //-------------------------------------------------------------------------------------
        private static Format DetermineFormat(Guid pixelFormat, WICFlags flags, out Guid pixelFormatOut)
        {
            Format format = ToDXGI(pixelFormat);
            pixelFormatOut = Guid.Empty;

            if (format == Format.Unknown)
            {
                for (int i = 0; i < WICConvertTable.Length; ++i)
                {
                    if (WICConvertTable[i].source == pixelFormat)
                    {
                        pixelFormatOut = WICConvertTable[i].target;

                        format = ToDXGI(WICConvertTable[i].target);
                        Debug.Assert(format != Format.Unknown);
                        break;
                    }
                }
            }

            // Handle special cases based on flags
            switch (format)
            {
                case Format.B8G8R8A8_UNorm: // BGRA
                case Format.B8G8R8X8_UNorm: // BGRX
                    if ((flags & WICFlags.ForceRgb) != 0)
                    {
                        format = Format.R8G8B8A8_UNorm;
                        pixelFormatOut = PixelFormat.Format32bppRGBA;
                    }
                    break;

                case Format.R10G10B10_Xr_Bias_A2_UNorm:
                    if ((flags & WICFlags.NoX2Bias) != 0)
                    {
                        format = Format.R10G10B10A2_UNorm;
                        pixelFormatOut = PixelFormat.Format32bppRGBA1010102;
                    }
                    break;

                case Format.B5G5R5A1_UNorm:
                case Format.B5G6R5_UNorm:
                    if ((flags & WICFlags.No16Bpp) != 0)
                    {
                        format = Format.R8G8B8A8_UNorm;
                        pixelFormatOut = PixelFormat.Format32bppRGBA;
                    }
                    break;

                case Format.R1_UNorm:
                    if ((flags & WICFlags.FlagsAllowMono) == 0)
                    {
                        // By default we want to promote a black & white to greyscale since R1 is not a generally supported D3D format
                        format = Format.R8_UNorm;
                        pixelFormatOut = PixelFormat.Format8bppGray;
                    }
                    break;
            }

            return format;
        }

        /// <summary>
        /// Determines metadata for image
        /// </summary>
        /// <param name="flags">The flags.</param>
        /// <param name="decoder">The decoder.</param>
        /// <param name="frame">The frame.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">If pixel format is not supported.</exception>
        private static Texture2DDescription? DecodeMetadata(WICFlags flags, BitmapDecoder decoder, BitmapFrameDecode frame, out Guid pixelFormat)
        {
            var size = frame.Size;

            var metadata = new Texture2DDescription
            {
                Width = size.Width,
                Height = size.Height,
                MipLevels = 1,
                ArraySize = (flags & WICFlags.AllFrames) != 0 ? decoder.FrameCount : 1,
                Format = DetermineFormat(frame.PixelFormat, flags, out pixelFormat),
            };

            if (metadata.Format == Format.Unknown)
                return null;

            return metadata;
        }

        private static BitmapDitherType GetWICDither(WICFlags flags)
        {
            if ((flags & WICFlags.Dither) != 0)
                return BitmapDitherType.Ordered4x4;

            if ((flags & WICFlags.DitherDiffusion) != 0)
                return BitmapDitherType.ErrorDiffusion;

            return BitmapDitherType.None;
        }


        private static BitmapInterpolationMode GetWICInterp(WICFlags flags)
        {
            if ((flags & WICFlags.FilterPoint) != 0)
                return BitmapInterpolationMode.NearestNeighbor;

            if ((flags & WICFlags.FilterLinear) != 0)
                return BitmapInterpolationMode.Linear;

            if ((flags & WICFlags.FilterCubic) != 0)
                return BitmapInterpolationMode.Cubic;

            return BitmapInterpolationMode.Fant;
        }

        //-------------------------------------------------------------------------------------
        // Decodes a single frame
        //-------------------------------------------------------------------------------------
        private static Texture2D DecodeSingleFrame(WICFlags flags, Texture2DDescription metadata, Guid convertGUID, BitmapFrameDecode frame)
        {
            var context = Engine.Device.ImmediateContext;

            var image = new Texture2D(Engine.Device, metadata);

            var box = context.MapSubresource(image, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);

            if (convertGUID == Guid.Empty)
            {
                frame.CopyPixels(box.RowPitch, box.DataPointer, box.SlicePitch);
            }
            else
            {
                using (var converter = new FormatConverter(Factory))
                {
                    converter.Initialize(frame, convertGUID, GetWICDither(flags), null, 0, BitmapPaletteType.Custom);
                    converter.CopyPixels(box.RowPitch, box.DataPointer, box.SlicePitch);
                }
            }

            context.UnmapSubresource(image, 0);
            return image;
        }

        //-------------------------------------------------------------------------------------
        // Decodes an image array, resizing/format converting as needed
        //-------------------------------------------------------------------------------------
        private static Texture2D DecodeMultiframe(WICFlags flags, Texture2DDescription metadata, BitmapDecoder decoder)
        {
            var image = new Texture2D(Engine.Device, metadata);
            var context = Engine.Device.ImmediateContext;

            
            Guid sourceGuid;
            if (!ToWIC(metadata.Format, out sourceGuid))
                return null;

            for (int index = 0; index < metadata.ArraySize; ++index)
            {
                var box = context.MapSubresource(image, index, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);

                using (var frame = decoder.GetFrame(index))
                {
                    var pfGuid = frame.PixelFormat;
                    var size = frame.Size;

                    if (pfGuid == sourceGuid)
                    {
                        if (size.Width == metadata.Width && size.Height == metadata.Height)
                        {
                            // This frame does not need resized or format converted, just copy...
                            frame.CopyPixels(box.RowPitch, box.DataPointer, box.SlicePitch);
                        }
                        else
                        {
                            // This frame needs resizing, but not format converted
                            using (var scaler = new BitmapScaler(Factory))
                            {
                                scaler.Initialize(frame, metadata.Width, metadata.Height, GetWICInterp(flags));
                                scaler.CopyPixels(box.RowPitch, box.DataPointer, box.SlicePitch);
                            }
                        }
                    }
                    else
                    {
                        // This frame required format conversion
                        using (var converter = new FormatConverter(Factory))
                        {
                            converter.Initialize(frame, pfGuid, GetWICDither(flags), null, 0, BitmapPaletteType.Custom);

                            if (size.Width == metadata.Width && size.Height == metadata.Height)
                            {
                                converter.CopyPixels(box.RowPitch, box.DataPointer, box.SlicePitch);
                            }
                            else
                            {
                                // This frame needs resizing, but not format converted
                                using (var scaler = new BitmapScaler(Factory))
                                {
                                    scaler.Initialize(frame, metadata.Width, metadata.Height, GetWICInterp(flags));
                                    scaler.CopyPixels(box.RowPitch, box.DataPointer, box.SlicePitch);
                                }
                            }
                        }
                    }
                }
            }

            context.UnmapSubresource(image, 0);

            return image;
        }

        //-------------------------------------------------------------------------------------
        // Load a WIC-supported file in memory
        //-------------------------------------------------------------------------------------
        internal static Texture2D LoadFromWICMemory(IntPtr pSource, int size, bool makeACopy, GCHandle? handle)
        {
            var flags = WICFlags.AllFrames;

            Texture2D image = null;
            // Create input stream for memory
            using (var stream = new WICStream(Factory, new DataPointer(pSource, size)))
            {
                // If the decoder is unable to decode the image, than return null
                BitmapDecoder decoder = null;
                try
                {
                    decoder = new BitmapDecoder(Factory, stream, DecodeOptions.CacheOnDemand);
                    using (var frame = decoder.GetFrame(0))
                    {
                        // Get metadata
                        Guid convertGuid;
                        var tempDesc = DecodeMetadata(flags, decoder, frame, out convertGuid);

                        // If not supported.
                        if (!tempDesc.HasValue)
                            return null;

                        var mdata = tempDesc.Value;

                        if ((mdata.ArraySize > 1) && (flags & WICFlags.AllFrames) != 0)
                        {
                            return DecodeMultiframe(flags, mdata, decoder);
                        }

                        image = DecodeSingleFrame(flags, mdata, convertGuid, frame);
                    }
                }
                catch
                {
                    image = null;
                }
                finally
                {
                    if (decoder != null)
                        decoder.Dispose();
                }
            }

            // For WIC, we are not keeping the original buffer.
            if (image != null && !makeACopy)
            {
                if (handle.HasValue)
                {
                    handle.Value.Free();
                }
                else
                {
                    Utilities.FreeMemory(pSource);
                }
            }
            return image;
        }

        //-------------------------------------------------------------------------------------
        // Encodes a single frame
        //-------------------------------------------------------------------------------------
        //private static void EncodeImage(Texture2D image, WICFlags flags, BitmapFrameEncode frame)
        //{
        //    Guid pfGuid;
        //    if (!ToWIC(image.Description.Format, out pfGuid))
        //        throw new NotSupportedException("Format not supported");

        //    frame.Initialize();
        //    frame.SetSize(image.Description.Width, image.Description.Height);
        //    frame.SetResolution(72, 72);
        //    Guid targetGuid = pfGuid;
        //    frame.SetPixelFormat(ref targetGuid);

        //    if (targetGuid != pfGuid)
        //    {
        //        using (var source = new Bitmap(Factory, image.Description.Width, image.Description.Height, pfGuid, new DataRectangle(image.DataPointer, image.RowStride), image.BufferStride))
        //        {
        //            using (var converter = new FormatConverter(Factory))
        //            {
        //                using (var palette = new Palette(Factory))
        //                {
        //                    palette.Initialize(source, 256, true);
        //                    converter.Initialize(source, targetGuid, GetWICDither(flags), palette, 0, BitmapPaletteType.Custom);

        //                    int bpp = GetBitsPerPixel(targetGuid);
        //                    if (bpp == 0) throw new NotSupportedException("Unable to determine the Bpp for the target format");

        //                    int rowPitch = (image.Width * bpp + 7) / 8;
        //                    int slicePitch = rowPitch * image.Height;

        //                    var temp = Utilities.AllocateMemory(slicePitch);
        //                    try
        //                    {
        //                        converter.CopyPixels(rowPitch, temp, slicePitch);
        //                        frame.Palette = palette;
        //                        frame.WritePixels(image.Height, temp, rowPitch, slicePitch);
        //                    }
        //                    finally
        //                    {
        //                        Utilities.FreeMemory(temp);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        // No conversion required
        //        frame.WritePixels(image.Height, image.DataPointer, image.RowStride, image.BufferStride);
        //    }

        //    frame.Commit();
        //}

        //private static void EncodeSingleFrame(PixelBuffer pixelBuffer, WICFlags flags, Guid guidContainerFormat, Stream stream)
        //{
        //    using (var encoder = new BitmapEncoder(Factory, guidContainerFormat, stream))
        //    {
        //        using (var frame = new BitmapFrameEncode(encoder))
        //        {
        //            if (guidContainerFormat == ContainerFormatGuids.Bmp)
        //            {
        //                try
        //                {
        //                    frame.Options.Set("EnableV5Header32bppBGRA", true);
        //                }
        //                catch
        //                {
        //                }
        //            }
        //            EncodeImage(pixelBuffer, flags, frame);
        //            encoder.Commit();
        //        }
        //    }
        //}

        //-------------------------------------------------------------------------------------
        // Encodes an image array
        //-------------------------------------------------------------------------------------
        //private static void EncodeMultiframe(PixelBuffer[] images, int count, WICFlags flags, Guid guidContainerFormat, Stream stream)
        //{
        //    if (images.Length < 2)
        //        throw new ArgumentException("Cannot encode to multiple frame. Image doesn't have multiple frame");

        //    using (var encoder = new BitmapEncoder(Factory, guidContainerFormat))
        //    {
        //        using (var eInfo = encoder.EncoderInfo)
        //        {
        //            if (!eInfo.IsMultiframeSupported)
        //                throw new NotSupportedException("Cannot encode to multiple frame. Format is not supporting multiple frame");
        //        }

        //        encoder.Initialize(stream);

        //        for (int i = 0; i < Math.Min(images.Length, count); i++)
        //        {
        //            var pixelBuffer = images[i];
        //            using (var frame = new BitmapFrameEncode(encoder))
        //                EncodeImage(pixelBuffer, flags, frame);
        //        }

        //        encoder.Commit();
        //    }
        //}

        //private static Guid GetContainerFormatFromFileType(ImageFileType fileType)
        //{
        //    switch (fileType)
        //    {
        //        case ImageFileType.Bmp:
        //            return ContainerFormatGuids.Bmp;
        //        case ImageFileType.Jpg:
        //            return ContainerFormatGuids.Jpeg;
        //        case ImageFileType.Gif:
        //            return ContainerFormatGuids.Gif;
        //        case ImageFileType.Png:
        //            return ContainerFormatGuids.Png;
        //        case ImageFileType.Tiff:
        //            return ContainerFormatGuids.Tiff;
        //        case ImageFileType.Wmp:
        //            return ContainerFormatGuids.Wmp;
        //        default:
        //            throw new NotSupportedException("Format not supported");
        //    }
        //}

        //internal static void SaveGifToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        //{
        //    SaveToWICMemory(pixelBuffers, count, WICFlags.AllFrames, ImageFileType.Gif, imageStream);
        //}

        //internal static void SaveTiffToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        //{
        //    SaveToWICMemory(pixelBuffers, count, WICFlags.AllFrames, ImageFileType.Tiff, imageStream);
        //}

        //internal static void SaveBmpToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        //{
        //    SaveToWICMemory(pixelBuffers, 1, WICFlags.None, ImageFileType.Bmp, imageStream);
        //}

        //internal static void SaveJpgToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        //{
        //    SaveToWICMemory(pixelBuffers, 1, WICFlags.None, ImageFileType.Jpg, imageStream);
        //}

        //internal static void SavePngToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        //{
        //    SaveToWICMemory(pixelBuffers, 1, WICFlags.None, ImageFileType.Png, imageStream);
        //}

        //internal static void SaveWmpToWICMemory(PixelBuffer[] pixelBuffers, int count, ImageDescription description, Stream imageStream)
        //{
        //    SaveToWICMemory(pixelBuffers, 1, WICFlags.None, ImageFileType.Wmp, imageStream);
        //}

        //private static void SaveToWICMemory(PixelBuffer[] pixelBuffer, int count, WICFlags flags, ImageFileType fileType, Stream stream)
        //{
        //    if (count > 1)
        //        EncodeMultiframe(pixelBuffer, count, flags, GetContainerFormatFromFileType(fileType), stream);
        //    else
        //        EncodeSingleFrame(pixelBuffer[0], flags, GetContainerFormatFromFileType(fileType), stream);
        //}

        public static void Dispose()
        {
            Utilities.Dispose(ref _factory);
        }
    }
}
#endif