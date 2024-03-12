using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using SharpDX;
using Squid;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.IO;

namespace Spark.Client
{
    public class EditorTestScene
    {
        public EditorTestScene()
        {
            Engine.Settings.VSync = true;

            AssetManager content = new AssetManager(Engine.Device)
            {
                BaseDirectory = Engine.ResourceDirectory
            };


            var terrainLayers = new List<Terrain.TerrainSurface>
            {
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Sand_01/Sand_01_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Sand_01/Sand_01_Nor.png"),
                    Tiling = new Vector2(8, 8),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/RockWall_01/RockWall_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain Textures/RockWall_01/Rock_01_Nor.png"),
                    Tiling = new Vector2(7, 7),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_01_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_01_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Sand_Dirt/Sand_Dirt.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Sand_Dirt/Sand_Dirt_Nor.png"),
                    Tiling = new Vector2(20, 30),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Sandstone/Sandstone_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Sandstone/Sandstone_Nor.png"),
                    Tiling = new Vector2(10, 10),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Gras_01/Gras_01_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Gras_01/Gras_01_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Sand_02/Sand_02_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Sand_02/Sand_02_Nor.png"),
                    Tiling = new Vector2(10, 10),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Leafs/Leafs_01_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Leafs/Leafs_01_Nor.png"),
                    Tiling = new Vector2(3, 3),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/RockWall_01/RockWall_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/RockWall_01/Rock_01_Nor.png"),
                    Tiling = new Vector2(20, 15),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_02_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Dirt/Dirt_01_Nor.png"),
                    Tiling = new Vector2(15, 15),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Raw_Dirt/Raw_Dirt_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Raw_Dirt/Raw_Dirt_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/Moos/Moss_01_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/Moos/Moss_01_Nor.png"),
                    Tiling = new Vector2(4, 4),
                },
                new Terrain.TerrainSurface
                {
                    Diffuse =  content.Load<Texture>("Terrain/Terrain Textures/RockWall_01/RockWall_Dif.png"),
                    Normals =  content.Load<Texture>("Terrain/Terrain Textures/RockWall_01/Rock_01_Nor.png"),
                    Tiling = new Vector2(200, 200),
                },
            };

            Texture mergedSplatmap = content.Load<Texture>("Terrain/Control Textures/SplatMapMerged.dds");
            Texture indexMap = content.Load<Texture>("Terrain/Control Textures/SplatMapIndices.dds");

            //Texture tex1 = content.Load<Texture>("ps_texture_4k.png");
            Texture tex1 = content.Load<Texture>("DefaultColorMap.dds");
            Texture tex2 = content.Load<Texture>("DefaultNormalMap.dds");
            Texture tex3 = content.Load<Texture>("DefaultSpecularMap.dds");
            Texture reflection = content.Load<Texture>("SkyboxSet1/ThickCloudsWater/cubemap.dds");

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

            Effect skyboxEffect = new Effect("mesh_skybox");
            skyboxEffect.SetValue("texDiffuse", reflection);

            Material skyboxMaterial = new Material(skyboxEffect)
            {
                Name = "Skybox"
            };

            // main gamme camera
            var camera = new Entity("MainCamera",
                new Camera { FarPlane = 4096 });

            var editorTools = new Entity("pick", new EditorTools());
            editorTools.Flags |= EntityFlags.HideAndDontSave;

            //var gui = new Entity("gui", new GameUI());

            // skybox
            var skybox = new Entity("skybox",
                new SkyBoxRenderer
                {
                    Materials = { skyboxMaterial },
                    Mesh = Mesh.CreateBox(1)
                });

            // main light
            var lightcolor = Color.Orange;
            lightcolor = Color.Lerp(lightcolor, Color.White, .5f);

            var light = new Entity("light",
                new DirectionalLight { Color = lightcolor.ToColor3(), Intensity = 4 });
            light.Transform.Rotate(Vector3.UnitX, 45);
            light.Transform.RotateLocal(Vector3.UnitY, 150);
            light.Transform.Position = new Vector3(0, 10, 0);
            //light.AddComponent<LightRotator>();

            Effect terrEffect = new Effect("terrain");
            terrEffect.SetValue("Data", tex3);  
            terrEffect.SetValue("Splatmap", mergedSplatmap);
            terrEffect.SetValue("Indexmap", indexMap);
            terrEffect.SetValue("sampIndex", Samplers.ClampedPoint);

            Material terrainMaterial = new Material(terrEffect)
            {
                Name = "Terrain"
            };

            float mapSizeMultiplier = 1.0f;
            var mapsize = new Vector2(1700, 1700) * mapSizeMultiplier;
            var terrainHeight = 600 * mapSizeMultiplier;
            var heightmap = Engine.Assets.Load<Texture>("terrain.dds");
            var terrmap = new RenderTexture2D(heightmap.Description.Width, heightmap.Description.Height, SharpDX.DXGI.Format.R16_Float, false);

            Graphics.SetViewport(0, 0, heightmap.Description.Width, heightmap.Description.Height);
            Graphics.Blit(heightmap, terrmap.Target);

            heightmap = terrmap;
            var heightfield = heightmap.Resource.To16BitField(terrainHeight);

            var terrain = new Entity("terrain",
                new Terrain
                {
                    Material = terrainMaterial,
                    TerrainSize = mapsize,
                    Heightmap = heightmap,
                    HeightField = heightfield,
                    MaxHeight = terrainHeight,
                    Surfaces = terrainLayers
                },
                new RigidBody
                {
                    Type = ShapeType.Terrain,
                    IsStatic = true
                });


            terrain.Transform.Position = new Vector3(842.0983f - 842.0983f, 85.85109f - 39.8f, 841.4021f - 839.8109f);
            //terrain.Transform.Position = new Vector3(-mapsize.X / 2, 0, -mapsize.Y / 2);

            var water = new Entity("water", new MeshRenderer
            {
                Mesh = Mesh.CreatePlane(AxisAlignment.XZ),
                Materials = { waterMaterial }
            });

            water.Transform.Position = terrain.Transform.Position + new Vector3(mapsize.X / 2, 20, mapsize.Y / 2) + Vector3.Up * 3;
            water.Transform.Scale = new Vector3(mapsize.X, 1, mapsize.X);

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

            var decMaterial = new Material(decEffect)
            {
                Name = "Grass",
                UseInstancing = true
            };

            //var decorator = new Entity("Decorator");
            //decorator.Transform.Parent = terrain.Transform;
            //var dec = decorator.AddComponent<TerrainDecorator>();
            //dec.Terrain = terrain.GetComponent<Terrain>();
            //dec.RandomColor = new Vector2(.6f, .8f);
            //dec.BaseSize = new Vector2(.25f, .4f);
            //dec.RandomSize = new Vector2(2f, 2f);
            //dec.Material = decMaterial;

            var pos = new Vector3(0, 100, 0);
            pos.Y = terrain.GetComponent<Terrain>().GetHeight(pos) + 1;

            // editor camera
            var editorCamera = new Entity("EditorCamera",
                new Camera { FarPlane = 104096, IsEditor = true },
                new EditorCamera());
            editorCamera.Flags |= EntityFlags.HideAndDontSave;

            return;

            var center = pos;
            RNG.Push(98345);

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

            // Trees

            // NPCs
            float size = 20;
            for (int i = 0; i < 1; i++)
            {
                pos += new Vector3(RNG.RangeFloat(-size, size), 100, RNG.RangeFloat(-size, size));
                pos.Y = terrain.GetComponent<Terrain>().GetHeight(pos) + 1;
                SceneUtil.AddNPC(content, pos);
            }
        }
    }
}
