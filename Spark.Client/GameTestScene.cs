using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using SharpDX;
using Squid;
using SharpDX.Direct3D11;
using System.Diagnostics;

namespace Spark.Client
{

    public class JointComponent : Component
    {
        public Vector3 ConnectionPoint;
        public RigidBody BodyA;
        public RigidBody BodyB;
        private Jitter.Dynamics.Joints.HingeJoint joint;

        protected override void Awake()
        {
            var center = Vector3.Lerp(BodyA.Transform.Position, BodyB.Transform.Position, .5f);
            joint = new Jitter.Dynamics.Joints.HingeJoint(Physics.World, BodyA.JBody, BodyA.JBody, center, Vector3.ForwardLH);
           
        }
    }

    public class GameTestScene
    {
        public GameTestScene()
        {
            Engine.Settings.VSync = true;

            AssetManager assets = new AssetManager(Engine.Device);
            assets.BaseDirectory = Engine.ResourceDirectory;

            // var test = Shader.Create(new ShaderDescription2 { Elements = VertexComplex.InputElements, Filename = "gbuffer", VsEntry = "VS", PsEntry = "PS" });

            RNG.Push(123);
            Random rnd = new Random(99);

            var terrainLayers = new List<Terrain.TextureLayer>
            {
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Sand_01/Sand_01_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Sand_01/Sand_01_Nor.png"),
                    Tiling = new Vector2(8, 8),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/RockWall_01/RockWall_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain Textures/RockWall_01/Rock_01_Nor.png"),
                    Tiling = new Vector2(7, 7),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_01_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_01_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Sand_Dirt/Sand_Dirt.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Sand_Dirt/Sand_Dirt_Nor.png"),
                    Tiling = new Vector2(20, 30),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Sandstone/Sandstone_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Sandstone/Sandstone_Nor.png"),
                    Tiling = new Vector2(10, 10),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Gras_01/Gras_01_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Gras_01/Gras_01_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Sand_02/Sand_02_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Sand_02/Sand_02_Nor.png"),
                    Tiling = new Vector2(10, 10),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Leafs/Leafs_01_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Leafs/Leafs_01_Nor.png"),
                    Tiling = new Vector2(3, 3),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/RockWall_01/RockWall_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/RockWall_01/Rock_01_Nor.png"),
                    Tiling = new Vector2(20, 15),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_02_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_01_Nor.png"),
                    Tiling = new Vector2(15, 15),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Raw_Dirt/Raw_Dirt_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Raw_Dirt/Raw_Dirt_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/Moos/Moss_01_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/Moos/Moss_01_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TextureLayer
                {
                    Diffuse =  assets.Load<Texture>("Terrain/Terrain Textures/RockWall_01/RockWall_Dif.png"),
                    Normals =  assets.Load<Texture>("Terrain/Terrain Textures/RockWall_01/Rock_01_Nor.png"),
                    Tiling = new Vector2(200, 200),
                },
            };

            Texture mergedSplatmap = assets.Load<Texture>("Terrain/Control Textures/SplatMapMerged.dds");
            Texture indexMap = assets.Load<Texture>("Terrain/Control Textures/SplatMapIndices.dds");
            
            //Texture tex1 = content.Load<Texture>("ps_texture_4k.png");
            Texture tex1 = assets.Load<Texture>("DefaultColorMap.dds");
            Texture tex2 = assets.Load<Texture>("DefaultNormalMap.dds");
            Texture tex3 = assets.Load<Texture>("DefaultSpecularMap.dds");
            Texture reflection = assets.Load<Texture>("SkyboxSet1/ThickCloudsWater/cubemap.dds");

            Effect skyboxEffect = new Effect("mesh_skybox");
            skyboxEffect.SetValue("texDiffuse", reflection);

            Material skyboxMaterial = new Material(skyboxEffect);

            Mesh cube = Mesh.CreateBox(1);
            //Mesh cube = Engine.Content.Load<Mesh>("cube.obj");
            Mesh sphere = Mesh.CreateSphere(0.5f, 16, 16);

            //var pick = new Entity("pick", new EditorTools());
            var gui = new Entity("gui", new GameUI());

            // skybox
            var skybox = new Entity("skybox",
                new SkyBoxRenderer
                {
                    Materials = { skyboxMaterial },
                    Mesh = Mesh.CreateBox(1)
                });

            Effect waterEffect = new Effect("mesh_unlit");
            waterEffect.SetValue("Color", new Vector4(0, .5f, 1, .5f));
            waterEffect.SetValue("Albedo", tex1);
            waterEffect.SetValue("Normal", tex2);
            waterEffect.SetValue("Data", tex3);
            waterEffect.DepthStencilState = States.ZReadZWriteNoStencil;
            waterEffect.BlendState = States.BlendAlpha;
            waterEffect.RasterizerState = States.NoCull;

            Material waterMaterial = new Material(waterEffect)
            {
                Name = "Water"
            };

            // main light
            var lightcolor = Color.Orange;
            lightcolor = Color.Lerp(lightcolor, Color.White, .5f);

            var light = new Entity("light",
                new DirectionalLight { Color = lightcolor.ToColor3(), Intensity = 4f });
            light.Transform.Rotate(Vector3.UnitX, 45);
            light.Transform.RotateLocal(Vector3.UnitY, 150);
            light.Transform.Position = new Vector3(0, 10, 0);

            Effect terrEffect = new Effect("terrain");
            terrEffect.SetValue("Data", tex3);
            terrEffect.SetValue("Splatmap", mergedSplatmap);
            terrEffect.SetValue("Indexmap", indexMap);
            terrEffect.SetValue("sampIndex", Samplers.ClampedPoint);

            Material terrainMaterial = new Material(terrEffect);

            float mapSizeMultiplier = 1.0f;
            var mapsize = new Vector2(1700, 1700) * mapSizeMultiplier;
            var terrainHeight = 600 * mapSizeMultiplier;
            var heightmap = Engine.Assets.Load<Texture>("terrain.dds");
            var heightfield = heightmap.Resource.To16BitField(terrainHeight);

            var terrain = new Entity("terrain",
                new Terrain
                {
                    Material = terrainMaterial,
                    TerrainSize = mapsize,
                    Heightmap = heightmap,
                    HeightField = heightfield,
                    MaxHeight = terrainHeight,
                    Layers = terrainLayers
                },
                new RigidBody
                {
                    Type = ShapeType.Terrain,
                    IsStatic = true
                });

            terrain.Transform.Position = new Vector3(842.0983f - 842.0983f, 85.85109f - 39.8f, 841.4021f - 839.8109f);
            //terrain.Transform.Position = new Vector3(-mapsize.X / 2, 0, -mapsize.Y / 2);

            //var water = new Entity("water", new MeshRenderer
            //{
            //    Mesh = Mesh.CreatePatch(),
            //    Materials = { waterMaterial }
            //});

            //water.Transform.Position = terrain.Transform.Position + new Vector3(mapsize.X / 2, 0, mapsize.Y / 2) + Vector3.Up * 3;
            //water.Transform.Scale = new Vector3(mapsize.X * 10, 1, mapsize.Y * 10);
            //var waterpos = water.Transform.Position;
            //waterpos.Y = 63.5f;
            //water.Transform.Position = waterpos;


            Engine.Settings.GlobalWater = true;

            // var grassTex = content.Load<Texture>("Textures/billboardgrass0002.png");
            var grassTex = assets.Load<Texture>("Textures/grass_atlas_2x2.dds");

            var decEffect = new Effect("mesh_grass");
            decEffect.SetValue("Albedo", grassTex);
            decEffect.SetValue("Normal", tex2);
            decEffect.SetValue("Data", tex3);
            decEffect.SetValue("Height", heightmap);
            decEffect.SetValue("sampData", Samplers.ClampedAnisotropic);
            decEffect.SetValue("sampHeight", Samplers.ClampedBilinear2D);
            decEffect.BlendState = States.BlendNone;
            decEffect.DepthStencilState = States.ZReadZWriteNoStencil;

            var decMaterial = new Material(decEffect);
            decMaterial.UseInstancing = true;

            var decorator = new Entity("Decorator");
            decorator.Transform.Parent = terrain.Transform;
            var dec = decorator.AddComponent<TerrainDecorator>();
            dec.Terrain = terrain.GetComponent<Terrain>();
            dec.RandomColor = new Vector2(.3f, .5f);
            dec.BaseSize = new Vector2(.25f, .4f) * 1.5f;
            dec.RandomSize = new Vector2(1f, 2f);
            dec.Material = decMaterial;
            dec.CountPerCell *= 5;

            var pos = new Vector3(1167, 64, 830);
           // var pos = new Vector3(999, 100, 775);
           // var pos = new Vector3(mapsize.X / 2, 100, mapsize.Y / 2);
            pos.Y = terrain.GetComponent<Terrain>().GetHeight(pos) + 1;

            var player = SceneUtil.AddPlayer(assets, pos);
            player.AddComponent<CharacterController>().Height = 1.3f;
            player.AddComponent(new PhysicsVicinity { Range = 8 });

            var plight = new Entity("plight",
                new PointLight { Color = lightcolor.ToColor3(), Intensity = 2f });
            plight.Transform.Parent = player.Transform;
            plight.Transform.Position = new Vector3(0, 2.5f, 0);

            var camera = new Entity("camera",
                new Camera { FarPlane = 2048 * 8 },
                new WowCamera { Target = player });
            //camera.Transform.Position = new Vector3(0, 1, -2);

            var loader = new SceneLoader();
            loader.Load(@"D:\Dump\ExportedScene.json");
            //Engine.Settings.GlobalWater = false;
        }
    }

    public class GameTestScene2
    {
        public GameTestScene2()
        {
            Engine.Settings.VSync = true;

            AssetManager content = new AssetManager(Engine.Device);
            content.BaseDirectory = Engine.ResourceDirectory;

            // var test = Shader.Create(new ShaderDescription2 { Elements = VertexComplex.InputElements, Filename = "gbuffer", VsEntry = "VS", PsEntry = "PS" });

            RNG.Push(123);
            Random rnd = new Random(99);

            //var test = new Effect2("shadertest");

            //Texture splat1 = content.Load<Texture>("Textures/grass01.jpg");
            //Texture splat2 = content.Load<Texture>("Textures/rock01.jpg");
            //Texture splat3 = content.Load<Texture>("Textures/dirt01.jpg");

            Texture splat1 = content.Load<Texture>("Terrain Textures/grass/grassgreen_01.dds");
            Texture splat2 = content.Load<Texture>("Terrain Textures/rocks/limestonecliff_d.dds");
            Texture splat3 = content.Load<Texture>("Terrain Textures/dirt/dirtgreen_01.dds");
            Texture splat4 = content.Load<Texture>("Terrain Textures/mud/seymaixa_2K_Albedo.dds");

            Texture norm1 = content.Load<Texture>("Terrain Textures/grass/grassgreen_01_n.dds");
            Texture norm2 = content.Load<Texture>("Terrain Textures/rocks/limestonecliff_n.dds");
            Texture norm3 = content.Load<Texture>("Terrain Textures/dirt/dirtgreen_01_n.dds");
            Texture norm4 = content.Load<Texture>("Terrain Textures/mud/seymaixa_2K_n.dds");

            var textureArray = Graphics.CreateTexture2DArray(1024, 1024, new List<Texture> { splat1, splat2, splat3, splat4 });
            var normalArray = Graphics.CreateTexture2DArray(1024, 1024, new List<Texture> { norm1, norm2, norm3, norm4 });

            //Texture tex1 = content.Load<Texture>("ps_texture_4k.png");
            Texture tex1 = content.Load<Texture>("DefaultColorMap.dds");
            Texture tex2 = content.Load<Texture>("DefaultNormalMap.dds");
            Texture tex3 = content.Load<Texture>("DefaultSpecularMap.dds");
            Texture reflection = content.Load<Texture>("SkyboxSet1/ThickCloudsWater/cubemap.dds");

            Effect skyboxEffect = new Effect("mesh_skybox");
            skyboxEffect.SetValue("texDiffuse", reflection);

            Material skyboxMaterial = new Material (skyboxEffect);

            Mesh cube = Mesh.CreateBox(1);
            //Mesh cube = Engine.Content.Load<Mesh>("cube.obj");
            Mesh sphere = Mesh.CreateSphere(0.5f, 16, 16);

            //var pick = new Entity("pick", new EditorTools());
            var gui = new Entity("gui", new GameUI());

            // skybox
            var skybox = new Entity("skybox",
                new SkyBoxRenderer
                {
                    Materials = { skyboxMaterial },
                    Mesh = Mesh.CreateBox(1)
                });

            //Effect waterEffect = new Effect("mesh_unlit", RenderPass.Transparent);
            //waterEffect.SetValue("Color", new Vector4(0, .5f, 1, .5f));
            //waterEffect.SetValue("Albedo", tex1);
            //waterEffect.SetValue("Normal", tex2);
            //waterEffect.SetValue("Data", tex3);
            //Material waterMaterial = new Material { waterEffect };

            //var water = new Entity("water", new MeshRenderer { Mesh = Mesh.CreatePlane(AxisAlignment.XZ), Materials = { waterMaterial } });
            //water.Transform.Position = new Vector3(0, 12f, 0);
            //water.Transform.Scale = new Vector3(4096 * 2, 1, 4096 * 2);


            // main light
            var lightcolor = Color.Orange;
            lightcolor = Color.Lerp(lightcolor, Color.White, .5f);

            var light = new Entity("light",
                new DirectionalLight { Color = lightcolor.ToColor3(), Intensity = 2f });
            light.Transform.Rotate(Vector3.UnitX, 45);
            light.Transform.RotateLocal(Vector3.UnitY, 150);
            light.Transform.Position = new Vector3(0, 10, 0);
          
            //light.AddComponent<LightRotator>();

            //var light2 = new Entity("light",
            //    new DirectionalLight { Color = Color.Blue.ToColor3(), Intensity = 2 });
            //light2.Transform.Rotate(Vector3.UnitX, 45);
            //light2.Transform.Rotate(Vector3.UnitY, -45);
            //light2.Transform.Position = new Vector3(0, 10, 0);

            Effect terrEffect = new Effect("terrain");
            terrEffect.SetValue("Data", tex3);
            terrEffect.SetValue("SplatDiffuse", textureArray);
            terrEffect.SetValue("SplatNormal", normalArray);

            Material terrainMaterial = new Material ( terrEffect );

            //var heightmap = Engine.Content.Load<Texture>("ps_height_4k.dds");
            //var heightmap = Engine.Content.Load<Texture>("island_height.dds");
            float mapSizeMultiplier = 1.5f;
            var terrainHeight = 400 * mapSizeMultiplier;
            var heightmap = Engine.Assets.Load<Texture>("perlin_billowy_1.dds");
            var heightfield = heightmap.Resource.To16BitField(terrainHeight);

            var mapsize = new Vector2(4096, 4096) * mapSizeMultiplier;
            var terrain = new Entity("terrain",
                new Terrain
                {
                    Material = terrainMaterial,
                    TerrainSize = mapsize,
                    Heightmap = heightmap,
                    HeightField = heightfield,
                    MaxHeight = terrainHeight,
                },
                new RigidBody
                {
                    Type = ShapeType.Terrain,
                    IsStatic = true
                });;
            terrain.Transform.Position = new Vector3(-mapsize.X / 2, 0, -mapsize.Y / 2);

            // var grassTex = content.Load<Texture>("Textures/billboardgrass0002.png");
            var grassTex = content.Load<Texture>("Textures/grass_atlas_2x2.dds");

            var decEffect = new Effect("mesh_grass");
            decEffect.SetValue("Albedo", grassTex);
            decEffect.SetValue("Normal", tex2);
            decEffect.SetValue("Data", tex3);
            decEffect.SetValue("Height", heightmap);
            decEffect.SetValue("sampData", Samplers.ClampedAnisotropic);
            decEffect.SetValue("sampHeight", Samplers.ClampedBilinear2D);
            decEffect.BlendState = States.BlendNone;
            decEffect.DepthStencilState = States.ZReadZWriteNoStencil;

            var decMaterial = new Material(decEffect);
            decMaterial.UseInstancing = true;

            var decorator = new Entity("Decorator");
            decorator.Transform.Parent = terrain.Transform;
            var dec = decorator.AddComponent<TerrainDecorator>();
            dec.Terrain = terrain.GetComponent<Terrain>();
            dec.RandomColor = new Vector2(.6f, .8f);
            dec.BaseSize = new Vector2(.25f, .4f) * 1.5f;
            dec.RandomSize = new Vector2(1f, 2f);
            dec.Material = decMaterial;
            dec.CountPerCell *= 5;

            var pos = new Vector3(0, 100, 0);
            pos.Y = terrain.GetComponent<Terrain>().GetHeight(pos) + 1;

            var player = SceneUtil.AddPlayer(content, pos);
            player.AddComponent<CharacterController>().Height = 1.3f;
            player.AddComponent(new PhysicsVicinity { Range = 8 });

            var plight = new Entity("plight",
                new PointLight { Color = lightcolor.ToColor3(), Intensity = 2f });
            plight.Transform.Parent = player.Transform;
            plight.Transform.Position = new Vector3(0, 2.5f, 0);

            var camera = new Entity("camera", 
                new Camera { FarPlane = 2048 * 8 }, 
                new WowCamera { Target = player });
            //camera.Transform.Position = new Vector3(0, 1, -2);

            return;


            var center = pos;// Vector3.Zero;

            RNG.Push(98345);

            Stopwatch clock = new Stopwatch();


            var collmesh = content.Load<Mesh>("Trees/Beech/beech-coll.obj");

            var trunkmat = SceneUtil.CreateMaterial("Forest/log.dds", "DefaultNormalMap.dds", "DefaultSpecularMap.dds", true);
            var leafmat = SceneUtil.CreateMaterial("Forest/Leaves2.dds", "DefaultNormalMap.dds", "DefaultSpecularMap.dds", true);
            //var leafmat2 = SceneUtil.CreateMaterial("Forest/Leaves2.dds", "DefaultNormalMap.dds", "DefaultSpecularMap.dds", true);

            leafmat.Effect.RasterizerState = States.NoCull;

            var tree01 = content.Load<Mesh>("Forest/Tree01.obj");
            var tree01lod = new List<LODMesh>
            {
                new LODMesh
                { 
                    Mesh = tree01,
                    Materials = { leafmat, trunkmat },
                    Range = 1024
                }
            };

            var tree03 = content.Load<Mesh>("Forest/Tree03.obj");
            var tree03lod = new List<LODMesh> { new LODMesh { Mesh = tree03, Materials = { trunkmat, leafmat }, Range = 1024 } };

            //var rock01mat = SceneUtil.CreateMaterial("Forest/rock01.dds", "DefaultNormalMap.dds", "DefaultSpecularMap.dds");
            //var rock01 = content.Load<Mesh>("Forest/rock01.obj");
            //var rock01lod = new List<LODMesh> { new LODMesh { Mesh = rock01, Materials = { rock01mat }, Range = 256 } };

            var treeMaterial = SceneUtil.CreateMaterial("Trees/Beech/beech.dds", "Trees/Beech/beech_0_n.dds", "DefaultSpecularMap.dds", true);
            var beech0 = content.Load<Mesh>("Trees/Beech/beech_0.obj");
            var beech1 = content.Load<Mesh>("Trees/Beech/beech_1.obj");
            var beech2 = content.Load<Mesh>("Trees/Beech/beech_2.obj");
            var beechlod = new List<LODMesh>
            {
                new LODMesh { Mesh = beech0, Materials = { treeMaterial }, Range = 48 },
                new LODMesh { Mesh = beech1, Materials = { treeMaterial }, Range = 96 },
                new LODMesh { Mesh = beech2, Materials = { treeMaterial }, Range = 384 }
            };


            var birch = SceneUtil.GetTreeLOD("birch");
            var fir = SceneUtil.GetTreeLOD("fir");

            //var rock01 = SceneUtil.CreateLod("Rock_01");
            //var rock02 = SceneUtil.CreateLod("Rock_02");
            var rock03 = SceneUtil.CreateLod("Rock_03");
            var rock04 = SceneUtil.CreateLod("Rock_04");
            //var rock05 = SceneUtil.CreateLod("Rock_05");
            //var rock06 = SceneUtil.CreateLod("Rock_06");

            float range = mapsize.X / 2;
            var terr = terrain.GetComponent<Terrain>();

            // Trees
            SceneUtil.AddModel(fir, (int)(range * 6 * mapSizeMultiplier), center, .15f, .5f, range, terr, 512);
            SceneUtil.AddModel(birch, (int)(range * 6 * mapSizeMultiplier), center, .15f, .5f, range, terr, 512);
            //SceneUtil.AddModel("Trees/Beech/beech", range * 8, center, 8f, 14f, range, terrain.GetComponent<Terrain>(), 512);

            // Rocks
            //SceneUtil.AddModel(rock02, range * 2, center, .25f, 2f, range, terr, 512, true);
            SceneUtil.AddModel(rock03, (int)(range * 2 * mapSizeMultiplier), center, 2f, 6f, range, terr, 512, true);
            SceneUtil.AddModel(rock04, (int)(range * 2 * mapSizeMultiplier), center, 2f, 4f, range, terr, 512, true);
            //SceneUtil.AddModel(rock05, range * 2, center, 2f, 4f, range, terr, 512, true);
            //SceneUtil.AddModel(rock06, range * 2, center, 2f, 4f, range, terr, 512, true);

            return;

            // NPCs
            float size = 10;
            for (int i = 0; i < 10; i++)
            {
                pos = pos + new Vector3(rnd.NextFloat(-size, size), 100, rnd.NextFloat(-size, size));
                pos.Y = terrain.GetComponent<Terrain>().GetHeight(pos) + 1;
                var npc = SceneUtil.AddNPC(content, pos);
                npc.AddComponent<NPCController>().Height = 1f;
                npc.AddComponent(new PhysicsVicinity { Range = 8 });
            }
        }
    }
}
