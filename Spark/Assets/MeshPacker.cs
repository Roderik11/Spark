using SharpDX.Direct3D;
using SharpDX;
using System;
using System.ComponentModel;
using System.IO;

namespace Spark
{
    public class MeshPacker : AssetPacker<Mesh>
    {
        public override void Pack(BinaryWriter writer, Mesh mesh)
        {
            writer.WriteRange(mesh.Indices);
            writer.WriteRange(mesh.Vertices);
            writer.WriteRange(mesh.Normals);
            writer.WriteRange(mesh.Tangents);
            writer.WriteRange(mesh.BiNormals);
            writer.WriteRange(mesh.UV);
            writer.WriteRange(mesh.UV1);
            writer.WriteRange(mesh.UV2);
            writer.WriteRange(mesh.Boneweights);

            writer.Write(mesh.RootRotation);
            writer.Write((int)mesh.Topology);
            writer.Write(mesh.BoundingBox);

            writer.Write(mesh.MeshParts.Count);
            foreach (var part in mesh.MeshParts)
                part.Write(writer);

            if (mesh.Bones == null)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(mesh.Bones.Length);
                foreach (var bone in mesh.Bones)
                    bone.Write(writer);
            }
        }

        public override Mesh Unpack(BinaryReader reader)
        {
            var mesh = new Mesh();

            mesh.Indices = reader.ReadRange<int>();
            mesh.Vertices = reader.ReadRange<Vector3>();
            mesh.Normals = reader.ReadRange<Vector3>();
            mesh.Tangents = reader.ReadRange<Vector3>();
            mesh.BiNormals = reader.ReadRange<Vector3>();
            mesh.UV = reader.ReadRange<Vector2>();
            mesh.UV1 = reader.ReadRange<Vector2>();
            mesh.UV2 = reader.ReadRange<Vector2>();
            mesh.Boneweights = reader.ReadRange<BoneWeight>();

            mesh.RootRotation = reader.Read<Matrix>();
            mesh.Topology = (PrimitiveTopology)reader.ReadInt32();
            mesh.BoundingBox = reader.Read<BoundingBox>();

            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var part = new MeshPart();
                part.Read(reader);
                mesh.MeshParts.Add(part);
            }

            int boneCount = reader.ReadInt32();
            if (boneCount > 0)
            {
                mesh.Bones = new Bone[boneCount];
                for (int i = 0; i < boneCount; i++)
                {
                    mesh.Bones[i] = new Bone();
                    mesh.Bones[i].Read(reader);
                }
            }
            return mesh;
        }
    }

}