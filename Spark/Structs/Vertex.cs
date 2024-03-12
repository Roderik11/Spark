using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Spark
{
    public struct UniversalVertex
    {
        public static InputElement[] InputElements = new InputElement[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0, 1, InputClassification.PerVertexData, 0),
            new InputElement("TANGENT", 0, Format.R32G32B32_Float, 0, 2, InputClassification.PerVertexData, 0),
            new InputElement("BINORMAL", 0, Format.R32G32B32_Float, 0, 3, InputClassification.PerVertexData, 0),

            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 0, 4, InputClassification.PerVertexData, 0),

            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0, 5, InputClassification.PerVertexData, 0),
            new InputElement("TEXCOORD", 1, Format.R32G32_Float, 0, 6, InputClassification.PerVertexData, 0),
            new InputElement("TEXCOORD", 2, Format.R32G32_Float, 0, 7, InputClassification.PerVertexData, 0),

            new InputElement("BONEIDS", 0, Format.R32G32B32A32_UInt, 0, 8, InputClassification.PerVertexData, 0),
            new InputElement("BONEWEIGHTS", 0, Format.R32G32B32A32_Float, 16, 8, InputClassification.PerVertexData, 0),

            new InputElement("TRANSFORM", 0, Format.R32G32B32A32_Float, 0, 9, InputClassification.PerInstanceData, 1),
            new InputElement("TRANSFORM", 1, Format.R32G32B32A32_Float, 16, 9, InputClassification.PerInstanceData, 1),
            new InputElement("TRANSFORM", 2, Format.R32G32B32A32_Float, 32, 9, InputClassification.PerInstanceData, 1),
            new InputElement("TRANSFORM", 3, Format.R32G32B32A32_Float, 48, 9, InputClassification.PerInstanceData, 1),
        };
    }

    public struct VertexColorUV
    {
        public Vector3 Position;
        public Vector4 Color;
        public Vector2 Uv;

        public static InputElement[] InputElements = new InputElement[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, InputClassification.PerVertexData, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 28, 0, InputClassification.PerVertexData, 0),
        };
    }

    public struct VertexColor
    {
        public Vector3 Position;
        public Vector4 Color;

        public static InputElement[] InputElements = new InputElement[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, InputClassification.PerVertexData, 0),
        };
    }
}