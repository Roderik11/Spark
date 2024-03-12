using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using SharpDX;
using SharpDX.Direct3D11;

namespace Spark.Editor
{
    public class EditorScene
    {
        public EditorScene()
        {
            Random rnd = new Random(99);

            Texture tex1 = Engine.Assets.Load<Texture>("DefaultColorMap.dds");
            Texture tex2 = Engine.Assets.Load<Texture>("DefaultNormalMap.dds");
            Texture tex3 = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");
            Texture reflection = Engine.Assets.Load<Texture>("space_skybox_02.dds");

            Effect effect1 = new Effect("mesh_opaque");
            effect1.SetValue("Albedo", tex1);
            effect1.SetValue("Normal", tex2);
            effect1.SetValue("Data", tex3);

            Material material1 = new Material(effect1);

            Effect effect3 = new Effect("mesh_skybox");
            effect3.SetValue("texDiffuse", reflection);
            effect3.RasterizerState = States.FrontCull;

            Material material2 = new Material(effect3);

            Mesh cube = Mesh.CreateBox(1);
            //Mesh cube = Engine.Content.Load<Mesh>("cube.obj");
            Mesh sphere = Mesh.CreateSphere(0.5f, 16, 16);

            Entity pick = new Entity("pick");
            pick.AddComponent<JointPick>();

            //// gui
            //var gui = new Entity("gui");
            //gui.AddComponent<GameGui>();
            //Add(gui);

            //var camera = new Entity("camera", new Camera { Active = true });
            //camera.AddComponent<FreeCamera>();
            //camera.Transform.Position = new Vector3(0, 5, -10);
            //camera.Tag = 0;

            // skybox
            Entity skybox = new Entity("skybox");
            SkyBoxRenderer renderer = skybox.AddComponent<SkyBoxRenderer>();
            renderer.Mesh = Mesh.CreateBox(1);
            renderer.Materials.Add(material2);

            // main light
            Entity light = new Entity("light");
            light.AddComponent<DirectionalLight>();
            light.Transform.Rotate(Vector3.UnitX, 45);

            // floor
            //var terrain = new Entity("terrain");
            //terrain.AddComponent<TerrainRenderer>().Materials.Add(material1);
            //terrain.GetComponent<TerrainRenderer>().CreateDefault();
            //terrain.AddComponent<RigidBody>().Type = ShapeType.Terrain;
            //terrain.GetComponent<RigidBody>().IsStatic = true;
            //terrain.Transform.Position = new Vector3(-64, 0, -64);

            for (int i = 0; i < 64; i++)
            {
                var box = new Entity("box",
                    new MeshRenderer { Materials = { material1 }, Mesh = cube },
                    new RigidBody { Type = ShapeType.Box, Mesh = cube, IsStatic = true });
                box.Transform.Position = new Vector3(rnd.Next(-64, 64), 2, rnd.Next(-64, 64));
                box.Transform.Scale = new Vector3(6, 6, 6);
            }

            for (int i = 0; i < 10; i++)
            {
                var box = new Entity("box",
                    new MeshRenderer { Materials = { material1 }, Mesh = cube },
                    new RigidBody { Type = ShapeType.Box, Mesh = cube });
                box.Transform.Position = new Vector3(4, 2 + i * 4, 0);
            }
        }
    }
}
