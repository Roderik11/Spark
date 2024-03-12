using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Spark
{
    public static class Geometry
    {
        public static Buffer CreateVertexBuffer<T>(T[] array) where T : struct
        {
            if (array.Length == 0) return null;
            int stride = Utilities.SizeOf<T>();
            var stream = new DataStream(stride * array.Length, false, true);
            stream.WriteRange(array);
            stream.Position = 0;

            var result = new Buffer(Engine.Device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)stream.Length,
                Usage = ResourceUsage.Default,
                StructureByteStride = 0
            });

            stream.Dispose();

            return result;
        }

        public static Buffer CreateIndexBuffer(int[] array)
        {
            if (array.Length == 0) return null;
            var stream = new DataStream(sizeof(uint) * array.Length, false, true);
            stream.WriteRange(array);
            stream.Position = 0;

            var result = new Buffer(Engine.Device, stream, new BufferDescription()
            {
                BindFlags = BindFlags.IndexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = (int)stream.Length,
                Usage = ResourceUsage.Default,
                StructureByteStride = 0
            });

            stream.Dispose();

            return result;
        }

        public static void TangentBinormal(int[] indices, Vector3[] vertices, Vector3[] tangents, Vector3[] binormals, Vector2[] uvs)
        {
            int faceCount, i, index;
            Vector3 vertex1, vertex2, vertex3;
            Vector2 uv1, uv2, uv3;
            Vector3 tangent = Vector3.Zero;
            Vector3 binormal = Vector3.Zero;
            Vector3 normal = Vector3.Zero;

            // Calculate the number of faces in the model.
            faceCount = indices.Length / 3;

            // Initialize the index to the model data.
            index = 0;

            // Go through all the faces and calculate the the tangent, binormal, and normal vectors.
            for (i = 0; i < faceCount; i++)
            {
                // Get the three vertices for this face from the model.
                vertex1 = vertices[indices[index]];
                uv1 = uvs[indices[index]];
                index++;

                vertex2 = vertices[indices[index]];
                uv2 = uvs[indices[index]];
                index++;

                vertex3 = vertices[indices[index]];
                uv3 = uvs[indices[index]];
                index++;

                // Calculate the tangent and binormal of that face.
                ComputeTangentBasis(vertex1, vertex2, vertex3, uv1, uv2, uv3, ref normal, ref tangent, ref binormal);

                // Store the normal, tangent, and binormal for this face back in the model structure.
                vertices[indices[index - 1]] = vertex3;
                vertices[indices[index - 2]] = vertex2;
                vertices[indices[index - 3]] = vertex1;

                tangents[indices[index - 1]] = tangent;
                tangents[indices[index - 2]] = tangent;
                tangents[indices[index - 3]] = tangent;

                binormals[indices[index - 1]] = binormal;
                binormals[indices[index - 2]] = binormal;
                binormals[indices[index - 3]] = binormal;
            }

            return;
        }

        public static void ComputeTangentBasis(Vector3 P0, Vector3 P1, Vector3 P2, Vector2 UV0, Vector2 UV1, Vector2 UV2, ref Vector3 normal, ref Vector3 tangent, ref Vector3 binormal)
        {
            Vector3 e0 = P1 - P0;
            Vector3 e1 = P2 - P0;
            normal = Vector3.Cross(e0, e1);

            //using Eric Lengyel's approach with a few modifications
            //from Mathematics for 3D Game Programmming and Computer Graphics
            // want to be able to trasform a vector in Object Space to Tangent Space
            // such that the x-axis cooresponds to the 's' direction and the
            // y-axis corresponds to the 't' direction, and the z-axis corresponds
            // to <0,0,1>, straight up out of the texture map

            //let P = v1 - v0
            Vector3 P = P1 - P0;

            //let Q = v2 - v0
            Vector3 Q = P2 - P0;

            float s1 = UV1.X - UV0.X;
            float t1 = UV1.Y - UV0.Y;
            float s2 = UV2.X - UV0.X;
            float t2 = UV2.Y - UV0.Y;

            //we need to solve the equation
            // P = s1*T + t1*B
            // Q = s2*T + t2*B

            // for T and B
            //this is a linear system with six unknowns and six equatinos, for TxTyTz BxByBz
            //[px,py,pz] = [s1,t1] * [Tx,Ty,Tz]
            // qx,qy,qz     s2,t2     Bx,By,Bz

            //multiplying both sides by the inverse of the s,t matrix gives
            //[Tx,Ty,Tz] = 1/(s1t2-s2t1) *  [t2,-t1] * [px,py,pz]
            // Bx,By,Bz                      -s2,s1	    qx,qy,qz

            //solve this for the unormalized T and B to get from tangent to object space
            float tmp;

            if (Math.Abs(s1 * t2 - s2 * t1) <= 0.0001f)
                tmp = 1.0f;
            else
                tmp = 1.0f / (s1 * t2 - s2 * t1);

            tangent.X = (t2 * P.X - t1 * Q.X);
            tangent.Y = (t2 * P.Y - t1 * Q.Y);
            tangent.Z = (t2 * P.Z - t1 * Q.Z);
            tangent = tangent * tmp;

            binormal.X = (s1 * Q.X - s2 * P.X);
            binormal.Y = (s1 * Q.Y - s2 * P.Y);
            binormal.Z = (s1 * Q.Z - s2 * P.Z);
            binormal = binormal * tmp;

            normal.Normalize();
            tangent.Normalize();
            binormal.Normalize();
        }
    }
}