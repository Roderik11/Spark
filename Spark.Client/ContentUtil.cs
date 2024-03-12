using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace Spark.Client
{
    public class PlacementGrid
    {
        private readonly float width;
        private readonly float height;

        private readonly float cellwidth;
        private readonly float cellheight;

        private Dictionary<int, List<Node>> cells = new Dictionary<int, List<Node>>();

        struct Node
        {
            public Vector3 position;
            public float radius;
        }

        public PlacementGrid(float width, float height, float cellwidth, float cellheight)
        {
            this.width = width;
            this.height = height;

            this.cellwidth = cellwidth;
            this.cellheight = cellheight;
        }

        public bool GeneratePosition(float radius, out Vector3 position)
        {
            position = Vector3.Zero;
            position.X = RNG.RangeFloat(0, width);
            position.Z = RNG.RangeFloat(0, height);

            int idx = (int)Math.Floor(position.X / cellwidth);
            int idz = (int)Math.Floor(position.Z / cellheight);
            int hash = (idx, idz).GetHashCode();

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    int nidx = idx + x;
                    int nidz = idx + z;

                    var h = (nidx, nidz).GetHashCode();
                    var valid = TryCell(h, position, radius);

                    if (!valid) return false;
                }
            }

            AddNode(hash, position, radius);

            return true;
        }

        void AddNode(int hash, Vector3 position, float radius)
        {
            if (cells.TryGetValue(hash, out List<Node> list))
            {
                list.Add(new Node { position = position, radius = radius });
            }
            else
            {
                cells.Add(hash, new List<Node> { new Node { position = position, radius = radius } });
            }
        }

        bool TryCell(int hash, Vector3 position, float radius)
        {
            if (cells.TryGetValue(hash, out List<Node> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var dist = Vector3.DistanceSquared(position, list[i].position);

                    if (dist < (radius * radius) + (list[i].radius * list[i].radius))
                        return false;
                }
            }

            return true;
        }
    }

    public class Placer
    {
        private PlacementGrid grid;

        public Placer(float size, float cellsize)
        {
            grid = new PlacementGrid(size, size, cellsize, cellsize);
        }

        private Material CreateMaterial(string path)
        {
            var diffuse = Engine.Assets.Load<Texture>($"{path}.dds");
            var normal = Engine.Assets.Load<Texture>($"{path}_n.dds");
            var specular = Engine.Assets.Load<Texture>($"{path}_s.dds");
            var ds = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");

            Effect trunkEffect = new Effect("mesh_opaque");
            trunkEffect.SetValue("Textures", new List<Texture>() { diffuse, normal, ds });
            trunkEffect.SetValue("sampData", Samplers.WrappedAnisotropic);
            trunkEffect.BlendState = States.BlendNone;
            trunkEffect.DepthStencilState = States.ZReadZWriteNoStencil;

            Material material = new Material (trunkEffect);
            material.UseInstancing = true;
            return material;
        }

        public void AddLODModel(string name, int count, Vector3 center, float minSize, float maxSize, float range, Terrain terrain)
        {
            var path = name;
            var diffuse = Engine.Assets.Load<Texture>($"{path}.dds");
            var normal = Engine.Assets.Load<Texture>($"{path}_n.dds");
            var specular = Engine.Assets.Load<Texture>($"{path}_s.dds");

            specular = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");
            //diffuse = Engine.Content.Load<Texture>("DefaultColorMap.dds");
            normal = Engine.Assets.Load<Texture>("DefaultNormalMap.dds");

            var mat0 = new Material (new Effect("mesh_opaque"));
            mat0.SetValue("Textures", new List<Texture>() { diffuse, normal, specular });
            mat0.SetValue("sampData", Samplers.WrappedAnisotropic);
            //var mat1 = CreateMaterial($"{path}_1");
            //var mat2 = CreateMaterial($"{path}_2");

            var mesh0 = Engine.Assets.Load<Mesh>($"{path}_0.obj");
            var mesh1 = Engine.Assets.Load<Mesh>($"{path}_1.obj");
            var mesh2 = Engine.Assets.Load<Mesh>($"{path}_2.obj");

            LODMesh lod0 = new LODMesh { Materials = { mat0, mat0 }, Mesh = mesh0, Range = 40 };
            LODMesh lod1 = new LODMesh { Materials = { mat0, mat0 }, Mesh = mesh1, Range = 100 };
            LODMesh lod2 = new LODMesh { Materials = { mat0, mat0 }, Mesh = mesh2, Range = 512 };

            System.Random rnd = new Random();
            int finalcount = 0;

            Debug.Log($"Requested Count: {count}");

            var group = new Entity { Name = name };

            for (int i = 0; i < count; i++)
            {
                var size = rnd.NextFloat(minSize, maxSize) * .04f;
                var rndpos = Vector3.Zero;
                bool foundposition = false;
                int iterations = 0;
                int maxiterations = 100;

                while (!foundposition)
                {
                    foundposition = grid.GeneratePosition(5, out rndpos);

                    var h = terrain.GetHeight(center + (rndpos - new Vector3(range, 0, range)));
                    if (h > 50)
                        foundposition = false;

                    iterations++;
                    if (iterations >= maxiterations)
                        break;
                }

                if (iterations >= maxiterations)
                    break;

                rndpos -= new Vector3(range, 0, range);

                var position = center + rndpos;
                // var position = center + new Vector3(rnd.NextFloat(-range, range), 0, rnd.NextFloat(-range, range));

                var entity = new Entity($"{name}", new LODMeshRenderer { LODs = { lod0, lod1, lod2 } });
                // new RigidBody { Type = ShapeType.Box, Mesh = randomTree, IsStatic = true });
                entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.Up, rnd.NextFloat(0, 90));
                entity.Transform.Scale = Vector3.One * size;

                if (terrain != null)
                    position.Y = terrain.GetHeight(position);

                //position.Y += scale.Y / 2;

                entity.Transform.Position = position;
                entity.Transform.Parent = group.Transform;
                //box.GetComponent<RigidBody>().DebugDraw = true;
                finalcount++;
            }

            Debug.Log($"Final Count: {finalcount}");

        }
    }

    public static class SceneUtil
    {
        private static LODMeshRenderer LoadTreeLOD(string name)
        {
            var tree = name;
            var diffuse = Engine.Assets.Load<Texture>($"Trees/{tree}/{tree}.dds");
            var normal = Engine.Assets.Load<Texture>($"Trees/{tree}/{tree}_n.dds");
            var specular = Engine.Assets.Load<Texture>($"Trees/{tree}/{tree}_s.dds");

            var mesh0 = Engine.Assets.Load<Mesh>($"Trees/{tree}/{tree}_0.obj");
            var mesh1 = Engine.Assets.Load<Mesh>($"Trees/{tree}/{tree}_1.obj");

            Effect effect = new Effect("mesh_opaque");
            effect.SetValue("Albedo", diffuse);
            effect.SetValue("Normal", normal);
            effect.SetValue("Data", specular);
           // effect.SetValue("Textures", new List<Texture>() { diffuse, normal, specular });
            effect.SetValue("sampData", Samplers.WrappedAnisotropic);
            effect.BlendState = States.BlendNone;
            effect.DepthStencilState = States.ZReadZWriteNoStencil;

            //Effect shadowEffect = new Effect("mesh_shadow");
            //shadowEffect.RenderPass = RenderPass.Shadow;
            //shadowEffect.RasterizerState = States.FrontCull;
            //shadowEffect.UseInstancing = true;

            Material material = new Material (effect);
            material.UseInstancing = true;

            var lod0 = new LODMesh { Materials = { material, material}, Mesh = mesh0, Range = 10 };
            var lod1 = new LODMesh { Materials = { material, material}, Mesh = mesh1, Range = 500 };

            var lodmesh = new LODMeshRenderer { LODs = { lod0, lod1 } };

            return lodmesh;
        }

        public static Material CreateMaterial(string path)
        {
            var diffuse = Engine.Assets.Load<Texture>($"{path}.dds");
            var normal = Engine.Assets.Load<Texture>($"{path}_n.dds");
            var specular = Engine.Assets.Load<Texture>($"{path}_s.dds");
            //diffuse  = Engine.Content.Load<Texture>("DefaultColorMap.dds");
            var ds = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");

            Effect effect = new Effect("mesh_opaque");
            effect.SetValue("Albedo", diffuse);
            effect.SetValue("Normal", normal);
            effect.SetValue("Data", ds);
            //effect.SetValue("Textures", new List<Texture>() { diffuse, normal, ds });
            effect.SetValue("sampData", Samplers.WrappedAnisotropic);
            effect.BlendState = States.BlendNone;
            effect.DepthStencilState = States.ZReadZWriteNoStencil;

            //Effect shadowEffect = new Effect("mesh_shadow");
            //shadowEffect.RenderPass = RenderPass.Shadow;
            //shadowEffect.RasterizerState = States.FrontCull;
            //shadowEffect.UseInstancing = true;
            //shadowEffect.SetValue("DiffuseMap", diffuse );
            //shadowEffect.SetValue("sampData", Samplers.WrappedAnisotropic);

            Material material = new Material (effect);
            material.Name = Path.GetFileNameWithoutExtension(path);
            material.UseInstancing = true;

            return material;
        }

        
        public static Material CreateMaterial(string diffuseMap, string normalMap, string specularMap, bool wind = false)
        {
            var diffuse = Engine.Assets.Load<Texture>(diffuseMap);
            var normal = Engine.Assets.Load<Texture>(normalMap);
            var specular = Engine.Assets.Load<Texture>(specularMap);
            //diffuse  = Engine.Content.Load<Texture>("DefaultColorMap.dds");
            var ds = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");

            Effect effect = new Effect(wind ? "tree" : "mesh_opaque");
            effect.SetValue("Albedo", diffuse);
            effect.SetValue("Normal", normal);
            effect.SetValue("Data", ds);
            effect.SetValue("sampData", Samplers.WrappedAnisotropic);
            effect.BlendState = States.BlendNone;
            effect.DepthStencilState = States.ZReadZWriteNoStencil;

            //Effect shadowEffect = new Effect(wind ? "tree_shadow" : "mesh_shadow");
            //shadowEffect.SetValue("DiffuseMap", diffuse);
            //shadowEffect.SetValue("sampData", Samplers.WrappedAnisotropic);
            //shadowEffect.RenderPass = RenderPass.Shadow;
            //shadowEffect.RasterizerState = States.FrontCull;
            //shadowEffect.UseInstancing = true;

            Material material = new Material (effect);
            material.Name = "RockMaterial";
            material.UseInstancing = true;

            return material;
        }

        public static List<LODMesh> CreateLod(string name)
        {
            var material = CreateMaterial($"Rocks/{name}/Textures/{name}_Dif.png", $"Rocks/{name}/Textures/{name}_Nor.png", $"Rocks/{name}/Textures/{name}_Spec.png");
            var mesh = Engine.Assets.Load<Mesh>($"Rocks/{name}/{name}.fbx", ("Scale", 0.01f));
          
            var lod = new List<LODMesh>
            {
                new LODMesh { Mesh = mesh, Materials = { material }, Range = 32, StartIndex = 0 },
                new LODMesh { Mesh = mesh, Materials = { material }, Range = 64, StartIndex = 1 },
                new LODMesh { Mesh = mesh, Materials = { material }, Range = 96, StartIndex = 2 },
                new LODMesh { Mesh = mesh, Materials = { material }, Range = 1024, StartIndex = 3 }
            };

            return lod;
        }

      public static List<LODMesh> GetTreeLOD(string name)
        {
            string prefix = $"Trees/{name}/{name}";

            var material = CreateMaterial($"{prefix}.dds", $"{prefix}_Normal.png", "DefaultSpecularMap.dds", true);
            var imposter = CreateMaterial($"{prefix}_LOD5.dds", $"{prefix}_LOD5_Normal.dds", "DefaultSpecularMap.dds", false);
            imposter.Effect.RasterizerState = States.NoCull;

            var meta = ("Scale", .1f);
            var small = ("Scale", .6f);
            var lod0 = Engine.Assets.Load<Mesh>($"{prefix}.obj", meta);
            var lod1 = Engine.Assets.Load<Mesh>($"{prefix}_LOD1.obj", meta);
            var lod2 = Engine.Assets.Load<Mesh>($"{prefix}_LOD2.obj", meta);
            var lod3 = Engine.Assets.Load<Mesh>($"{prefix}_LOD3.obj", meta);
            var lod4 = Engine.Assets.Load<Mesh>($"{prefix}_LOD4.obj", meta);
            var lod5 = Engine.Assets.Load<Mesh>($"{prefix}_LOD5.obj", small);

            //var rangeBase = 1024 / 6f;
            //return new List<LODMesh>
            //{
            //    new LODMesh { Mesh = lod0, Materials = { material }, Range = rangeBase * 1},
            //    new LODMesh { Mesh = lod1, Materials = { material }, Range = rangeBase * 2},
            //    new LODMesh { Mesh = lod2, Materials = { material }, Range = rangeBase * 3},
            //    new LODMesh { Mesh = lod3, Materials = { material }, Range = rangeBase * 4},
            //    new LODMesh { Mesh = lod4, Materials = { material }, Range = rangeBase * 5},
            //    new LODMesh { Mesh = lod5, Materials = { imposter }, Range = rangeBase * 6}
            //};

            int start = 4;
            return new List<LODMesh>
            {
                new LODMesh { Mesh = lod0, Materials = { material }, Range = (int)Math.Pow(2, 4)},
                new LODMesh { Mesh = lod1, Materials = { material }, Range = (int)Math.Pow(2, 5)},
                new LODMesh { Mesh = lod2, Materials = { material }, Range = (int)Math.Pow(2, 6)},
                new LODMesh { Mesh = lod3, Materials = { material }, Range = (int)Math.Pow(2, 7)},
                new LODMesh { Mesh = lod4, Materials = { material }, Range = (int)Math.Pow(2, 9)},
                new LODMesh { Mesh = lod5, Materials = { imposter }, Range = (int)Math.Pow(2, 10)}
            };
        }

        public static void AddModel(List<LODMesh> lods, int count, Vector3 center, float minSize, float maxSize, float range, Terrain terrain, float maxRange = 512, bool rigid = false, Mesh collisionMesh = null)
        {
            var sizes = (maxSize - minSize) / 10;

            var grid = new PlacementGrid(range * 2, range * 2, 32, 32);
            int finalcount = 0;

            Debug.Log($"Requested Count: {count}");

            //var group = new Entity { Name = "lodgroup" };

            for (int i = 0; i < count; i++)
            {
                //var randomSize = RNG.RangeFloat(minSize, maxSize);
                var randomSize = sizes * RNG.RangeInt(0, 10);
                var size = (minSize + randomSize);
                var rndpos = Vector3.Zero;
                bool foundposition = false;
                int iterations = 0;
                int maxiterations = 10;

                while (!foundposition)
                {
                    foundposition = grid.GeneratePosition(size / 2, out rndpos);

                    //var h = terrain.GetHeight(center + (rndpos - new Vector3(range, 0, range)));
                    //if (h > 50)
                    //    foundposition = false;

                    iterations++;
                    if (iterations >= maxiterations)
                        break;
                }

                if (iterations >= maxiterations)
                    break;

                rndpos -= new Vector3(range, 0, range);

                var position = center + rndpos;
                // var position = center + new Vector3(rnd.NextFloat(-range, range), 0, rnd.NextFloat(-range, range));

                var entity = new Entity("tree", new LODMeshRenderer { LODs = lods });
                if (rigid)
                {
                    var rigidBody = entity.AddComponent<RigidBody>();
                    rigidBody.Mesh = collisionMesh ?? lods[lods.Count - 1].Mesh;
                    rigidBody.Type = ShapeType.Mesh;
                    rigidBody.IsStatic = true;
                    rigidBody.AutoInsert = false;
                }
                // new RigidBody { Type = ShapeType.Box, Mesh = randomTree, IsStatic = true });
                entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.Up, RNG.RangeFloat(0, 90));
                entity.Transform.Scale = Vector3.One * size;

                if (terrain != null)
                    position.Y = terrain.GetHeight(position);

                //position.Y += scale.Y / 2;

                entity.Transform.Position = position;
                //entity.Transform.Parent = group.Transform;
                //box.GetComponent<RigidBody>().DebugDraw = true;
                finalcount++;
            }

            Debug.Log($"Final Count: {finalcount}");
        }

        public static void AddModel(string path, int count, Vector3 center, float minSize, float maxSize, float range, Terrain terrain, float maxRange = 512, bool rigid = false, Mesh collisionMesh = null)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            var fullpath = Engine.Assets.BaseDirectory + System.IO.Path.GetDirectoryName(path) + "\\";

            var mat0 = CreateMaterial($"{path}");

            var files = System.IO.Directory.GetFiles(fullpath, $"{name}_*.obj");

            var sizes = (maxSize - minSize) / 10;

            var lods = new List<LODMesh>();
            float lodrange = 64;
            foreach(var file in files)
            {
                var filepath = file.Replace(Engine.Assets.BaseDirectory, "");
                var mesh = Engine.Assets.Load<Mesh>($"{filepath}");
                var lod = new LODMesh { Materials = { mat0, mat0 }, Mesh = mesh, Range = lodrange };
                lods.Add(lod);
                lodrange *= 2;

                if (lodrange >= maxRange)
                    break;
            }

            if(lods.Count > 0)
            {
                lods[lods.Count - 1].Range = maxRange;
            }

            var grid = new PlacementGrid(range * 2, range * 2, 10, 10);
            int finalcount = 0;

            Debug.Log($"Requested Count: {count}");

            //var group = new Entity { Name = "lodgroup" };

            for (int i = 0; i < count; i++)
            {
                //var randomSize = RNG.RangeFloat(minSize, maxSize);
                var randomSize = sizes * RNG.RangeInt(0, 10);
                var size = (minSize + randomSize) * .04f;
                var rndpos = Vector3.Zero;
                bool foundposition = false;
                int iterations = 0;
                int maxiterations = 100;

                while (!foundposition)
                {
                    foundposition = grid.GeneratePosition(randomSize / 2, out rndpos);

                    //var h = terrain.GetHeight(center + (rndpos - new Vector3(range, 0, range)));
                    //if (h > 50)
                    //    foundposition = false;

                    iterations++;
                    if (iterations >= maxiterations)
                        break;
                }

                if (iterations >= maxiterations)
                    break;

                rndpos -= new Vector3(range, 0, range);

                var position = center + rndpos;
                // var position = center + new Vector3(rnd.NextFloat(-range, range), 0, rnd.NextFloat(-range, range));

                var entity = new Entity($"{name}", new LODMeshRenderer { LODs = lods });
                if (rigid)
                {
                    var rigidBody = entity.AddComponent<RigidBody>();
                    rigidBody.Mesh = collisionMesh ?? lods[lods.Count - 1].Mesh;
                    rigidBody.Type = ShapeType.Mesh;
                    rigidBody.IsStatic = true;
                    rigidBody.AutoInsert = false;
                }
                // new RigidBody { Type = ShapeType.Box, Mesh = randomTree, IsStatic = true });
                entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.Up, RNG.RangeFloat(0, 90));
                entity.Transform.Scale = Vector3.One * size;

                if (terrain != null)
                    position.Y = terrain.GetHeight(position);

                //position.Y += scale.Y / 2;

                entity.Transform.Position = position;
                //entity.Transform.Parent = group.Transform;
                //box.GetComponent<RigidBody>().DebugDraw = true;
                finalcount++;
            }

            Debug.Log($"Final Count: {finalcount}");

        }

        public static void AddTreesLOD(int count, Vector3 center, float minSize, float maxSize, float range, Terrain terrain)
        {
            var name = "beech";
            var path = $"Trees/{name}/{name}";

            var mat0 = CreateMaterial($"{path}_0");
            //var mat1 = CreateMaterial($"{path}_1");
            //var mat2 = CreateMaterial($"{path}_2");

            var mesh0 = Engine.Assets.Load<Mesh>($"{path}_0.obj");
            var mesh1 = Engine.Assets.Load<Mesh>($"{path}_1.obj");
            var mesh2 = Engine.Assets.Load<Mesh>($"{path}_2.obj");

            LODMesh lod0 = new LODMesh { Materials = { mat0, mat0}, Mesh = mesh0, Range = 40 };
            //LODMesh lod1 = new LODMesh { Materials = { mat0, mat0}, Mesh = mesh1, Range = 100 };
            LODMesh lod2 = new LODMesh { Materials = { mat0, mat0}, Mesh = mesh2, Range = 512 };

            System.Random rnd = new Random();
            var grid = new PlacementGrid(range * 2, range * 2, 10, 10);
            int finalcount = 0;

            Debug.Log($"Requested Count: {count}");

            var group = new Entity { Name = "treegroup" };

            for (int i = 0; i < count; i++)
            {
                var randomSize = rnd.NextFloat(minSize, maxSize);
                var size = randomSize * .04f;
                var rndpos = Vector3.Zero;
                bool foundposition = false;
                int iterations = 0;
                int maxiterations = 100;

                while(!foundposition)
                {
                    foundposition = grid.GeneratePosition(randomSize / 3, out rndpos);

                    //var h = terrain.GetHeight(center + (rndpos - new Vector3(range, 0, range)));
                    //if (h > 50)
                    //    foundposition = false;

                    iterations++;
                    if (iterations >= maxiterations)
                        break;
                }

                if (iterations >= maxiterations)
                    break;

                rndpos -= new Vector3(range, 0, range);

                var position = center + rndpos;
                // var position = center + new Vector3(rnd.NextFloat(-range, range), 0, rnd.NextFloat(-range, range));

                var entity = new Entity($"{name} tree", new LODMeshRenderer { LODs = { lod0, lod2 } });
                // new RigidBody { Type = ShapeType.Box, Mesh = randomTree, IsStatic = true });
                entity.Transform.Rotation = Quaternion.RotationAxis(Vector3.Up, rnd.NextFloat(0, 90));
                entity.Transform.Scale = Vector3.One * size;

                if (terrain != null)
                    position.Y = terrain.GetHeight(position);

                //position.Y += scale.Y / 2;

                entity.Transform.Position = position;
                entity.Transform.Parent = group.Transform;
                //box.GetComponent<RigidBody>().DebugDraw = true;
                finalcount++;
            }

            Debug.Log($"Final Count: {finalcount}");

        }

        public static void AddBoxes(int count, Vector3 center, float size, float range, Terrain terrain)
        {
            Texture tex1 = Engine.Assets.Load<Texture>("DefaultColorMap.dds");
            Texture tex2 = Engine.Assets.Load<Texture>("DefaultNormalMap.dds");
            Texture tex3 = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");
            Mesh cube = Mesh.CreateBox(1);

            Effect effect1 = new Effect("mesh_opaque");
            effect1.SetValue("Textures", new List<Texture>() { tex1, tex2, tex3 });

            Material material1 = new Material (effect1);

            System.Random rnd = new Random();

            for (int i = 0; i < count; i++)
            {
                var scale = Vector3.One * size;
               // scale.X = rnd.NextFloat(8, 16);
               // scale.Z = rnd.NextFloat(8, 16);
               scale.Y = rnd.NextFloat(4, 10);

                var box = new Entity($"large box {i}",
                    new MeshRenderer { Materials = { material1 }, Mesh = cube },
                    new RigidBody { Type = ShapeType.Box, Mesh = cube, IsStatic = true });

                var position = center + new Vector3(rnd.NextFloat(-range, range), 0, rnd.NextFloat(-range, range));
                box.Transform.Rotation = Quaternion.RotationAxis(Vector3.Up, rnd.NextFloat(0, 90));
                box.Transform.Scale = scale;

                if (terrain != null)
                    position.Y =  terrain.GetHeight(position);

                position.Y += scale.Y / 2;

                box.Transform.Position = position;
                //box.GetComponent<RigidBody>().DebugDraw = true;
            }
        }

        public static Entity AddCapsule(Vector3 position, float length, float radius)
        {
            Texture tex1 = Engine.Assets.Load<Texture>("DefaultColorMap.dds");
            Texture tex2 = Engine.Assets.Load<Texture>("DefaultNormalMap.dds");
            Texture tex3 = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");
            Mesh cube = Mesh.CreateBox(1);

            Effect effect1 = new Effect("mesh_opaque");
            effect1.SetValue("Textures", new List<Texture>() { tex1, tex2, tex3 });

            Material material1 = new Material (effect1);

            var thing = new Entity("capsule", new MeshRenderer { Materials = { material1 }, Mesh = cube });
            thing.AddComponent<MeshRenderer>().Materials.Add(material1);
            thing.Transform.Scale = new Vector3(radius, length, radius);

            thing.Transform.Position = position;
            var rb = thing.AddComponent<RigidBody>();
            rb.Mesh = cube;
            rb.Type = ShapeType.Capsule;
            rb.DebugDraw = true;

            return thing;
        }

        public static Entity AddNPC(AssetManager content, Vector3 position)
        {
            var npc = CreateAAAModel(content);
            npc.AddComponent<NPCController>().Height = 1f;
            npc.Transform.Position = position;// new Vector3(0, 5, 0);
            return npc;
        }

        public static Entity AddPlayer(AssetManager content, Vector3 position)
        {
            var player = CreateAAAModel(content);
            var pos = position;

            player.AddComponent<CharacterController>().Height = 1.3f;
            //player.AddComponent<CharacterController>().Center = new Vector3(0, 1, 0);
            player.Transform.Position = pos;// new Vector3(0, 5, 0);

            return player;
        }

        public static void CreateBlendTreeAnimations(Entity entity)
        {
            var scale = ("Scale", .01f);

            var walkAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Walking.dae-anim", scale);
            var idleAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Idle.dae-anim", scale);
            var runAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Running.dae-anim", scale);
            var walkBack = Engine.Assets.Load<AnimationClip>("Characters/Knight/Walking Backward.dae-anim", scale);
            var strafeRightAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Right Strafe Walking.dae-anim", scale);
            var strafeLeftAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Left Strafe Walking.dae-anim", scale);
            var strafeRightRunAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Right Strafe.dae-anim", scale);
            var strafeLeftRunAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Left Strafe.dae-anim", scale);

            //var jumpAnim = Engine.Content.Load<AnimationClip>("Characters/Knight/Jump.dae-anim", meta);
            // var jumpAnim = Engine.Content.Load<AnimationClip>("Characters/Knight/Unarmed Jump Running.dae-anim", meta);

            //var strafeRightRunAnim = Engine.Content.Load<AnimationClip>("Characters/Knight/Jog Strafe Right.dae-anim", meta);
            //var strafeLeftRunAnim = Engine.Content.Load<AnimationClip>("Characters/Knight/Jog Strafe Left.dae-anim", meta);

            var jogBack = Engine.Assets.Load<AnimationClip>("Characters/Knight/Jog Backward.dae-anim", scale);

            var jogFWLeft = Engine.Assets.Load<AnimationClip>("Characters/Knight/Jog Forward Left.dae-anim", scale);
            var jogFWRight = Engine.Assets.Load<AnimationClip>("Characters/Knight/Jog Forward Right.dae-anim", scale);

            var jogBackLeft = Engine.Assets.Load<AnimationClip>("Characters/Knight/Jog Backward Left.dae-anim", scale);
            var jogBackRight = Engine.Assets.Load<AnimationClip>("Characters/Knight/Jog Backward Right.dae-anim", scale);


            var jumpAnim = Engine.Assets.Load<AnimationClip>("Characters/Knight/Jumping Up Quick.dae-anim", scale);
            var falling = Engine.Assets.Load<AnimationClip>("Characters/Knight/Falling Idle.dae-anim", scale);
            var landing = Engine.Assets.Load<AnimationClip>("Characters/Knight/Jumping Down Quick.dae-anim", scale);

            var idleState = new AnimationState { Name = "Idle", Clip = idleAnim, Loop = true };
            var walkState = new AnimationState { Name = "Walk", Clip = walkAnim, Loop = true };
            var walkBackState = new AnimationState { Name = "WalkBack", Clip = walkBack, Loop = true };
            var runState = new AnimationState { Name = "Run", Clip = runAnim, Loop = true };
            var strafeRightState = new AnimationState { Name = "StrafeRight", Clip = strafeRightAnim, Loop = true };
            var strafeLeftState = new AnimationState { Name = "StrafeLeft", Clip = strafeLeftAnim, Loop = true };
            var strafeRightRunState = new AnimationState { Name = "StrafeRightRun", Clip = strafeRightRunAnim, Loop = true };
            var strafeLeftRunState = new AnimationState { Name = "StrafeLeftRun", Clip = strafeLeftRunAnim, Loop = true };
            var jumpState = new AnimationState { Name = "Jump", Clip = jumpAnim, Loop = false };


            var jogBackState = new AnimationState { Name = "JogBack", Clip = jogBack, Loop = true };

            var jogBackLeftState = new AnimationState { Name = "JobBackLeft", Clip = jogBackLeft, Loop = true };
            var jogBackRightState = new AnimationState { Name = "JogBackRight", Clip = jogBackRight, Loop = true };

            var jogFWLeftState = new AnimationState { Name = "JogForwardLeft", Clip = jogFWLeft, Loop = true };
            var jogFWRightState = new AnimationState { Name = "JogForwardRight", Clip = jogFWRight, Loop = true };

            var fallingState = new AnimationState { Name = "Falling", Clip = falling, Loop = true };
            var landingState = new AnimationState { Name = "Landing", Clip = landing, Loop = false };


            // state:
            // idle
            // jump start
            // fall loop
            // landing idle
            // landing running
            // locomotion

            //var curveColliderY = new AnimationCurve { TargetParameter = "ColliderY" };
            //curveColliderY.AddKeyFrame(0, 1);
            //curveColliderY.AddKeyFrame(.2f, 1);
            //curveColliderY.AddKeyFrame(.3f, .01f);
            //curveColliderY.AddKeyFrame(.7f, .01f);
            //curveColliderY.AddKeyFrame(.8f, 1);
            //curveColliderY.AddKeyFrame(1f, 1);
            //jumpState.Curves.Add(curveColliderY);

            //var curveGravity = new AnimationCurve { TargetParameter = "Gravity" };
            //curveGravity.AddKeyFrame(0, 1);
            //curveGravity.AddKeyFrame(.2f, 0f);
            //curveGravity.AddKeyFrame(.9f, 1f);
            //curveGravity.AddKeyFrame(1f, 1);
            //jumpState.Curves.Add(curveGravity);

            var blendTree = new AnimationBlendTree
            {
                Name = "Locomotion",
                ParameterA = "axisX",
                ParameterB = "axisY",
            };

            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = idleState, values = new Vector2(0, 0) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = walkState, values = new Vector2(0, 0.5f) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = runState, values = new Vector2(0, 1f) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = walkBackState, values = new Vector2(0, -.5f) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = jogBackState, values = new Vector2(0, -1f) });
            
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = strafeRightState, values = new Vector2(.5f, 0) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = strafeLeftState, values = new Vector2(-.5f, 0) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = strafeRightRunState, values = new Vector2(1f, 0) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = strafeLeftRunState, values = new Vector2(-1f, 0) });

          //  blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = jogFWLeftState, values = new Vector2(-1f, 1f) });
          //  blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = jogFWRightState, values = new Vector2(1f, 1f) });

            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = jogBackLeftState, values = new Vector2(-1f, -1f) });
            blendTree.Layers.Add(new AnimationBlendTree.BlendLayer { Animation = jogBackRightState, values = new Vector2(1f, -1f) });

            var toJump = new AnimationTransition
            {
                Source = blendTree,
                Target = jumpState,
                Conditions = {
                    new AnimationCondition {
                        Parameter = "jump",
                        Comparison = ComparisonType.Greater,
                        Value = 0
                    }
                }
            };

            var toFalling = new AnimationTransition
            {
                Source = jumpState,
                Target = fallingState,
                //Conditions = {
                //    new AnimationCondition {
                //        Parameter = "falling",
                //        Comparison = ComparisonType.Greater,
                //        Value = 0
                //    }
                //}
            };

            //var toLandingNow = new AnimationTransition
            //{
            //    Source = jumpState,
            //    Target = landingState,
            //};

            var toLanding = new AnimationTransition
            {
                Source = fallingState,
                Target = landingState,
                Conditions = {
                    new AnimationCondition {
                        Parameter = "landing",
                        Comparison = ComparisonType.Greater,
                        Value = 0
                    }
                }
            };

            var toBlendTree = new AnimationTransition
            {
                Source = landingState,
                Target = blendTree,
            };

            //var fromJump = new AnimationTransition
            //{
            //    Source = jumpState,
            //    Conditions = {
            //        new AnimationCondition {
            //            AutoReturn = true
            //        }
            //    }
            //};

            var layer = new AnimationLayer
            {
                Name = "Default",
                States = { blendTree, jumpState, fallingState, landingState },
                Transitions = { toJump, toFalling, toLanding, toBlendTree }
                //   Transitions = { toJump, fromJump }
            };


            var animation = new Animation
            {
                Layers = { layer },
                Name = "TestAnimation"
            };


            Animator animator = entity.AddComponent<Animator>();
            animator.Parameters.Add("axisX", 0);
            animator.Parameters.Add("axisY", 0);
            animator.Parameters.Add("jump", 0);
            animator.Parameters.Add("landing", 0);
            animator.Parameters.Add("falling", 0);
            animator.Parameters.Add("ColliderY", 1);
            animator.Parameters.Add("Gravity", 1);
            animator.Animation = animation;
        }

        public static Entity CreateAAAModel(AssetManager content)
        {
            //Mesh mesh = content.Load<Mesh>("Characters/Paladin/paladin_j_nordstrom.dae", ("Scale", .01f));
            //mesh.RootRotation = Matrix.RotationY(MathUtil.DegreesToRadians(180));

            //Texture normal = content.Load<Texture>("Characters/Paladin/textures/Paladin_normal.png");
            //Texture specular = content.Load<Texture>("DefaultSpecularMap.dds");
            //Texture diffuse = content.Load<Texture>("Characters/Paladin/textures/Paladin_diffuse.png");

            Mesh mesh = content.Load<Mesh>("Characters/Knight/knight_d_pelegrini.dae", ("Scale", .01f));
            mesh.RootRotation = Matrix.RotationY(MathUtil.DegreesToRadians(180));

            Texture normal = content.Load<Texture>("Characters/Knight/textures/Knight_normal.png");
            Texture specular = content.Load<Texture>("DefaultSpecularMap.dds");
            Texture diffuse = content.Load<Texture>("Characters/Knight/textures/Knight_diffuse.png");
            Texture aoc = content.Load<Texture>("white.dds");
            //normal = content.Load<Texture>("DefaultNormalMap.dds");
            //specular = content.Load<Texture>("Characters/Knight/textures/Knight_specular.png");

            Effect effect = new Effect("mesh_opaque");
            effect.SetValue("Albedo", diffuse);
            effect.SetValue("Normal", normal);
            effect.SetValue("Occlusion", aoc);
            effect.SetValue("Data", specular);
            effect.SetValue("Textures", new List<Texture>() { diffuse, normal, specular });

            Material material = new Material ( effect);
            material.Name = "PlayerMaterial";
            material.Technique = 2;

            var entity = new Entity("player");
            var renderer = entity.AddComponent<MeshRenderer>();
            renderer.Mesh = mesh;

            for (int i = 0; i < mesh.MeshParts.Count; i++)
                renderer.Materials.Add(material);

            //CreateMixamoAnimations(entity, mesh);
            CreateBlendTreeAnimations(entity);

            return entity;
        }

    }
}
