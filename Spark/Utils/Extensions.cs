using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.WIC;

namespace Spark
{
    public static class Utils
    {

        public static void WriteRange<T>(this BinaryWriter writer, T[] values) where T: struct
        {
            if(values == null)
            {
                writer.Write(0);
                return;
            }

            var bytes = ToByteArray(values);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public static T[] ReadRange<T>(this BinaryReader reader) where T : struct
        {
            var size = reader.ReadInt32();
            if (size == 0) return null;
            var bytes = reader.ReadBytes(size);
            return FromByteArray<T>(bytes);
        }

        public static void Write<T>(this BinaryWriter writer, T value) where T : struct
        {
            var bytes = StructToBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        public static T Read<T>(this BinaryReader reader) where T : struct
        {
            var size = reader.ReadInt32();
            var bytes = reader.ReadBytes(size);
            return BytesToStruct<T>(bytes);
        }

        private static byte[] ToByteArray<T>(T[] source) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                byte[] destination = new byte[source.Length * Marshal.SizeOf(typeof(T))];
                Marshal.Copy(pointer, destination, 0, destination.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        private static T[] FromByteArray<T>(byte[] source) where T : struct
        {
            T[] destination = new T[source.Length / Marshal.SizeOf(typeof(T))];
            GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
            try
            {
                IntPtr pointer = handle.AddrOfPinnedObject();
                Marshal.Copy(source, 0, pointer, source.Length);
                return destination;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        public static T BytesToStruct<T>(byte[] data) where T : struct
        {
            GCHandle handle;
            T result;

            handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();

            return result;
        }

        public static byte[] StructToBytes<T>(T value) where T : struct
        {
            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];

            GCHandle handle;
            handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            handle.Free();

            return array;
        }

        public static IEnumerable<FileInfo> GetFilesFiltered(this DirectoryInfo dir, params string[] extensions)
        {
            if (extensions == null)
                throw new ArgumentNullException("extensions");

            var hashset = new HashSet<string>(extensions);
            IEnumerable<FileInfo> files = dir.EnumerateFiles("*.*", SearchOption.AllDirectories);
            return files.Where(f => hashset.Contains(f.Extension, StringComparer.OrdinalIgnoreCase));
        }

        // Returns the human-readable file size for an arbitrary, 64-bit file size 
        // The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
        public static string GetBytesReadable(this long i, string format = "0.##")
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString(format) + suffix;
        }

        public static void For<T>(this IList<T> list, Action<int> action, bool parallel = false)
        {
            if (parallel)
            {
                Parallel.For(0, list.Count, action);
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                    action(i);
            }
        }
            
        public static void For<T>(this IList<T> list, Action<T> action, bool parallel = false)
        {
            if (parallel)
            {
                Parallel.For(0, list.Count, (i) => action(list[i]));
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                    action(list[i]);
            }
        }

        public static void DrawImmediate(this Mesh mesh, MeshPart part, Material material, DeviceContext context)
        {
            Profiler.Start("SetBuffers");
            var assembler = context.InputAssembler;
            assembler.PrimitiveTopology = mesh.Topology;
            assembler.SetVertexBuffers(0, mesh.Bindings);
            assembler.SetIndexBuffer(mesh.IndexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
            assembler.InputLayout = material.GetInputLayout(mesh.InputElements);
            Profiler.Stop();

            if (mesh.IndexBuffer != null)
            {
                Profiler.Start("DrawIndexed");
                context.DrawIndexed(part.NumIndices, part.BaseIndex, part.BaseVertex);
                Profiler.Stop();
            }
            else
            {
                Profiler.Start("DrawNonIndexed");
                context.Draw(part.NumIndices, part.BaseVertex);
                Profiler.Stop();
            }
        }

        public static void Render(this Mesh mesh, Material material, MaterialBlock block = null)
        {
            int count = mesh.MeshParts.Count;
            MeshPart part;

            for (int meshPart = 0; meshPart < count; meshPart++)
            {
                part = mesh.MeshParts[meshPart];
                if (!part.Enabled) continue;

                CommandBuffer.Enqueue(mesh, meshPart, material, block);
            }
        }

        public static void Render(this Mesh mesh, int partIndex, Material material, MaterialBlock block = null)
        {
            CommandBuffer.Enqueue(mesh, partIndex, material, block);
        }

        public static void Render(this Mesh mesh, List<Material> materials, MaterialBlock block = null, int startIndex = 0)
        {
            var materialCount = materials.Count;
            var meshPartCount = mesh.MeshParts.Count - 1;

            for (int i = 0; i < materialCount; i++)
            {
                int index = Math.Min(startIndex + i, meshPartCount);
                var meshPart = mesh.MeshParts[index];
                if (!meshPart.Enabled) continue;
                var material = materials[i];
                if(material == null)continue;

                CommandBuffer.Enqueue(mesh, index, material, block);
            }
        }

        private static string Encode(string guidText)
        {
            Guid guid = new Guid(guidText);
            return GuidToString(guid);
        }

        public static string GuidToString(Guid guid)
        {
            string base64 = Convert.ToBase64String(guid.ToByteArray());
            base64 = base64.Replace("/", "_");
            base64 = base64.Replace("+", "-");
            return base64.Substring(0, 22);
        }

        public static Guid StringToGuid(string encoded)
        {
            encoded = encoded.Replace("_", "/");
            encoded = encoded.Replace("-", "+");
            byte[] buffer = Convert.FromBase64String(encoded + "==");
            return new Guid(buffer);
        }

        public static string Vector2ToString(Vector2 vector)
        {
            return vector.X + ";" + vector.Y;
        }

        public static string Vector3ToString(Vector3 vector)
        {
            return vector.X + ";" + vector.Y + ";" + vector.Z;
        }

        public static string Vector4ToString(Vector4 vector)
        {
            return vector.X + ";" + vector.Y + ";" + vector.Z + ";" + vector.W;
        }

        public static string QuaternionToString(Quaternion quaternion)
        {
            return quaternion.X + ";" + quaternion.Y + ";" + quaternion.Z + ";" + quaternion.W;
        }

        public static Vector2 StringToVector2(string str)
        {
            string[] arr = str.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            float[] components = new float[arr.Length];

            for (int i = 0; i < components.Length; i++)
            {
                try
                {
                    components[i] = (float)System.Convert.ToSingle(arr[i]);
                }
                catch
                {
                    components[i] = 0;
                }
            }

            if (components.Length < 2)
                return new Vector2(0, 0);

            return new Vector2(components[0], components[1]);
        }

        public static Vector3 StringToVector3(string str)
        {
            string[] arr = str.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            float[] components = new float[arr.Length];

            for (int i = 0; i < components.Length; i++)
            {
                try
                {
                    components[i] = (float)System.Convert.ToSingle(arr[i]);
                }
                catch
                {
                    components[i] = 0;
                }
            }

            if (components.Length < 3)
                return new Vector3(0, 0, 0);

            return new Vector3(components[0], components[1], components[2]);
        }

        public static Vector4 StringToVector4(string str)
        {
            string[] arr = str.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            float[] components = new float[arr.Length];

            for (int i = 0; i < components.Length; i++)
            {
                try
                {
                    components[i] = (float)System.Convert.ToSingle(arr[i]);
                }
                catch
                {
                    components[i] = 0;
                }
            }

            if (components.Length < 4)
                return new Vector4(0, 0, 0, 0);

            return new Vector4(components[0], components[1], components[2], components[3]);
        }

        public static Quaternion StringToQuaternion(string str)
        {
            string[] arr = str.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            float[] components = new float[arr.Length];

            for (int i = 0; i < components.Length; i++)
            {
                try
                {
                    components[i] = (float)System.Convert.ToSingle(arr[i]);
                }
                catch
                {
                    components[i] = 0;
                }
            }

            if (components.Length < 4)
                return new Quaternion(0, 0, 0, 0);

            return new Quaternion(components[0], components[1], components[2], components[3]);
        }

    }

    public static class MathExtensions
    {
        public const float Singularity = 0.499f;
        public const float Pi = 3.14159265359f;

        public static float ClockwiseAngle(Vector3 a, Vector3 b, Vector3 n)
        {
            var dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            var det = a.X * b.Y * n.Z + b.X * n.Y * a.Z + n.X * a.Y * b.Z - a.Z * b.Y * n.X - b.Z * n.Y * a.X - n.Z * a.Y * b.X;
            var angle = Math.Atan2(det, dot);
            return (float)angle;
        }

        public static float AngleBetween(Vector3 u, Vector3 v, bool returndegrees = true)
        {
            double toppart = 0;
            for (int d = 0; d < 3; d++) toppart += u[d] * v[d];

            double u2 = 0; //u squared
            double v2 = 0; //v squared
            for (int d = 0; d < 3; d++)
            {
                u2 += u[d] * u[d];
                v2 += v[d] * v[d];
            }

            double bottompart = 0;
            bottompart = Math.Sqrt(u2 * v2);


            double rtnval = Math.Acos(toppart / bottompart);
            if (returndegrees) rtnval *= 360.0 / (2 * Math.PI);
            return (float)rtnval;
        }

        // Derived from: http://stackoverflow.com/questions/1031005/is-there-an-algorithm-for-converting-quaternion-rotations-to-euler-angle-rotatio/2070899#2070899
        // Returns Euler angles applied in ZYX order to match Matrix.CreateRotationZYX.
        public static Vector3 ToEuler(this Quaternion q)
        {
            float ww = q.W * q.W;
            float xx = q.X * q.X;
            float yy = q.Y * q.Y;
            float zz = q.Z * q.Z;
            float lengthSqd = xx + yy + zz + ww;
            float singularityTest = q.Y * q.W - q.X * q.Z;
            float singularityValue = Singularity * lengthSqd;
            return singularityTest > singularityValue
                ? new Vector3(-2 * MathExtensions.Atan2(q.Z, q.W), 90.0f, 0.0f)
                : singularityTest < -singularityValue
                    ? new Vector3(2 * MathExtensions.Atan2(q.Z, q.W), -90.0f, 0.0f)
                    : new Vector3(MathExtensions.Atan2(2.0f * (q.Y * q.Z + q.X * q.W), 1.0f - 2.0f * (xx + yy)),
                        MathExtensions.Asin(2.0f * singularityTest / lengthSqd),
                        MathExtensions.Atan2(2.0f * (q.X * q.Y + q.Z * q.W), 1.0f - 2.0f * (yy + zz)));
        }

        public static Quaternion QuaternionFromEuler(Vector3 eulerAngles)
        {
            var matrix = CreateRotationAboutZThenYThenX(eulerAngles.X, eulerAngles.Y, eulerAngles.Z);
            return Quaternion.RotationMatrix(matrix);
        }

        public static Matrix CreateRotationAboutZThenYThenX(float x, float y, float z)
        {
            float cx = MathExtensions.Cos(x), sx = MathExtensions.Sin(x);
            float cy = MathExtensions.Cos(y), sy = MathExtensions.Sin(y);
            float cz = MathExtensions.Cos(z), sz = MathExtensions.Sin(z);
            return new Matrix(
                cy * cz, cy * sz, -sy, 0.0f,
                sx * sy * cz + cx * -sz, sx * sy * sz + cx * cz, sx * cy, 0.0f,
                cx * sy * cz + sx * sz, cx * sy * sz + -sx * cz, cx * cy, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static float Sin(float degrees)
        {
            return (float)Math.Sin(degrees * Pi / 180.0f);
        }

        public static float Cos(float degrees)
        {
            return (float)Math.Cos(degrees * Pi / 180.0f);
        }

        public static float Tan(float degrees)
        {
            return (float)Math.Tan(degrees * Pi / 180.0f);
        }

        public static float Asin(float value)
        {
            return (float)Math.Asin(value) * 180 / Pi;
        }

        public static float Acos(float value)
        {
            return (float)Math.Acos(value) * 180 / Pi;
        }

        public static float Atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x) * 180 / Pi;
        }
    }

    public static class Extensions
    {
        public static bool Intersects(this Mesh mesh, Ray ray, Matrix matrix, out RaycastResult result)
        {
            var inverse = Matrix.Invert(matrix);

            ray.Position = Vector3.TransformCoordinate(ray.Position, inverse);
            ray.Direction = Vector3.TransformNormal(ray.Direction, inverse);
            ray.Direction = Vector3.Normalize(ray.Direction);

            result = new RaycastResult();

            if (mesh.Vertices == null || mesh.Indices == null)
                return false;

            var verts = mesh.Vertices;
            var inds = mesh.Indices;

            try
            {
                for (int i = 0; i < inds.Length; i += 3)
                {
                    var v1 = verts[inds[i]];
                    var v2 = verts[inds[i + 1]];
                    var v3 = verts[inds[i + 2]];
                    var intersects = ray.Intersects(ref v1, ref v2, ref v3, out Vector3 intersectPoint);

                    if (intersects)
                    {
                        result.hitPoint = Vector3.TransformCoordinate(intersectPoint, matrix);

                        for (int m = 0; m < mesh.MeshParts.Count; m++)
                        {
                            if (mesh.MeshParts[m].BaseIndex > i)
                                break;

                            result.meshPart = m;
                        }

                        return true;
                    }
                }
            }
            catch(Exception ex) 
            {
                Debug.Log(ex.Message);
            }
            return false;
        }

       

        public static T GetAttribute<T>(this Field field) where T : Attribute
        {
            object[] arr = field.Member.GetCustomAttributes(typeof(T), false);

            if (arr.Length > 0)
                return arr[0] as T;

            return null;
        }

        public static string GetCategory(this Field property)
        {
            CategoryAttribute a = property.GetAttribute<CategoryAttribute>();
            if (a != null) return a.Category;

            return null;
        }

        public static Matrix AsMatrix(this Assimp.Matrix4x4 matrix)
        {
            Matrix result = Matrix.Identity;

            result.M11 = matrix.A1;
            result.M12 = matrix.A2;
            result.M13 = matrix.A3;
            result.M14 = matrix.A4;

            result.M21 = matrix.B1;
            result.M22 = matrix.B2;
            result.M23 = matrix.B3;
            result.M24 = matrix.B4;

            result.M31 = matrix.C1;
            result.M32 = matrix.C2;
            result.M33 = matrix.C3;
            result.M34 = matrix.C4;

            result.M41 = matrix.D1;
            result.M42 = matrix.D2;
            result.M43 = matrix.D3;
            result.M44 = matrix.D4;

            result.Transpose();

            return result;
        }

        public static Vector3 AsVector3(this Assimp.Vector3D vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static Vector2 AsVector2(this Assimp.Vector3D vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static Vector3 AsVector3(this Vector4 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static SharpDX.Direct3D11.ShaderResourceView[] ToResourceArray(this List<Texture> list)
        {
            SharpDX.Direct3D11.ShaderResourceView[] result = new SharpDX.Direct3D11.ShaderResourceView[list.Count];

            for (int i = 0; i < list.Count; i++)
                result[i] = list[i].View;

            return result;
        }

        public static EffectParameter ToEffectProperty(this SharpDX.Direct3D11.EffectVariable var)
        {
            string typename = var.TypeInfo.Description.TypeName;
         
            if (typename == "SamplerState")
                return new SamplerParameter();

            if (typename == "StructuredBuffer")
                return new StructuredBufferParameter();

            if (typename == "float4x4" || typename == "float4x3")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new MatrixArrayParameter();

                return new MatrixParameter();
            }

            if (typename == "float4")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new Vector4ArrayParameter();

                return new Vector4Parameter();
            }

            if (typename == "float3")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new Vector3ArrayParameter();

                return new Vector3Parameter();
            }

            if (typename == "float2")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new Vector2ArrayParameter();

                return new Vector2Parameter();
            }

            if(typename == "Texture2DArray")
                return new TextureParameter();

            if (typename == "Texture2D" || typename == "TextureCube")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new TextureListParameter();

                return new TextureParameter();
            }

            if (typename == "RWTexture2D")
            {
                return new RWTextureParameter();
            }

            if (typename == "float")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new FloatArrayParameter();

                return new FloatParameter();
            }

            if (typename == "int")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new IntArrayParameter();

                return new IntParameter();
            }

            if (typename == "uint")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new UIntArrayParameter();

                return new UIntParameter();
            }

            if (typename == "bool")
            {
                if (var.TypeInfo.Description.Elements > 0)
                    return new BoolArrayParameter();

                return new BoolParameter();
            }

            return null;
        }


        public static Vector3[] GetCorners(this BoundingBox box, Matrix transform)
        {
            Vector3[] points = box.GetCorners();
            Vector3.TransformCoordinate(points, ref transform, points);
            return points;
        }

        public static void GetCorners(this BoundingBox box, Vector3[] points, Matrix transform)
        {
            box.GetCorners(points);
            Vector3.TransformCoordinate(points, ref transform, points);
        }

        public static T GetAttribute<T>(this PropertyInfo property) where T : Attribute
        {
            object[] arr = property.GetCustomAttributes(typeof(T), false);

            if (arr.Length > 0)
                return arr[0] as T;

            return null;
        }

        public static string GetDescription(this PropertyInfo property)
        {
            DescriptionAttribute a = property.GetAttribute<DescriptionAttribute>();
            if (a != null) return a.Description;

            return null;
        }

        public static string GetDisplayName(this PropertyInfo property)
        {
            DisplayNameAttribute a = property.GetAttribute<DisplayNameAttribute>();
            if (a != null) return a.DisplayName;

            return null;
        }

        public static string GetCategory(this PropertyInfo property)
        {
            CategoryAttribute a = property.GetAttribute<CategoryAttribute>();
            if (a != null) return a.Category;

            return null;
        }

        public static float NextFloat(this Random random)
        {
            return (float)random.NextDouble();
        }

        public static float NextFloat(this Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }

    public static class TextureExtensions
    {
        public static float[,] To16BitField(this Texture2D tex, float scale)
        {
            var sourceData = new Texture2D(Engine.Device, new Texture2DDescription
            {
                Format = SharpDX.DXGI.Format.R16_Float,
                MipLevels = tex.Description.MipLevels,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                ArraySize = 1,
                Height = tex.Description.Height,
                Width = tex.Description.Width,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            });

            Engine.Device.ImmediateContext.CopyResource(tex, sourceData);

            int size = tex.Description.Width;
            var buff = new Half[size * size];
            var box = Engine.Device.ImmediateContext.MapSubresource(sourceData, 0, MapMode.Read, MapFlags.None, out DataStream stream);

            if (box.RowPitch == size * 2)
            {
                stream.ReadRange(buff, 0, buff.Length);
            }
            else
            {
                int offset = 0;
                var ptr = box.DataPointer;

                for (int i = 0; i < size; i++)
                {
                    Utilities.Read((IntPtr)ptr, buff, offset, size);
                    ptr += box.RowPitch;
                    offset += size;
                }
            }

            Engine.Device.ImmediateContext.UnmapSubresource(sourceData, 0);
            Disposer.SafeDispose(ref sourceData);
            Disposer.SafeDispose(ref stream);

            int x = 0;
            int z = 0;
            float[,] heights = new float[size, size];

            for (int i = 0; i < heights.Length; i++)
            {
                heights[x, z] = buff[i] * scale;

                x++;
                if (x > (size - 1))
                {
                    x = 0;
                    z++;
                }
            }

            buff = null;
            return heights;
        }

        public static float[,] To16BitField(string texture, float scale)
        {
            var source = Texture.FromFile(Engine.Device, texture);

            var sourceData = new Texture2D(Engine.Device, new Texture2DDescription
            {
                Format = SharpDX.DXGI.Format.R16_Float,
                MipLevels = source.Description.MipLevels,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                ArraySize = 1,
                Height = source.Description.Height,
                Width = source.Description.Width,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            });

            Engine.Device.ImmediateContext.CopyResource(source, sourceData);

            int size = sourceData.Description.Width;
            var buff = new Half[size * size];
            var box = Engine.Device.ImmediateContext.MapSubresource(sourceData, 0, MapMode.Read, MapFlags.None, out DataStream stream);

            if (box.RowPitch == size * 2)
            {
                stream.ReadRange(buff, 0, buff.Length);
            }
            else
            {
                int offset = 0;
                var ptr = box.DataPointer;

                for (int i = 0; i < size; i++)
                {
                    Utilities.Read((IntPtr)ptr, buff, offset, size);
                    ptr += box.RowPitch;
                    offset += size;
                }
            }

            Engine.Device.ImmediateContext.UnmapSubresource(sourceData, 0);
            Disposer.SafeDispose(ref sourceData);
            Disposer.SafeDispose(ref stream);

            int x = 0;
            int z = 0;
            float[,] heights = new float[size, size];

            for (int i = 0; i < heights.Length; i++)
            {
                //x = i % size;
                //z = i / size;

                heights[x, z] = buff[i] * scale;

                x++;
                if (x > size - 1)
                {
                    x = 0;
                    z++;
                }
            }

            buff = null;

            return heights;
        }

        public static Guid PixelFormatFromFormat(SharpDX.DXGI.Format format)
        {
            switch(format)
            { 
                case SharpDX.DXGI.Format.R32G32B32A32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32A32_Float:
                    return PixelFormat.Format128bppRGBAFloat;

                case SharpDX.DXGI.Format.R32G32B32A32_UInt:
                case SharpDX.DXGI.Format.R32G32B32A32_SInt:
                    return PixelFormat.Format128bppRGBAFixedPoint;

                case SharpDX.DXGI.Format.R32G32B32_Typeless:
                case SharpDX.DXGI.Format.R32G32B32_Float:
                    return PixelFormat.Format96bppRGBFloat;

                case SharpDX.DXGI.Format.R32G32B32_UInt:
                case SharpDX.DXGI.Format.R32G32B32_SInt:
                    return PixelFormat.Format96bppRGBFixedPoint;

                case SharpDX.DXGI.Format.R16G16B16A16_Typeless:
                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_UInt:
                case SharpDX.DXGI.Format.R16G16B16A16_SNorm:
                case SharpDX.DXGI.Format.R16G16B16A16_SInt:
                    return PixelFormat.Format64bppRGBA;

                case SharpDX.DXGI.Format.R10G10B10A2_Typeless:
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                case SharpDX.DXGI.Format.R10G10B10A2_UInt:
                    return PixelFormat.Format32bppRGBA1010102;
               
                case SharpDX.DXGI.Format.R8G8B8A8_Typeless:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.R8G8B8A8_UInt:
                case SharpDX.DXGI.Format.R8G8B8A8_SNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_SInt:
                    return PixelFormat.Format32bppRGBA;

                case SharpDX.DXGI.Format.R24G8_Typeless:
                case SharpDX.DXGI.Format.D24_UNorm_S8_UInt:
                case SharpDX.DXGI.Format.R24_UNorm_X8_Typeless:
                    return PixelFormat.Format32bppGrayFloat;

                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm:
                    return PixelFormat.Format32bppBGRA;

                case SharpDX.DXGI.Format.R10G10B10_Xr_Bias_A2_UNorm:
                    return PixelFormat.Format32bppBGR101010;

                case SharpDX.DXGI.Format.B8G8R8A8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb:
                case SharpDX.DXGI.Format.B8G8R8X8_Typeless:
                case SharpDX.DXGI.Format.B8G8R8X8_UNorm_SRgb:
                    return PixelFormat.Format32bppBGRA;


                case SharpDX.DXGI.Format.R16_Typeless:
                case SharpDX.DXGI.Format.R16_Float:
                case SharpDX.DXGI.Format.D16_UNorm:
                case SharpDX.DXGI.Format.R16_UNorm:
                case SharpDX.DXGI.Format.R16_SNorm:
                    return PixelFormat.Format16bppGrayHalf;

                case SharpDX.DXGI.Format.R16_UInt:
                case SharpDX.DXGI.Format.R16_SInt:
                    return PixelFormat.Format16bppGrayFixedPoint;

                case SharpDX.DXGI.Format.B5G6R5_UNorm:
                    return PixelFormat.Format16bppBGR565;

                case SharpDX.DXGI.Format.B5G5R5A1_UNorm:
                    return PixelFormat.Format16bppBGRA5551;

                case SharpDX.DXGI.Format.R8_Typeless:
                case SharpDX.DXGI.Format.R8_UNorm:
                case SharpDX.DXGI.Format.R8_UInt:
                case SharpDX.DXGI.Format.R8_SNorm:
                case SharpDX.DXGI.Format.R8_SInt:
                    return PixelFormat.Format8bppGray;

                case SharpDX.DXGI.Format.A8_UNorm:
                    return PixelFormat.Format8bppAlpha;

                case SharpDX.DXGI.Format.R1_UNorm:
                    return PixelFormat.Format1bppIndexed;

                case SharpDX.DXGI.Format.R16G16_Typeless:
                case SharpDX.DXGI.Format.R16G16_Float:
                case SharpDX.DXGI.Format.R16G16_UNorm:
                case SharpDX.DXGI.Format.R16G16_UInt:
                case SharpDX.DXGI.Format.R16G16_SNorm:
                case SharpDX.DXGI.Format.R16G16_SInt:
                case SharpDX.DXGI.Format.R32_Typeless:
                case SharpDX.DXGI.Format.D32_Float:
                case SharpDX.DXGI.Format.R32_Float:
                case SharpDX.DXGI.Format.R32_UInt:
                case SharpDX.DXGI.Format.R32_SInt:
                case SharpDX.DXGI.Format.X24_Typeless_G8_UInt:
                case SharpDX.DXGI.Format.R9G9B9E5_Sharedexp:
                case SharpDX.DXGI.Format.R8G8_B8G8_UNorm:
                case SharpDX.DXGI.Format.G8R8_G8B8_UNorm:
                case SharpDX.DXGI.Format.R32G32_Typeless:
                case SharpDX.DXGI.Format.R32G32_Float:
                case SharpDX.DXGI.Format.R32G32_UInt:
                case SharpDX.DXGI.Format.R32G32_SInt:
                case SharpDX.DXGI.Format.R32G8X24_Typeless:
                case SharpDX.DXGI.Format.D32_Float_S8X24_UInt:
                case SharpDX.DXGI.Format.R32_Float_X8X24_Typeless:
                case SharpDX.DXGI.Format.X32_Typeless_G8X24_UInt:
                case SharpDX.DXGI.Format.R11G11B10_Float:
                case SharpDX.DXGI.Format.R8G8_Typeless:
                case SharpDX.DXGI.Format.R8G8_UNorm:
                case SharpDX.DXGI.Format.R8G8_UInt:
                case SharpDX.DXGI.Format.R8G8_SNorm:
                case SharpDX.DXGI.Format.R8G8_SInt:
                case SharpDX.DXGI.Format.B4G4R4A4_UNorm:
                    return Guid.Empty;

                default:
                    return Guid.Empty;
                }
        }

        public static Texture RamCopy(this Texture texture)
        {
            var device = Engine.Device;
            var context = Engine.Device.ImmediateContext;
            var dxgiFormat = SharpDX.DXGI.Format.R8G8B8A8_UNorm;

            int width = texture.Description.Width;
            int height = texture.Description.Height;

            var ramTexture = new Texture2D(device, new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 0,
                ArraySize = 1,
                Format = dxgiFormat,
                Usage = ResourceUsage.Staging,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });

            var gpuTexture = new RenderTexture2D(width, height, dxgiFormat, false);

            Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);
            Graphics.SetDepthStencilState(States.ZReadZWriteNoStencil, 0);
            Graphics.SetViewport(0, 0, width, height);
            Graphics.Blit(texture, gpuTexture.Target);
            Graphics.ResetTargets();

            context.CopyResource(gpuTexture.Resource, ramTexture);
            gpuTexture.Dispose();

            var result = new Texture
            {
                Description = ramTexture.Description,
                Resource = ramTexture
            };

            return result;
        }

        public static void Save(this Texture texture, string path, int width, int height)
        {
            var device = Engine.Device;
            var context = Engine.Device.ImmediateContext;
            var wic = Engine.WicFactory;
            var dxgiFormat = SharpDX.DXGI.Format.R8G8B8A8_UNorm;
            var wicFormat = PixelFormatFromFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm);

            if (texture.Description.Format != dxgiFormat)
                wicFormat = PixelFormatFromFormat(dxgiFormat);

            if (wicFormat == Guid.Empty) return;

            Texture2D sourceTexture = texture.Resource;
            Texture2D ramCopy = null;

            if (!texture.Description.CpuAccessFlags.HasFlag(CpuAccessFlags.Read))
            {
                ramCopy = new Texture2D(device, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 0,
                    ArraySize = 1,
                    Format = dxgiFormat,
                    Usage = ResourceUsage.Staging,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None
                });

                var gpuTexture = new RenderTexture2D(width, height, dxgiFormat, false);

                Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);
                Graphics.SetDepthStencilState(States.ZReadZWriteNoStencil, 0);
                Graphics.SetViewport(0, 0, width, height);
                Graphics.Blit(texture, gpuTexture.Target);
                Graphics.ResetTargets();

                context.CopyResource(gpuTexture.Resource, ramCopy);
                gpuTexture.Dispose();
                sourceTexture = ramCopy;
            }

            var dataBox = context.MapSubresource(sourceTexture, 0, 0, MapMode.Read, MapFlags.None, out var dataStream);
            var dataRectangle = new DataRectangle { DataPointer = dataStream.DataPointer, Pitch = dataBox.RowPitch };

            var stream = File.Create(path);
            var bitmap = new Bitmap(wic, width, height, wicFormat, dataRectangle);
            
            BitmapEncoder bitmapEncoder;
            
            if (path.EndsWith(".dds"))
            {
                wicFormat = Guid.Parse("9967cb952E854ac88ca283d7ccd425c9");
                bitmapEncoder = new BitmapEncoder(wic, wicFormat, stream);
            }
            else
            {
                bitmapEncoder = new PngBitmapEncoder(wic, wicFormat, stream);
            }

            var bitmapFrameEncode = new BitmapFrameEncode(bitmapEncoder);
            var pxFormat = wicFormat;// PixelFormat.FormatDontCare;

            bitmapFrameEncode.Initialize();
            bitmapFrameEncode.SetSize(bitmap.Size.Width, bitmap.Size.Height);
            bitmapFrameEncode.SetPixelFormat(ref pxFormat);
            bitmapFrameEncode.WriteSource(bitmap);
            bitmapFrameEncode.Commit();
            bitmapEncoder.Commit();

            context.UnmapSubresource(sourceTexture, 0);

            bitmapEncoder.Dispose();
            bitmapFrameEncode.Dispose();

            ramCopy?.Dispose();
            bitmap.Dispose();
            stream.Dispose();
        }

        public static void Save(this Texture texture, string path)
        {
            texture.Save(path, texture.Description.Width, texture.Description.Height);
        }
    }
         
}