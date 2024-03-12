using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using SharpDX;
using Squid;
using SharpDX.Direct3D11;

namespace Spark.Client
{
    public class AnimTest
    {
        public AnimTest()
        {
            Random rnd = new Random(99);

            Texture axeTexture = Engine.Assets.Load<Texture>("axe.jpg");
            Texture dwarfTexture = Engine.Assets.Load<Texture>("dwarf.jpg");
            Texture reflection = Engine.Assets.Load<Texture>("space_skybox_02.dds");
            Texture metal1 = Engine.Assets.Load<Texture>("metal_plates_01_cm.dds");
            Texture metal2 = Engine.Assets.Load<Texture>("DefaultNormalMap.dds");
            Texture metal3 = Engine.Assets.Load<Texture>("metal_plates_01_dm.dds");

            #region meta data test
            //MetaData meta = new MetaData();
            //meta.Set("Ranges", new List<AnimationRange>
            //{
            //    new AnimationRange{ Name = "walk", Start = 1, End = 13 },
            //    new AnimationRange{ Name = "run", Start = 15, End = 25 },
            //    new AnimationRange{ Name = "jump", Start = 27, End = 39 },
            //    new AnimationRange{ Name = "jump2", Start = 41, End = 53 },

            //    new AnimationRange{ Name = "crouchStart", Start = 55, End = 58 },
            //    new AnimationRange{ Name = "crouchLoop", Start = 59, End = 68 },
            //    new AnimationRange{ Name = "crouchEnd", Start = 69, End = 73 },

            //    new AnimationRange{ Name = "ready1", Start = 74, End = 87 },
            //    new AnimationRange{ Name = "ready2", Start = 89, End = 109 },
            
            //    new AnimationRange{ Name = "Attack1", Start = 111, End = 125 },
            //    new AnimationRange{ Name = "Attack2", Start = 127, End = 141 },
            //    new AnimationRange{ Name = "Attack3", Start = 143, End = 159 },
            //    new AnimationRange{ Name = "Attack4", Start = 161, End = 179 },
            //    new AnimationRange{ Name = "Attack5", Start = 181, End = 191 },
            //    new AnimationRange{ Name = "Block", Start = 193, End = 209 },
            
            //    new AnimationRange{ Name = "Die1", Start = 211, End = 226 },
            //    new AnimationRange{ Name = "Die2", Start = 229, End = 251 },
            //    new AnimationRange{ Name = "Nod", Start = 252, End = 271 },
            //    new AnimationRange{ Name = "ShakeHead", Start = 273, End = 289 },
            //    new AnimationRange{ Name = "Idle", Start = 291, End = 324 },
            //    new AnimationRange{ Name = "Idle", Start = 326, End = 359 },
            //});
            #endregion

            Mesh mesh = Engine.Assets.Load<Mesh>("dwarf1.x");//, meta);

            Effect metalEffect = new Effect("mesh_opaque");
            metalEffect.SetValue("Textures", new List<Texture>() { metal1, metal2, metal3 });

            Effect dwarfEffect = new Effect("mesh_opaque");
            dwarfEffect.SetValue("Textures", new List<Texture>() { dwarfTexture, metal2, metal3 });

            Effect axeEffect = new Effect("mesh_opaque");
            axeEffect.SetValue("Textures", new List<Texture>() { axeTexture, metal2, metal3 });

            Material dwarfMaterial = new Material(dwarfEffect);
            Material axeMaterial = new Material(axeEffect);
            axeMaterial.Technique = 2;

            Effect skyboxEffect = new Effect("mesh_skybox");
            skyboxEffect.SetValue("texDiffuse", reflection);
            skyboxEffect.RasterizerState = States.FrontCull;

            Material metalMaterial = new Material(metalEffect);

            Material skyboxmaterial = new Material(skyboxEffect);

            var skybox = new Entity("skybox",
            new SkyBoxRenderer { Materials = { skyboxmaterial }, Mesh = Mesh.CreateBox(1) });


            Entity pick = new Entity("pick");
            pick.AddComponent<EditorTools>();

            // gui
            var gui = new Entity("gui");
            gui.AddComponent<GameUI>();

            Entity camera = new Entity("camera", new Camera { });
            //camera.AddComponent<FreeCamera>();
            camera.Transform.Position = new Vector3(0, 5, -10);

            // main light
            Entity light = new Entity("light");
            light.AddComponent<DirectionalLight>();
            light.Transform.Rotate(Vector3.UnitX, 45);

            // model
            Entity dwarf = new Entity("dwarf");
            dwarf.Transform.Rotate(Vector3.UnitY, 180);
            dwarf.Transform.Scale = Vector3.One * 1;

            MeshRenderer boxrenderer = dwarf.AddComponent<MeshRenderer>();
            boxrenderer.Materials.Add(axeMaterial);
            boxrenderer.Materials.Add(dwarfMaterial);
            boxrenderer.Mesh = mesh;

            //AnimationClip walk = mesh.Animations[0].CreateRange("walk", 1, 13);
            //AnimationClip run = mesh.Animations[0].CreateRange("run", 15, 25);
            //mesh.Animations = new AnimationClip[1] { run };
        
            //var terrain = new Entity("terrain");
            //terrain.AddComponent<TerrainRenderer>().Materials.Add(metalMaterial);
            //terrain.Transform.Position = new Vector3(-64, 0, -64);
            //Add(terrain);
        }   
    }
}
