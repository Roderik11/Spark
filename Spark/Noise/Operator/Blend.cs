namespace Spark.Noise.Operator
{
    /// <summary>
    /// Provides a noise module that outputs a weighted blend of the output values from
    /// two source modules given the output value supplied by a control module. [OPERATOR]
    /// </summary>
    public class Blend : ModuleBase
    {

        /// <summary>
        /// Initializes a new instance of Blend.
        /// </summary>
        public Blend()
            : base(3)
        {
        }

        /// <summary>
        /// Initializes a new instance of Blend.
        /// </summary>
        /// <param name="lhs">The left hand input module.</param>
        /// <param name="rhs">The right hand input module.</param>
        /// <param name="controller">The controller of the operator.</param>
        public Blend(ModuleBase lhs, ModuleBase rhs, ModuleBase controller)
            : base(3)
        {
            this.m_modules[0] = lhs;
            this.m_modules[1] = rhs;
            this.m_modules[2] = controller;
        }

        public ModuleBase Input1
        {
            get { return this[0]; }
            set { this[0] = value; }
        }

        public ModuleBase Input2
        {
            get { return this[1]; }
            set { this[1] = value; }
        }

        /// <summary>
        /// Gets or sets the controlling module.
        /// </summary>
        public ModuleBase Controller
        {
            get
            {
                return this.m_modules[2];
            }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                this.m_modules[2] = value;
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
            System.Diagnostics.Debug.Assert(this.m_modules[0] != null);
            System.Diagnostics.Debug.Assert(this.m_modules[1] != null);
            System.Diagnostics.Debug.Assert(this.m_modules[2] != null);
            double a = this.m_modules[0].GetValue(x, y, z);
            double b = this.m_modules[1].GetValue(x, y, z);
            double c = (this.m_modules[2].GetValue(x, y, z) + 1.0) / 2.0;
            return NoiseUtils.InterpolateLinear(a, b, c);
        }

    }
}