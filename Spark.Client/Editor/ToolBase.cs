using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;

namespace Spark.Client
{

    public enum MoveAxis
    {
        Z = 0,
        X = 1,
        Y = 2,
        Free = 3,
        XZ = 4,
        XY = 5,
        ZY = 6,
        SZ = 7,
        SX = 8,
        SY = 9,
    }

    public enum RotateAxis
    {
        Free = 0,
        X = 1,
        Y = 2,
        Z = 3,
    }

    public enum TransformAxis
    {
        None, CameraUp, CameraForward, CameraRight, LocalUp, LocalForward, LocalRight, WorldUp, WorldForward, WorldRight
    }

    public class AxisData
    {
        public TransformAxis Axis1;
        public TransformAxis Axis2;
    }

    public enum TransformMode
    {
        Translate,
        Rotate,
        Scale
    }

    public abstract class ToolBase
    {
        public bool MouseCaptured { get; protected set; }
        public bool IsClicked { get; protected set; }

        public abstract void Initialize();

        public abstract void Update(Camera camera);

        public virtual void Render() { }

        public virtual void Disable() { }
    }

}
