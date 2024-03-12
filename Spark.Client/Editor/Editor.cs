﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Windows;
using Squid;
using YamlDotNet;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Spark.Client
{
    public class EditorApp
    {
        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        private RenderView viewport;
        private RenderForm Form;
        private EditorUI editorUI;

        public void Run()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var color = System.Drawing.ColorTranslator.FromHtml("#222222");
           
            Form = new SharpDX.Windows.RenderForm
            {
                BackColor = color,
                WindowState = FormWindowState.Minimized,
                Size = new System.Drawing.Size(1680, 1020),
                StartPosition = FormStartPosition.CenterScreen,
                Text = "Ember"
            };

            Form.SizeChanged += Form_SizeChanged;
            Form.HandleCreated += Form_HandleCreated;
            Form.Show();
            //System.Windows.Forms.Cursor.Hide();

            Thread.Sleep(1000);

            Reflector.RegisterAssemblies(System.Reflection.Assembly.GetAssembly(typeof(EditorApp)));
            Engine.Initialize("D:\\Projects\\2024\\ProjectXYZ\\", true);

            viewport = new RenderView(Form.Handle)
            {
                OnRender = RenderGui
            };

            RenderView.OnViewsChanged += RenderView_OnViewsChanged;

            InitializeEditor();
     
            Form.WindowState = FormWindowState.Normal;

            Engine.Run(Form);
        }

        private void InitializeEditor()
        {
            Gui.Renderer = new RendererSlimDX();
            new EditorTestScene();
            editorUI = new EditorUI();
            MessageDispatcher.Send(Msg.RefreshExplorer);
        }

        private void RenderView_OnViewsChanged()
        {
            RenderView.All.Remove(viewport);
            RenderView.All.Add(viewport);
        }

        private void Form_HandleCreated(object sender, EventArgs e)
        {
            if (DwmSetWindowAttribute(Form.Handle, 19, new[] { 1 }, 4) != 0)
                DwmSetWindowAttribute(Form.Handle, 20, new[] { 1 }, 4);
        }

        void RenderGui(RenderView view)
        {
            Graphics.SetTargets(viewport.BackBufferTarget);
            Graphics.SetViewport(new ViewportF(0, 0, viewport.Size.X, viewport.Size.Y, 0.0f, 1.0f));
            Graphics.SetScissorRectangle(0, 0, (int)viewport.Size.X, (int)viewport.Size.Y);
            Graphics.ClearRenderTargetView(viewport.BackBufferTarget, new Color4(0, 0, 0, 1));

            viewport.Prepare();
            editorUI.Update();
            editorUI.Draw();
            viewport.Present();
        }

        void Form_SizeChanged(object sender, EventArgs e)
        {
            if (Form.WindowState == FormWindowState.Minimized) return;
            viewport?.Resize();
        }
    }

    public static class Editor
    {
        public class SerializedEntity
        {
            public Entity Entity { get; set; }
            public List<Component> Components { get; set; } = new List<Component>();
        }

        public static void SaveScene(string path)
        {
            path = @"D:\Dump\testjson.json";

            JSON json = new JSON();
            List<JSON> entities = new List<JSON>();

            foreach (var entity in Entity.Entities)
            {
                if (!entity.Flags.HasFlag(EntityFlags.HideAndDontSave))
                    entities.Add(entity.ToJSON());
            }

            json["entities"] = entities.ToArray();

            string text = json.ToText();
            System.IO.File.WriteAllText(path, text);
        }

        static void ClearScene()
        {
            var entities = new List<Entity>(Entity.Entities);
            foreach (var entity in entities)
            {
                if (entity.Flags.HasFlag(EntityFlags.DontDestroy))
                    continue;

                entity.Destroy();
            }
        }

        public static void MergeSplatMaps()
        {
            var map0 = Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_0.dds");
            var map1 = Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_1.dds");
            var map2 = Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_2.dds");
            var map3 = Engine.Assets.Load<Texture>("Terrain/Terrain_splatmap_3.dds");

            var size = new Int3(map0.Description.Width, map0.Description.Height, 0);

            var surface0 = new RenderTexture2D(size.X, size.Y, SharpDX.DXGI.Format.R8G8B8A8_UNorm, false, true);
            var surface1 = new RenderTexture2D(size.X, size.Y, SharpDX.DXGI.Format.R8G8B8A8_UNorm, false, true);
            var shader = new ComputeShader2("Shaders/splatmerge.fx");

            shader.SetParameter("texture0", map0.View);
            shader.SetParameter("texture1", map1.View);
            shader.SetParameter("texture2", map2.View);
            shader.SetParameter("texture3", map3.View);
            shader.SetParameter("linearSampler", Samplers.ClampedBilinear2D);
            shader.SetParameter("mergedOutput", surface0.UnorderedAccess);
            shader.SetParameter("mapIndexOutput", surface1.UnorderedAccess);

            shader.Dispatch(size.X / 8, size.X / 8, 1);

            surface0.Save("D:\\Projects\\2022\\Spark\\ProjectXYZ\\Resources\\Terrain\\Control Textures\\SplatMapMerged.dds");
            surface1.Save("D:\\Projects\\2022\\Spark\\ProjectXYZ\\Resources\\Terrain\\Control Textures\\SplatMapIndices.dds");
        }

        public static void LoadScene(string path)
        {
            var loader = new SceneLoader();
            loader.Load(@"D:\Dump\ExportedScene.json");
            MessageDispatcher.Send(Msg.RefreshExplorer);

            return;

            path = "G:\\testjson.json";
            ClearScene();
           // return;

            string text = System.IO.File.ReadAllText(path);
            JSON json = new JSON(text);
            JSON[] entities = json.ToArray<JSON>("entities");

            foreach (var e in entities)
            {
                if (JSONSerializer.Deserialize(e) is Entity entity)
                    entity.FromJSON(e);
            }

            JSONSerializer.ResolveReferences();
        }

        public static void TextureRoundtripTest()
        {
            // LESSON LEARNED: DDS is already the best way to package textures
            // Use DXTex instead

            var process = new Process();
            var info = new ProcessStartInfo("CMD.exe");
            info.Arguments = $"/C texconv -f DXT5 -r {AssetDatabase.Assets}*.png -o {AssetDatabase.Packaged}";
            process.StartInfo = info;
            process.Exited += (a, b) =>
            {

            };
           // var process = Process.Start("CMD.exe",$"/C texconv -f DXT5 -r {AssetDatabase.Assets}*.png -o {AssetDatabase.Packaged}");

            //Stopwatch clock = new Stopwatch();
            //clock.Reset();
            //clock.Start();
            //var texture = Engine.Assets.Load<Texture>("Buildings/Wall_01/Textures/Wall_01_Dif.png");
            //clock.Stop();
            //Debug.Log($"Load: {clock.Elapsed.TotalMilliseconds}");

            //clock.Reset();
            //clock.Start();
            //var packer = new TexturePacker();
            //FileStream filestream = File.Create(@"D:\Dump\CustomTexture.stex");
            //BinaryWriter writer = new BinaryWriter(filestream);
            //packer.Pack(writer, texture);
            //filestream.Close();
            //clock.Stop();
            //Debug.Log($"Pack: {clock.Elapsed.TotalMilliseconds}");

            //clock.Reset();
            //clock.Start();
            //filestream = File.OpenRead(@"D:\Dump\CustomTexture.stex");
            //BinaryReader reader = new BinaryReader(filestream);
            //var loaded = packer.Unpack(reader);
            //clock.Stop();
            //Debug.Log($"Unpack: {clock.Elapsed.TotalMilliseconds}");

            //return;

            //Stopwatch clock = new Stopwatch();
            //clock.Reset();
            //clock.Start();
            //var mesh = Engine.Assets.Load<Mesh>("Buildings/Castle_Tower/Castle_Tower.fbx");
            //clock.Stop();
            //Debug.Log($"Load: {clock.Elapsed.TotalMilliseconds}");

            //clock.Reset();
            //clock.Start();
            //var packer = new MeshPacker();
            //FileStream filestream = File.Create(@"D:\Dump\Castle_Tower.smesh");
            //BinaryWriter writer = new BinaryWriter(filestream);
            //packer.Pack(writer, mesh);
            //filestream.Close();
            //clock.Stop();
            //Debug.Log($"Pack: {clock.Elapsed.TotalMilliseconds}");

            //clock.Reset();
            //clock.Start();
            //filestream = File.OpenRead(@"D:\Dump\Castle_Tower.smesh");
            //BinaryReader reader = new BinaryReader(filestream);
            //var loaded = packer.Unpack(reader);
            //clock.Stop();
            //Debug.Log($"Unpack: {clock.Elapsed.TotalMilliseconds}");
        }
    }
}
