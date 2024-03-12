using System;
using System.ComponentModel;
using SharpDX;
using SharpDX.Direct3D11;

namespace Spark.Noise
{
    /// <summary>
    /// Provides a two-dimensional noise map.
    /// </summary>
    public class Noise2D : IDisposable
    {
        #region Constants

        public const double South = -90.0;
        public const double North = 90.0;
        public const double West = -180.0;
        public const double East = 180.0;
        public const double AngleMin = -180.0;
        public const double AngleMax = 180.0;

        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }

        public bool Seamless { get; set; }

        #endregion Constants

        #region Fields

        private int m_width = 0;
        private int m_height = 0;
        private float m_borderValue = float.NaN;
        public float[,] m_data = null;
        private ModuleBase m_generator = null;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        public Noise2D()
            : this(128, 128, null)
        {
            Left = -1.0;
            Right = 1.0;
            Top = -1.0;
            Bottom = 1.0;
        }

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="size">The width and height of the noise map.</param>
        public Noise2D(int size)
            : this(size, size, null)
        {
            Left = -1.0;
            Right = 1.0;
            Top = -1.0;
            Bottom = 1.0;
        }

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="size">The width and height of the noise map.</param>
        /// <param name="generator">The generator module.</param>
        public Noise2D(int size, ModuleBase generator)
            : this(size, size, generator)
        {
            Left = -1.0;
            Right = 1.0;
            Top = -1.0;
            Bottom = 1.0;
        }

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="width">The width of the noise map.</param>
        /// <param name="height">The height of the noise map.</param>
        public Noise2D(int width, int height)
            : this(width, height, null)
        {
            Left = -1.0;
            Right = 1.0;
            Top = -1.0;
            Bottom = 1.0;
        }

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        /// <param name="width">The width of the noise map.</param>
        /// <param name="height">The height of the noise map.</param>
        /// <param name="generator">The generator module.</param>
        public Noise2D(int width, int height, ModuleBase generator)
        {
            this.m_generator = generator;
            this.m_width = width;
            this.m_height = height;
            this.m_data = new float[width, height];

            Left = -1.0;
            Right = 1.0;
            Top = -1.0;
            Bottom = 1.0;
        }

        #endregion Constructors

        #region Indexers

        /// <summary>
        /// Gets or sets a value in the noise map by its position.
        /// </summary>
        /// <param name="x">The position on the x-axis.</param>
        /// <param name="y">The position on the y-axis.</param>
        /// <returns>The corresponding value.</returns>
        //public float this[int x, int y]
        //{
        //    get
        //    {
        //        if (x < 0 && x >= this.m_width)
        //        {
        //            throw new ArgumentOutOfRangeException();
        //        }
        //        if (y < 0 && y >= this.m_height)
        //        {
        //            throw new ArgumentOutOfRangeException();
        //        }
        //        return this.m_data[x, y];
        //    }
        //    set
        //    {
        //        if (x < 0 && x >= this.m_width)
        //        {
        //            throw new ArgumentOutOfRangeException();
        //        }
        //        if (y < 0 && y >= this.m_height)
        //        {
        //            throw new ArgumentOutOfRangeException();
        //        }
        //        this.m_data[x, y] = value;
        //    }
        //}

        #endregion Indexers

        #region Properties

        /// <summary>
        /// Gets or sets the constant value at the noise maps borders.
        /// </summary>
        [Browsable(false)]
        public float Border
        {
            get { return this.m_borderValue; }
            set { this.m_borderValue = value; }
        }

        /// <summary>
        /// Gets or sets the generator module.
        /// </summary>
        public ModuleBase Generator
        {
            get { return this.m_generator; }
            set { this.m_generator = value; }
        }

        /// <summary>
        /// Gets the height of the noise map.
        /// </summary>
        public int Height
        {
            get
            {
                return this.m_height;
            }
            set
            {
                if (value != m_height)
                {
                    m_height = value;
                    m_data = new float[m_width, m_height];
                }
            }
        }

        /// <summary>
        /// Gets the width of the noise map.
        /// </summary>
        public int Width
        {
            get
            {
                return this.m_width;
            }
            set
            {
                if (value != m_width)
                {
                    m_width = value;
                    m_data = new float[m_width, m_height];
                }
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Clears the noise map.
        /// </summary>
        public void Clear()
        {
            this.Clear(0.0f);
        }

        /// <summary>
        /// Clears the noise map.
        /// </summary>
        /// <param name="value">The constant value to clear the noise map with.</param>
        public void Clear(float value)
        {
            for (int x = 0; x < this.m_width; x++)
            {
                for (int y = 0; y < this.m_height; y++)
                {
                    this.m_data[x, y] = value;
                }
            }
        }

        /// <summary>
        /// Generates a cylindrical projection of a point in the noise map.
        /// </summary>
        /// <param name="angle">The angle of the point.</param>
        /// <param name="height">The height of the point.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GenerateCylindrical(double angle, double height)
        {
            double x = Math.Cos(angle * NoiseUtils.DegToRad);
            double y = height;
            double z = Math.Sin(angle * NoiseUtils.DegToRad);
            return this.m_generator.GetValue(x, y, z);
        }

        /// <summary>
        /// Generates a cylindrical projection of the noise map.
        /// </summary>
        /// <param name="angleMin">The maximum angle of the clip region.</param>
        /// <param name="angleMax">The minimum angle of the clip region.</param>
        /// <param name="heightMin">The minimum height of the clip region.</param>
        /// <param name="heightMax">The maximum height of the clip region.</param>
        public void GenerateCylindrical(double angleMin, double angleMax, double heightMin, double heightMax)
        {
            if (angleMax <= angleMin || heightMax <= heightMin || this.m_generator == null)
            {
                throw new ArgumentException();
            }
            double ae = angleMax - angleMin;
            double he = heightMax - heightMin;
            double xd = ae / (double)this.m_width;
            double yd = he / (double)this.m_height;
            double ca = angleMin;
            double ch = heightMin;
            for (int x = 0; x < this.m_width; x++)
            {
                ch = heightMin;
                for (int y = 0; y < this.m_height; y++)
                {
                    this.m_data[x, y] = (float)this.GenerateCylindrical(ca, ch);
                    ch += yd;
                }
                ca += xd;
            }
        }

        /// <summary>
        /// Generates a planar projection of a point in the noise map.
        /// </summary>
        /// <param name="x">The position on the x-axis.</param>
        /// <param name="y">The position on the y-axis.</param>
        /// <returns>The corresponding noise map value.</returns>
        public double GeneratePlanar(double x, double y)
        {
            return this.m_generator.GetValue(x, 0.0, y);
        }

        /// <summary>
        /// Generates a planar projection of the noise map.
        /// </summary>
        /// <param name="left">The clip region to the left.</param>
        /// <param name="right">The clip region to the right.</param>
        /// <param name="top">The clip region to the top.</param>
        /// <param name="bottom">The clip region to the bottom.</param>
        public void GeneratePlanar(double left, double right, double top, double bottom)
        {
            this.GeneratePlanar(left, right, top, bottom, false);
        }

        public void GeneratePlanar()
        {
            this.GeneratePlanar(Left, Right, Top, Bottom, Seamless);
        }

        public float Minimum;
        public float Maximum;

        /// <summary>
        /// Generates a non-seamless planar projection of the noise map.
        /// </summary>
        /// <param name="left">The clip region to the left.</param>
        /// <param name="right">The clip region to the right.</param>
        /// <param name="top">The clip region to the top.</param>
        /// <param name="bottom">The clip region to the bottom.</param>
        /// <param name="seamless">Indicates whether the resulting noise map should be seamless.</param>
        public void GeneratePlanar(double left, double right, double top, double bottom, bool seamless)
        {
            if (right <= left || bottom <= top || this.m_generator == null)
            {
                // throw new ArgumentException();
                return;
            }

            double xe = right - left;
            double ze = bottom - top;
            double xd = xe / (double)this.m_width;
            double zd = ze / (double)this.m_height;
            double xc = left;
            double zc = top;
            float fv = 0.0f;
            for (int x = 0; x < this.m_width; x++)
            {
                zc = top;
                for (int z = 0; z < this.m_height; z++)
                {
                    if (!seamless) { fv = (float)this.GeneratePlanar(xc, zc); }
                    else
                    {
                        double swv = this.GeneratePlanar(xc, zc);
                        double sev = this.GeneratePlanar(xc + xe, zc);
                        double nwv = this.GeneratePlanar(xc, zc + ze);
                        double nev = this.GeneratePlanar(xc + xe, zc + ze);
                        double xb = 1.0 - ((xc - left) / xe);
                        double zb = 1.0 - ((zc - top) / ze);
                        double z0 = NoiseUtils.InterpolateLinear(swv, sev, xb);
                        double z1 = NoiseUtils.InterpolateLinear(nwv, nev, xb);
                        fv = (float)NoiseUtils.InterpolateLinear(z0, z1, zb);
                    }
                    this.m_data[x, z] = fv;

                    zc += zd;
                }
                xc += xd;
            }
        }

        /// <summary>
        /// Generates a spherical projection of a point in the noise map.
        /// </summary>
        /// <param name="lat">The latitude of the point.</param>
        /// <param name="lon">The longitude of the point.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GenerateSpherical(double lat, double lon)
        {
            double r = Math.Cos(NoiseUtils.DegToRad * lat);
            return this.m_generator.GetValue(r * Math.Cos(NoiseUtils.DegToRad * lon), Math.Sin(NoiseUtils.DegToRad * lat),
                r * Math.Sin(NoiseUtils.DegToRad * lon));
        }

        /// <summary>
        /// Generates a spherical projection of the noise map.
        /// </summary>
        /// <param name="south">The clip region to the south.</param>
        /// <param name="north">The clip region to the north.</param>
        /// <param name="west">The clip region to the west.</param>
        /// <param name="east">The clip region to the east.</param>
        public void GenerateSpherical(double south, double north, double west, double east)
        {
            if (east <= west || north <= south || this.m_generator == null)
            {
                throw new ArgumentException();
            }
            double loe = east - west;
            double lae = north - south;
            double xd = loe / (double)this.m_width;
            double yd = lae / (double)this.m_height;
            double clo = west;
            double cla = south;
            for (int x = 0; x < this.m_width; x++)
            {
                cla = south;
                for (int y = 0; y < this.m_height; y++)
                {
                    this.m_data[x, y] = (float)this.GenerateSpherical(cla, clo);
                    cla += yd;
                }
                clo += xd;
            }
        }

        /// <summary>
        /// Creates a normal map for the current content of the noise map.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="scale">The scaling of the normal map values.</param>
        /// <returns>The created normal map.</returns>
        public Texture2D GetNormalMap(float scale)
        {
            int[] data = new int[this.m_width * this.m_height];

            for (int y = 0; y < this.m_height; y++)
            {
                for (int x = 0; x < this.m_width; x++)
                {
                    Vector3 normX = Vector3.Zero;
                    Vector3 normY = Vector3.Zero;
                    Vector3 normalVector = new Vector3();
                    if (x > 0 && y > 0 && x < this.m_width - 1 && y < this.m_height - 1)
                    {
                        normX = new Vector3((this.m_data[x - 1, y] - this.m_data[x + 1, y]) / 2 * scale, 0, 1);
                        normY = new Vector3(0, (this.m_data[x, y - 1] - this.m_data[x, y + 1]) / 2 * scale, 1);
                        normalVector = Vector3.Normalize(normX + normY);

                        Vector3 texVector = Vector3.Zero;
                        texVector.X = (normalVector.X + 1) / 2f;
                        texVector.Y = (normalVector.Y + 1) / 2f;
                        texVector.Z = (normalVector.Z + 1) / 2f;

                        data[x + y * this.m_height] = new Color4(texVector, 1).ToRgba();
                    }
                    else
                    {
                        normX = new Vector3(0, 0, 1);
                        normY = new Vector3(0, 0, 1);
                        normalVector = Vector3.Normalize(normX + normY);

                        Vector3 texVector = Vector3.Zero;
                        texVector.X = (normalVector.X + 1) / 2f;
                        texVector.Y = (normalVector.Y + 1) / 2f;
                        texVector.Z = (normalVector.Z + 1) / 2f;

                        data[x + y * this.m_height] = new Color4(texVector, 1).ToRgba();
                    }
                }
            }

            DataStream stream = new DataStream(data.Length * 4, true, true);
            stream.WriteRange(data);

            DataRectangle rect = new DataRectangle(stream.DataPointer, m_width * 4);

            Texture2D res = new Texture2D(Engine.Device, new Texture2DDescription
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                Usage = ResourceUsage.Immutable,
                Width = m_width,
                Height = m_height,
                ArraySize = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }, rect);

            Disposer.SafeDispose(ref stream);

            return res;
        }

        /// <summary>
        /// Creates a grayscale texture map for the current content of the noise map.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture()
        {
            return this.GetTexture(Gradient.Grayscale);
        }

        /// <summary>
        /// Creates a texture map for the current content of the noise map.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="gradient">The gradient to color the texture map with.</param>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture(Gradient gradient)
        {
            return this.GetTexture(ref gradient);
        }

        /// <summary>
        /// Creates a texture map for the current content of the noise map.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="gradient">The gradient to color the texture map with.</param>
        /// <returns>The created texture map.</returns>
        public Texture2D GetTexture(ref Gradient gradient)
        {
            int[] data = new int[this.m_width * this.m_height];
            Color4 color;

            Minimum = 1;
            Maximum = -1;

            int id = 0;
            for (int y = 0; y < this.m_height; y++)
            {
                for (int x = 0; x < this.m_width; x++, id++)
                {
                    float d = 0.0f;

                    if (!float.IsNaN(this.m_borderValue) && (x == 0 || x == this.m_width - 1 || y == 0 || y == this.m_height - 1))
                    {
                        d = this.m_borderValue;
                    }
                    else
                    {
                        d = this.m_data[x, y];
                    }

                    color = gradient[d];
                    data[id] = color.ToRgba();

                    Minimum = Math.Min(Minimum, color.Red);
                    Maximum = Math.Max(Maximum, color.Red);
                }
            }

            DataStream stream = new DataStream(data.Length * 4, true, true);
            stream.WriteRange(data);

            DataRectangle rect = new DataRectangle(stream.DataPointer, m_width * 4);

            Texture2D res = new Texture2D(Engine.Device, new Texture2DDescription
            {
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                Usage = ResourceUsage.Immutable,
                Width = m_width,
                Height = m_height,
                ArraySize = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }, rect);

            Disposer.SafeDispose(ref stream);

            return res;
        }

        /// <summary>
        /// Creates a texture map for the current content of the noise map.
        /// </summary>
        /// <param name="device">The graphics device to use.</param>
        /// <param name="gradient">The gradient to color the texture map with.</param>
        /// <returns>The created texture map.</returns>
        public Texture Get16BitTexture()
        {
            Half[] data = new Half[this.m_width * this.m_height];

            Minimum = 1;
            Maximum = -1;

            int id = 0;
            for (int y = 0; y < this.m_height; y++)
            {
                for (int x = 0; x < this.m_width; x++, id++)
                {
                    float d = this.m_data[x, y];

                    data[id] = Gradient.Grayscale[d - .5f].Red;

                    Minimum = Math.Min(Minimum, d - 1);
                    Maximum = Math.Max(Maximum, d - 1);
                }
            }

            DataStream stream = new DataStream(data.Length * 2, true, true);
            stream.WriteRange(data);

            int pitch = m_width * System.Runtime.InteropServices.Marshal.SizeOf(typeof(Half));
            DataRectangle rect = new DataRectangle(stream.DataPointer, pitch);

            Texture2D res = new Texture2D(Engine.Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R16_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Height = m_width,
                Width = m_height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            }, rect);

            Texture result = new Texture
            {
                Resource = res,
                View = new ShaderResourceView(Engine.Device, res)
            };

            // Texture2D.ToFile(Engine.Device.ImmediateContext, res, ImageFileFormat.Dds, "G:\\test16bit.dds");

            Disposer.SafeDispose(ref stream);

            return result;
        }

        public Texture Get32BitTexture()
        {
            float[] data = new float[this.m_width * this.m_height];

            Minimum = 1;
            Maximum = -1;

            int id = 0;
            for (int y = 0; y < this.m_height; y++)
            {
                for (int x = 0; x < this.m_width; x++, id++)
                {
                    float d = this.m_data[x, y];

                    data[id] = Gradient.Grayscale[d - .5f].Red;

                    Minimum = Math.Min(Minimum, data[id]);
                    Maximum = Math.Max(Maximum, data[id]);
                }
            }

            DataStream stream = new DataStream(data.Length * 4, true, true);
            stream.WriteRange(data);

            int pitch = m_width * System.Runtime.InteropServices.Marshal.SizeOf(typeof(float));
            DataRectangle rect = new DataRectangle(stream.DataPointer, pitch);

            Texture2D res = new Texture2D(Engine.Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R32_Float,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Height = m_width,
                Width = m_height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            }, rect);

            Texture result = new Texture
            {
                Resource = res,
                View = new ShaderResourceView(Engine.Device, res)
            };

            // Texture2D.ToFile(Engine.Device.ImmediateContext, res, ImageFileFormat.Dds, "G:\\test16bit.dds");

            Disposer.SafeDispose(ref stream);

            return result;
        }

        #endregion Methods

        #region IDisposable Members

        [System.Xml.Serialization.XmlIgnore]
#if !XBOX360 && !ZUNE
        [NonSerialized]
#endif
        private bool m_disposed = false;

        /// <summary>
        /// Gets a value whether the object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return this.m_disposed; }
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        public void Dispose()
        {
            if (!this.m_disposed) { this.m_disposed = this.Disposing(); }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Immediately releases the unmanaged resources used by this object.
        /// </summary>
        /// <returns>True if the object is completely disposed.</returns>
        protected virtual bool Disposing()
        {
            if (this.m_data != null) { this.m_data = null; }
            this.m_width = 0;
            this.m_height = 0;
            return true;
        }

        #endregion IDisposable Members
    }
}