namespace Spark.Noise.Operator
{
    /// <summary>
    /// Provides a noise module that outputs the product of the two output values from
    /// two source modules. [OPERATOR]
    /// </summary>
    public class Multiply : ModuleBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of Multiply.
        /// </summary>
        public Multiply()
            : base(2)
        {
        }

        /// <summary>
        /// Initializes a new instance of Multiply.
        /// </summary>
        /// <param name="lhs">The left hand input module.</param>
        /// <param name="rhs">The right hand input module.</param>
        public Multiply(ModuleBase lhs, ModuleBase rhs)
            : base(2)
        {
            this.m_modules[0] = lhs;
            this.m_modules[1] = rhs;
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

        #endregion Constructors

        #region ModuleBase Members

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
            return this.m_modules[0].GetValue(x, y, z) * this.m_modules[1].GetValue(x, y, z);
        }

        #endregion ModuleBase Members
    }
}