using System;
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


    public class SceneLoader
    {
        private Dictionary<string, StaticMesh> staticMeshLookup = new Dictionary<string, StaticMesh>();
        private Dictionary<string, string> meshLookup = new Dictionary<string, string>();

        public class SceneExport
        {
            public List<SceneObject> Objects = new List<SceneObject>();
            public List<PointLight> Lights = new List<PointLight>();
            public List<SceneObject> Trees = new List<SceneObject>();
        }

        [Serializable]
        public class SceneObject
        {
            public string PrefabName;
            public string Position;
            public string Rotation;
            public string Scale;
        }

        [Serializable]
        public class PointLight
        {
            public string PrefabName;
            public string Position;
            public string Rotation;
            public string Scale;
            public string Color;
            public float Range;
            public float Intensity;
        }

        public SceneLoader()
        {
            var root = new DirectoryInfo(Engine.ResourceDirectory);
            var extensions = AssetManager.GetAllExtensions().ToArray();

            var files = root.GetFilesFiltered(".fbx");
            var relativePath = new HashSet<string>(files.Select(fi => fi.FullName.Replace(root.FullName, string.Empty)));

            foreach (var path in relativePath)
            {
                var fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (!meshLookup.ContainsKey(fileName))
                    meshLookup.Add(fileName, path);
            }
        }

        private class TimeTaker
        {
            private Stopwatch clock;

            public TimeTaker()
            {
                clock =  new Stopwatch();
            }

            public void Start()
            {
                clock.Restart();
            }

            public void Stop(string text)
            {
                clock.Stop();
                Debug.Log($"{text} : {clock.Elapsed.TotalMilliseconds}ms");
            }
        }

        public void Load(string path, int maxcount = int.MaxValue)
        {
            //var path = @"D:\Dump\ExportedScene.json";
            var time = new TimeTaker();

            time.Start();
            var text = System.IO.File.ReadAllText(path);
            time.Stop("Read Text");

            time.Start();
            var json = JSON.Deserialize(text);
            time.Stop("Text to JSON");

            time.Start();
            var scene = JSONSerializer.Deserialize<SceneExport>(json);
            time.Stop("JSON to OBJECT");

            time.Start();
            var array = json.ToArray<JSON>("Objects");
            int count = 0;
            foreach (var child in array)
            {
                var obj = JSONSerializer.Deserialize<SceneObject>(child);

                if (!staticMeshLookup.TryGetValue(obj.PrefabName, out var staticMesh))
                {
                    if (!meshLookup.TryGetValue(obj.PrefabName, out var relativePath))
                        continue;

                    var mesh = Engine.Assets.Load<Mesh>(relativePath);
                    staticMesh = CreateStaticMesh(mesh);
                    staticMeshLookup.Add(obj.PrefabName, staticMesh);
                }

                var entity = new Entity(obj.PrefabName);
                entity.Transform.Position = Utils.StringToVector3(obj.Position);
                entity.Transform.Rotation = Utils.StringToQuaternion(obj.Rotation) * Quaternion.RotationAxis(Vector3.Up, MathHelper.DegreesToRadians(180));
                entity.Transform.Scale = Utils.StringToVector3(obj.Scale);

                var renderer = entity.AddComponent<StaticMeshRenderer>();
                renderer.StaticMesh = staticMesh;

                count++;
                if (count >= maxcount)
                    break;
            }
            time.Stop("Load Meshes and Textures");

            time.Start();
            array = json.ToArray<JSON>("Trees");
            count = 0;
            foreach (var child in array)
            {
                var obj = JSONSerializer.Deserialize<SceneObject>(child);

                if (!staticMeshLookup.TryGetValue(obj.PrefabName, out var staticMesh))
                {
                    if (!meshLookup.TryGetValue(obj.PrefabName, out var relativePath))
                        continue;

                    var mesh = Engine.Assets.Load<Mesh>(relativePath);
                    staticMesh = CreateStaticMesh(mesh, false);
                    staticMeshLookup.Add(obj.PrefabName, staticMesh);
                }

                var entity = new Entity(obj.PrefabName);
                entity.Transform.Position = Utils.StringToVector3(obj.Position);
                entity.Transform.Rotation = Utils.StringToQuaternion(obj.Rotation) * Quaternion.RotationAxis(Vector3.Up, MathHelper.DegreesToRadians(180));
                entity.Transform.Scale = Utils.StringToVector3(obj.Scale);

                var renderer = entity.AddComponent<StaticMeshRenderer>();
                renderer.StaticMesh = staticMesh;

                count++;
                if (count >= maxcount)
                    break;
            }
            time.Stop("Load Trees and Textures");

            time.Start();
            var lights = json.ToArray<JSON>("Lights");
            foreach (var child in lights)
            {
                var light = JSONSerializer.Deserialize<PointLight>(child);

                var entity = new Entity(light.PrefabName);
                entity.Transform.Position = Utils.StringToVector3(light.Position);
                entity.Transform.Rotation = Utils.StringToQuaternion(light.Rotation) * Quaternion.RotationAxis(Vector3.Up, MathHelper.DegreesToRadians(180));
                entity.Transform.Scale = Utils.StringToVector3(light.Scale);

                var renderer = entity.AddComponent<Spark.PointLight>();
                renderer.Color = Utils.StringToVector3(light.Color);
                renderer.Range = light.Range;
                renderer.Intensity = light.Intensity;
            }
            time.Stop("Load Lights");

            MessageDispatcher.Send(Msg.RefreshExplorer);
        }

        private StaticMesh CreateStaticMesh(Mesh mesh, bool tree = false)
        {
            var staticMesh = new StaticMesh { Name = mesh.Name, Mesh = mesh, LODGroup = LODGroups.LargeProps };
            var allLOds = mesh.MeshParts.FindAll((m) => m.Name.Contains("LOD_"));

            var material = Engine.DefaultMaterial;

            var texturepath = Path.GetDirectoryName(mesh.Path) + @"\Textures\";
            var absolutePath = Path.Combine(Engine.ResourceDirectory, texturepath);

            if(Directory.Exists(absolutePath))
            {
                var defaultDiffuse = Engine.Assets.Load<Texture>("checker_grey.dds");
                var defaultNormal = Engine.Assets.Load<Texture>("DefaultNormalMap.dds");
                var defaultData = Engine.Assets.Load<Texture>("DefaultSpecularMap.dds");
                var aoc = Engine.Assets.Load<Texture>("white.dds");

                var shader = new Effect(tree ? "tree" : "mesh_opaque");
                material = new Material(shader);
                material.Name = mesh.Name;
                material.UseInstancing = true;
                material.SetValue("Albedo", defaultDiffuse);
                material.SetValue("Normal", defaultNormal);
                material.SetValue("Occlusion", aoc);
                material.SetValue("Data", defaultData);
                material.SetValue("sampData", Samplers.WrappedAnisotropic);

                foreach (var file in  Directory.EnumerateFiles(absolutePath))
                {
                    if (file.EndsWith(".psd")) continue;
                    var relativePath = file.Replace(Engine.ResourceDirectory, "");

                    var withoutExtension = Path.GetFileNameWithoutExtension(file);
                    if(withoutExtension.EndsWith("_Dif"))
                    {
                        var diffuse = Engine.Assets.Load<Texture>(relativePath);
                        material.SetValue("Albedo", diffuse);
                    }

                    if (withoutExtension.EndsWith("_Nor"))
                    {
                        var diffuse = Engine.Assets.Load<Texture>(relativePath);
                        material.SetValue("Normal", diffuse);
                    }

                    if (withoutExtension.EndsWith("_Aoc"))
                    {
                        aoc = Engine.Assets.Load<Texture>(relativePath);
                        material.SetValue("Occlusion", aoc);
                    }
                }
            }

            // no lods
            if (mesh.MeshParts.Count - allLOds.Count <= 0)
            {
                for (int i = 0; i < mesh.MeshParts.Count; i++)
                {
                    staticMesh.MeshParts.Add(new MeshElement
                    {
                        Mesh = mesh,
                        Material = material,
                        MeshPart = i
                    });
                }
                return staticMesh;
            }

            for (int i = 0; i < mesh.MeshParts.Count; i++)
            {
                var part = mesh.MeshParts[i];
                var name = part.Name;

                int index = name.IndexOf("_LOD_");
                if (index < 0)
                {
                    staticMesh.MeshParts.Add(new MeshElement
                    {
                        Mesh = mesh,
                        Material = material,
                        MeshPart = i
                    });
                }
                else
                {
                    int endIndex = index + 6 < name.Length ? 2 : 1;
                    int lvl = Convert.ToInt32(name.Substring(index + 5, endIndex)) - 1;
                    while (staticMesh.LODs.Count < lvl + 1)
                        staticMesh.LODs.Add(new StaticMeshLOD { Mesh = mesh });

                    staticMesh.LODs[lvl].MeshParts.Add(new MeshElement
                    {
                        Mesh = mesh,
                        Material = material,
                        MeshPart = i
                    });
                }
            }

            return staticMesh;
        }
    }

}
