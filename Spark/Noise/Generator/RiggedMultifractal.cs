using System;

namespace Spark.Noise.Generator
{
    /// <summary>
    /// Provides a noise module that outputs 3-dimensional ridged-multifractal noise. [GENERATOR]
    /// </summary>
    public class RiggedMultifractal : ModuleBase
    {

        private double m_frequency = 1.0;
        private double m_lacunarity = 2.0;
        private QualityMode m_quality = QualityMode.Medium;
        private int m_octaveCount = 6;
        private int m_seed = 0;
        private double[] m_weights = new double[NoiseUtils.OctavesMaximum];

        /// <summary>
        /// Initializes a new instance of RiggedMultifractal.
        /// </summary>
        public RiggedMultifractal()
            : base(0)
        {
            this.UpdateWeights();
        }

        /// <summary>
        /// Initializes a new instance of RiggedMultifractal.
        /// </summary>
        /// <param name="frequency">The frequency of the first octave.</param>
        /// <param name="lacunarity">The lacunarity of the ridged-multifractal noise.</param>
        /// <param name="octaves">The number of octaves of the ridged-multifractal noise.</param>
        /// <param name="seed">The seed of the ridged-multifractal noise.</param>
        /// <param name="quality">The quality of the ridged-multifractal noise.</param>
        public RiggedMultifractal(double frequency, double lacunarity, int octaves, int seed, QualityMode quality)
            : base(0)
        {
            this.Frequency = frequency;
            this.Lacunarity = lacunarity;
            this.OctaveCount = octaves;
            this.Seed = seed;
            this.Quality = quality;
        }

        /// <summary>
        /// Gets or sets the frequency of the first octave.
        /// </summary>
        public double Frequency
        {
            get { return this.m_frequency; }
            set { this.m_frequency = value; }
        }

        /// <summary>
        /// Gets or sets the lacunarity of the ridged-multifractal noise.
        /// </summary>
        public double Lacunarity
        {
            get
            {
                return this.m_lacunarity;
            }
            set
            {
                this.m_lacunarity = value;
                this.UpdateWeights();
            }
        }

        /// <summary>
        /// Gets or sets the quality of the ridged-multifractal noise.
        /// </summary>
        public QualityMode Quality
        {
            get { return this.m_quality; }
            set { this.m_quality = value; }
        }

        /// <summary>
        /// Gets or sets the number of octaves of the ridged-multifractal noise.
        /// </summary>
        public int OctaveCount
        {
            get { return this.m_octaveCount; }
            set { this.m_octaveCount = (int)MathHelper.Clamp(value, 1, NoiseUtils.OctavesMaximum); }
        }

        /// <summary>
        /// Gets or sets the seed of the ridged-multifractal noise.
        /// </summary>
        public int Seed
        {
            get { return this.m_seed; }
            set { this.m_seed = value; }
        }

        /// <summary>
        /// Updates the weights of the ridged-multifractal noise.
        /// </summary>
        private void UpdateWeights()
        {
            double f = 1.0;
            for (int i = 0; i < NoiseUtils.OctavesMaximum; i++)
            {
                this.m_weights[i] = Math.Pow(f, -1.0);
                f *= this.m_lacunarity;
            }
        }

        /// <summary>
        /// Returns the output value for the given input coordinates.
        /// </summary>
        /// <param name="x">The input coordinate on the x-axis.</param>
        /// <param name="y">The input coordinate on the y-axis.</param>
        /// <param name="z">The input coordinate on the z-axis.</param>
        /// <returns>The resulting output value.</returns>
        public override double GetValue(double x, double y, double z)
        {
            x *= this.m_frequency;
            y *= this.m_frequency;
            z *= this.m_frequency;
            double signal = 0.0;
            double value = 0.0;
            double weight = 1.0;
            double offset = 1.0;
            double gain = 2.0;
            for (int i = 0; i < this.m_octaveCount; i++)
            {
                double nx = NoiseUtils.MakeInt32Range(x);
                double ny = NoiseUtils.MakeInt32Range(y);
                double nz = NoiseUtils.MakeInt32Range(z);
                long seed = (this.m_seed + i) & 0x7fffffff;
                signal = NoiseUtils.GradientCoherentNoise3D(nx, ny, nz, seed, this.m_quality);
                signal = Math.Abs(signal);
                signal = offset - signal;
                signal *= signal;
                signal *= weight;
                weight = signal * gain;
                if (weight > 1.0) { weight = 1.0; }
                if (weight < 0.0) { weight = 0.0; }
                value += (signal * this.m_weights[i]);
                x *= this.m_lacunarity;
                y *= this.m_lacunarity;
                z *= this.m_lacunarity;
            }
            return (value * 1.25) - 1.0;
        }

    }
}