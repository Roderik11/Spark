using System;
using SharpDX;

namespace Spark
{
    public enum AxisAlignment
    {
        XZ,
        XY,
        YZ
    }

    partial class Mesh
    {
        private static Mesh quadMesh;

        public static Mesh Quad
        {
            get
            {
                if (quadMesh != null)
                    return quadMesh;

                int[] indices = { 0, 1, 2, 2, 3, 0 };

                var verts = new Vector3[]
                {
                        new Vector3( 1,-1, 0),
                        new Vector3(-1,-1, 0),
                        new Vector3(-1, 1, 0),
                        new Vector3( 1, 1, 0),
                };

                var uvs = new Vector2[]
                {
                        new Vector2(1, 1),
                        new Vector2(0, 1),
                        new Vector2(0, 0),
                        new Vector2(1, 0),
                };

                quadMesh = new Mesh { Name = "Quad" };
                quadMesh.Indices = indices;
                quadMesh.Vertices = verts;
                quadMesh.UV = uvs;
                quadMesh.MeshParts.Add(new MeshPart { NumIndices = 6 });

                return quadMesh;
            }
        }

        public static Mesh CreatePatch()
        {
            int num = 33;
            int count = num * num;
            float offset = (float)(num - 1) / 2;

            int[] indices = new int[(num - 1) * (num - 1) * 6];

            var verts = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];

            for (int z = 0; z < num; z++)
            {
                for (int x = 0; x < num; x++)
                {
                    int index = z * num + x;
                    verts[index] = new Vector3(x - offset, 0, z - offset);
                    normals[index] = new Vector3(0, 1, 0);
                    uvs[index] = new Vector2(x, z) / (num - 1);
                }
            }

            int id = 0;

            for (int y = 0; y < num - 1; y++)
            {
                for (int x = 0; x < num - 1; x++)
                {
                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    indices[id++] = v2;
                    indices[id++] = v1;
                    indices[id++] = v0;

                    indices[id++] = v2;
                    indices[id++] = v3;
                    indices[id++] = v1;
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
                BoundingBox = BoundingBox.FromPoints(verts)
            };

            grid.MeshParts.Add(new MeshPart(){ NumIndices = indices.Length });

            return grid;
        }

        public static Mesh CreatePlane(AxisAlignment alignment)
        {
            var verts = new Vector3[4];

            switch (alignment)
            {
                case AxisAlignment.XZ:
                    verts = new Vector3[4] { new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, -0.5f) };
                    break;

                case AxisAlignment.XY:
                    verts = new Vector3[4] { new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, -0.5f, 0) };
                    break;

                case AxisAlignment.YZ:
                    verts = new Vector3[4] { new Vector3(0, 0.5f, -0.5f), new Vector3(0, 0.5f, 0.5f), new Vector3(0, -0.5f, 0.5f), new Vector3(0, -0.5f, -0.5f) };
                    break;
            }

            var normals = new Vector3[4]
            {
                new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 0)
            };

            var tangents = new Vector3[4]
            {
               new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 1), new Vector3(0, 0, 1)
            };

            var binormals = new Vector3[4]
            {
               new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 0)
            };

            var uvs = new Vector2[4]
            {
               new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };

            var indices = new int[] { 2, 3, 0, 2, 0, 1 };

            Mesh plane = new Mesh { Name = "plane" };
            plane.Indices = indices;
            plane.Vertices = verts;
            plane.Normals = normals;
            plane.Tangents = tangents;
            plane.BiNormals = binormals;
            plane.UV = uvs;
            plane.BoundingBox = BoundingBox.FromPoints(verts);
            plane.MeshParts.Add(new MeshPart() { Name = "plane", NumIndices = 6 });

            return plane;
        }

        public static Mesh CreateBox(float size)
        {
            float w = size * 0.5f;

            var verts = new Vector3[]
            {
                new Vector3(-w, w, w), new Vector3( w, w, w), new Vector3( w, w,-w), new Vector3(-w, w,-w), // top
                new Vector3(-w,-w, w), new Vector3( w,-w, w), new Vector3( w,-w,-w), new Vector3(-w,-w,-w), // bottom
                new Vector3(-w, w, w), new Vector3(-w,-w, w), new Vector3(-w,-w,-w), new Vector3(-w, w,-w), // left
                new Vector3( w, w, w), new Vector3( w,-w, w), new Vector3( w,-w,-w), new Vector3( w, w,-w), // right
                new Vector3(-w, w,-w), new Vector3( w, w,-w), new Vector3( w,-w,-w), new Vector3(-w,-w,-w), // front
                new Vector3(-w, w, w), new Vector3( w, w, w), new Vector3( w,-w, w), new Vector3(-w,-w, w), // back
            };

            var normals = new Vector3[]
            {
                new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), // top
                new Vector3( 0,-1, 0), new Vector3( 0,-1, 0), new Vector3( 0,-1, 0), new Vector3( 0,-1, 0), // bottom
                new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, 0, 0), // left
                new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), // right
                new Vector3( 0, 0,-1), new Vector3( 0, 0,-1), new Vector3( 0, 0,-1), new Vector3( 0, 0,-1), // front
                new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), // back
            };

            var tangents = new Vector3[]
            {
                new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), // top
                new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), // bottom
                new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), // left
                new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), new Vector3( 0, 0, 1), // right
                new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), // front
                new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), // back
            };

            var biNormals = new Vector3[]
            {
                new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), // top
                new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), // bottom
                new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), // left
                new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), new Vector3( 0, 1, 0), // right
                new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), // front
                new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), new Vector3( 1, 0, 0), // back
            };

            var uvs = new Vector2[]
            {
                new Vector2( 0, 0), new Vector2( 1, 0), new Vector2( 1, 1), new Vector2( 0, 1), // top
                new Vector2( 0, 0), new Vector2( 1, 0), new Vector2( 1, 1), new Vector2( 0, 1), // bottom
                new Vector2( 0, 0), new Vector2( 1, 0), new Vector2( 1, 1), new Vector2( 0, 1), // left
                new Vector2( 0, 0), new Vector2( 1, 0), new Vector2( 1, 1), new Vector2( 0, 1), // right
                new Vector2( 0, 0), new Vector2( 1, 0), new Vector2( 1, 1), new Vector2( 0, 1), // front
                new Vector2( 0, 0), new Vector2( 1, 0), new Vector2( 1, 1), new Vector2( 0, 1), // back
            };

            var indices = new int[]
            {
                // top
                2, 3, 0, 2, 0, 1,
                // bottom
                4, 7, 6, 5, 4, 6,
                // left
                8, 11, 10, 9, 8, 10,
                // right
                14, 15, 12, 14, 12, 13,
                // front
                18, 19, 16, 18, 16, 17,
                // back
                20, 23, 22, 21, 20, 22,
            };

            Mesh box = new Mesh { Name = "cube" };
            box.Indices = indices;
            box.Vertices = verts;
            box.Normals = normals;
            box.Tangents = tangents;
            box.BiNormals = biNormals;
            box.UV = uvs;
            box.BoundingBox = new BoundingBox(Vector3.One * -w, Vector3.One * w);
            box.MeshParts.Add(new MeshPart() { Name = "box", NumIndices = 36 });

            return box;
        }

        public static Mesh CreateSphere(float radius, int stacks, int slices)
        {
            int numVerts = (stacks + 1) * (slices + 1);
            int numIndices = (3 * stacks * (slices + 1)) * 2;

            int[] indices = new int[numIndices];
            
            var verts = new Vector3[numVerts];
            var normals = new Vector3[numVerts];
            var uvs = new Vector2[numVerts];

            //calculates the resulting number of vertices and indices

            float StackAngle = MathHelper.Pi / (float)stacks;
            float SliceAngle = (float)(Math.PI * 2.0) / (float)slices;

            int index = 0;
            int idcount = 0;
            int vertcount = 0;

            for (int stack = 0; stack < (stacks + 1); stack++)
            {
                float r = (float)Math.Sin((float)stack * StackAngle);
                float y = (float)Math.Cos((float)stack * StackAngle);

                //Generate the group of segments for the current Stack
                for (int slice = 0; slice < (slices + 1); slice++)
                {
                    float x = r * (float)Math.Sin((float)slice * SliceAngle);
                    float z = r * (float)Math.Cos((float)slice * SliceAngle);

                    verts[vertcount] = new Vector3(x * radius, y * radius, z * radius); //normalized
                    normals[vertcount] = Vector3.Normalize(new Vector3(x, y, z));
                    uvs[vertcount] = new Vector2((float)slice / (float)slices, (float)stack / (float)stacks);

                    vertcount++;

                    if (!(stack == (stacks - 1)))
                    {
                        indices[idcount] = index + (slices + 1);
                        idcount++;
                        indices[idcount] = index + 1;
                        idcount++;
                        indices[idcount] = index;
                        idcount++;
                        indices[idcount] = index + (slices);
                        idcount++;
                        indices[idcount] = index + (slices + 1);
                        idcount++;
                        indices[idcount] = index;
                        idcount++;
                        index++;
                    }
                }
            }

            Mesh mesh = new Mesh { Name = "sphere" };
            mesh.Indices = indices;
            mesh.Vertices = verts;
            mesh.Normals = normals;
            mesh.UV = uvs;
            mesh.BoundingBox = BoundingBox.FromSphere(new BoundingSphere(Vector3.Zero, radius));
            mesh.MeshParts.Add(new MeshPart() { Name = "sphere", NumIndices = indices.Length });

            return mesh;
        }

    }
}