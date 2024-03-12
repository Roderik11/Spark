using SharpDX;
using System.Windows.Forms;

namespace Spark
{
    partial class Mesh
    {
        public static Mesh PatchW()
        {
            int num = 33;
            int count = num * num;
            float offset = (float)(num - 1) / 2;

            var indices = new int[(num - 1) * (num - 1) * 6];
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    // left edge
                    if (x == 0)
                    {
                        if (flipy == 1)
                            v2 = v0;
                        else
                            v0 = x + (y - 1) * num;

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart(){ NumIndices = indices.Length });

            return grid;
        }

        public static Mesh PatchE()
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    // right edge
                    if (x == num - 2)
                    {
                        if (flipy == 1)
                            v3 = (x + 1) + (y + 2) * num;
                        else
                            v1 = v3;

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart() { NumIndices = indices.Length });

            return grid;
        }

        public static Mesh PatchN()
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    if (y == num - 2) // top edge
                    {
                        if (flipx == 1)
                            v3 = (x + 2) + (y + 1) * num;
                        else
                            v2 = (x + 1) + (y + 1) * num;

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart() { NumIndices = indices.Length });

            return grid;
        }

        public static Mesh PatchS()
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    if (y == 0) // top edge
                    {
                        if (flipx == 1)
                            v1 = v0;
                        else
                            v0 = (x - 1) + y * num;

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart() { NumIndices = indices.Length });

            return grid;
        }

        public static Mesh PatchNW()
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    // left edge
                    if (x == 0)
                    {
                        if (flipy == 1)
                            v2 = v0;
                        else
                            v0 = x + (y - 1) * num;

                        if (y == num - 2)
                        {
                            if (flipx == 1)
                                v3 = (x + 2) + (y + 1) * num;
                            else
                                v2 = (x + 1) + (y + 1) * num;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else if (y == num - 2) // top edge
                    {
                        if (flipx == 1)
                            v3 = (x + 2) + (y + 1) * num;
                        else
                            v2 = (x + 1) + (y + 1) * num;

                        if (x == 0)
                        {
                            if (flipy == 1)
                                v2 = v0;
                            else
                                v0 = x + (y - 1) * num;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart() { NumIndices = indices.Length });

            return grid;
        }

        public static Mesh PatchNE()
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    // right edge
                    if (x == num - 2)
                    {
                        if (flipy == 1)
                            v3 = (x + 1) + (y + 2) * num;
                        else
                            v1 = v3;

                        if (y == num - 2)
                        {
                            if (flipx == 1)
                                v3 = (x + 2) + (y + 1) * num;
                            else
                                v2 = (x + 1) + (y + 1) * num;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else if (y == num - 2) // top edge
                    {
                        if (flipx == 1)
                            v3 = (x + 2) + (y + 1) * num;
                        else
                            v2 = (x + 1) + (y + 1) * num;

                        if (x == num - 2)
                        {
                            if (flipy == 1)
                                v3 = (x + 1) + (y + 2) * num;
                            else
                                v1 = v3;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart() { NumIndices = indices.Length });

            return grid;
        }

        public static Mesh PatchSW()
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    // left edge
                    if (x == 0)
                    {
                        if (flipy == 1)
                            v2 = v0;
                        else
                            v0 = x + (y - 1) * num;

                        if (y == 0)
                        {
                            if (flipx == 1)
                                v1 = v0;
                            else
                                v0 = (x - 1) + y * num;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else if (y == 0) // top edge
                    {
                        if (flipx == 1)
                            v1 = v0;
                        else
                            v0 = (x - 1) + y * num;

                        if (x == 0)
                        {
                            if (flipy == 1)
                                v2 = v0;
                            else
                                v0 = x + (y - 1) * num;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart() { NumIndices = indices.Length });

            return grid;
        }

        public static Mesh PatchSE()
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
            int flipy = 0;
            int flipx = 0;

            for (int y = 0; y < num - 1; y++)
            {
                flipy = 1 - flipy;

                for (int x = 0; x < num - 1; x++)
                {
                    flipx = 1 - flipx;

                    int v0 = x + y * num;
                    int v1 = (x + 1) + y * num;
                    int v2 = x + (y + 1) * num;
                    int v3 = (x + 1) + (y + 1) * num;

                    // right edge
                    if (x == num - 2)
                    {
                        if (flipy == 1)
                            v3 = (x + 1) + (y + 2) * num;
                        else
                            v1 = v3;

                        if (y == 0)
                        {
                            if (flipx == 1)
                                v1 = v0;
                            else
                                v0 = (x - 1) + y * num;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else if (y == 0) // top edge
                    {
                        if (flipx == 1)
                            v1 = v0;
                        else
                            v0 = (x - 1) + y * num;

                        if (x == num - 2)
                        {
                            if (flipy == 1)
                                v3 = (x + 1) + (y + 2) * num;
                            else
                                v1 = v3;
                        }

                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                    else
                    {
                        indices[id++] = v2;
                        indices[id++] = v1;
                        indices[id++] = v0;

                        indices[id++] = v2;
                        indices[id++] = v3;
                        indices[id++] = v1;
                    }
                }
            }

            Mesh grid = new Mesh()
            {
                Name = "mesh",
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                UV = uvs,
            };

            grid.MeshParts.Add(new MeshPart() { NumIndices = indices.Length });

            return grid;
        }
    }
}