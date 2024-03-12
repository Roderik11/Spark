using System;
using System.ComponentModel;

namespace Spark.Noise
{
    /// <summary>
    /// Provides a two-dimensional noise map.
    /// </summary>
    public class Noise3D : IDisposable
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
        private int m_depth = 0;
        private float m_borderValue = float.NaN;
        private float[,,] m_data = null;
        private ModuleBase m_generator = null;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of Noise2D.
        /// </summary>
        public Noise3D()
            : this(128, 128, 128, null)
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
        public Noise3D(int size)
            : this(size, size, size, null)
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
        public Noise3D(int size, ModuleBase generator)
            : this(size, size, size, generator)
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
        public Noise3D(int width, int height, int depth)
            : this(width, height, depth, null)
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
        public Noise3D(int width, int height, int depth, ModuleBase generator)
        {
            this.m_generator = generator;
            this.m_width = width;
            this.m_height = height;
            this.m_depth = depth;
            this.m_data = new float[width, height, depth];

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
                    m_data = new float[m_width, m_height, m_depth];
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
                    m_data = new float[m_width, m_height, m_depth];
                }
            }
        }

        /// <summary>
        /// Gets the depth of the noise map.
        /// </summary>
        public int Depth
        {
            get
            {
                return this.m_depth;
            }
            set
            {
                if (value != m_depth)
                {
                    m_depth = value;
                    m_data = new float[m_width, m_height, m_depth];
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
                    for (int z = 0; y < this.m_height; y++)
                    {
                        this.m_data[x, y, z] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Generates a planar projection of a point in the noise map.
        /// </summary>
        /// <param name="x">The position on the x-axis.</param>
        /// <param name="y">The position on the y-axis.</param>
        /// <param name="z">The position on the z-axis.</param>
        /// <returns>The corresponding noise map value.</returns>
        private double GeneratePlanar(double x, double y, double z)
        {
            return this.m_generator.GetValue(x, y, z);
        }

        ///// <summary>
        ///// Creates a grayscale texture map for the current content of the noise map.
        ///// </summary>
        ///// <param name="device">The graphics device to use.</param>
        ///// <returns>The created texture map.</returns>
        //public Texture2D GetTexture()
        //{
        //    return this.GetTexture(Gradient.Grayscale);
        //}

        ///// <summary>
        ///// Creates a texture map for the current content of the noise map.
        ///// </summary>
        ///// <param name="device">The graphics device to use.</param>
        ///// <param name="gradient">The gradient to color the texture map with.</param>
        ///// <returns>The created texture map.</returns>
        //public Texture2D GetTexture(Gradient gradient)
        //{
        //    return this.GetTexture(ref gradient);
        //}

        ///// <summary>
        ///// Creates a texture map for the current content of the noise map.
        ///// </summary>
        ///// <param name="device">The graphics device to use.</param>
        ///// <param name="gradient">The gradient to color the texture map with.</param>
        ///// <returns>The created texture map.</returns>
        //public Texture2D GetTexture(ref Gradient gradient)
        //{
        //    int[] data = new int[this.m_width * this.m_height];

        //    int id = 0;
        //    for (int y = 0; y < this.m_height; y++)
        //    {
        //        for (int x = 0; x < this.m_width; x++, id++)
        //        {
        //            float d = 0.0f;

        //            if (!float.IsNaN(this.m_borderValue) && (x == 0 || x == this.m_width - 1 || y == 0 || y == this.m_height - 1))
        //            {
        //                d = this.m_borderValue;
        //            }
        //            else
        //            {
        //                d = this.m_data[x, y];
        //            }

        //            data[id] = gradient[d].ToArgb();
        //        }
        //    }

        //    DataStream stream = new DataStream(data.Length * 4, true, true);
        //    stream.WriteRange(data);

        //    DataRectangle rect = new DataRectangle(m_width * 4, stream);

        //    Texture2D res = new Texture2D(Engine.Device, new Texture2DDescription
        //    {
        //        BindFlags = BindFlags.ShaderResource,
        //        CpuAccessFlags = CpuAccessFlags.None,
        //        Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
        //        OptionFlags = ResourceOptionFlags.None,
        //        MipLevels = 1,
        //        Usage = ResourceUsage.Immutable,
        //        Width = m_width,
        //        Height = m_height,
        //        ArraySize = 1,
        //        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
        //    }, rect);

        //    stream.Release();
        //    stream = null;
        //    rect = null;

        //    return res;
        //}

        //public Texture2D GetTextureCube(Gradient gradient)
        //{
        //    Texture2D cube = new Texture2D(Engine.Device, new Texture2DDescription
        //    {
        //        BindFlags = BindFlags.ShaderResource,
        //        CpuAccessFlags = CpuAccessFlags.Write,
        //        Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
        //        OptionFlags = ResourceOptionFlags.None,
        //        MipLevels = 1,
        //        Usage = ResourceUsage.Dynamic,
        //        Width = m_width,
        //        Height = m_height,
        //        ArraySize = 1,
        //        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
        //    });

        //    int[] data = new int[this.m_width * this.m_height];

        //    int id = 0;
        //    for (int y = 0; y < this.m_height; y++)
        //    {
        //        for (int x = 0; x < this.m_width; x++, id++)
        //        {
        //            float d = 0.0f;

        //            if (!float.IsNaN(this.m_borderValue) && (x == 0 || x == this.m_width - 1 || y == 0 || y == this.m_height - 1))
        //            {
        //                d = this.m_borderValue;
        //            }
        //            else
        //            {
        //                d = this.m_data[x, y];
        //            }

        //            data[id] = gradient[d].ToArgb();
        //        }
        //    }

        //    //DataStream stream = new DataStream(data.Length * 4, true, true);
        //    //stream.WriteRange(data);

        //    DataBox box = Engine.Device.ImmediateContext.MapSubresource(cube, 0, (m_width * m_height) * 4, MapMode.WriteDiscard, MapFlags.None);
        //    box.Data.WriteRange(data);
        //    Engine.Device.ImmediateContext.UnmapSubresource(cube, 0);

        //    //DataStream stream = new DataStream(4 * 4, true, true);
        //    //DataBox box = new DataBox(4, 4, stream);

        //    //Engine.Device.ImmediateContext.UpdateSubresource(box, cube, 0);

        //    Resource.ToFile(Engine.Device.ImmediateContext, cube, ImageFileFormat.Dds, Engine.ResourceDirectory + "testcube.dds");

        //    return cube;
        //}

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