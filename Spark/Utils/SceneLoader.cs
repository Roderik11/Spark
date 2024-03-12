using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark
{
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
                clock = new Stopwatch();
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
        }

        private StaticMesh CreateStaticMesh(Mesh mesh, bool tree = false)
        {
            var staticMesh = new StaticMesh { Name = mesh.Name, Mesh = mesh, LODGroup = LODGroups.LargeProps };
            var allLOds = mesh.MeshParts.FindAll((m) => m.Name.Contains("LOD_"));

            var material = Engine.DefaultMaterial;

            var texturepath = Path.GetDirectoryName(mesh.Path) + @"\Textures\";
            var absolutePath = Path.Combine(Engine.ResourceDirectory, texturepath);

            if (Directory.Exists(absolutePath))
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

                foreach (var file in Directory.EnumerateFiles(absolutePath))
                {
                    if (file.EndsWith(".psd")) continue;
                    var relativePath = file.Replace(Engine.ResourceDirectory, "");

                    var withoutExtension = Path.GetFileNameWithoutExtension(file);
                    if (withoutExtension.EndsWith("_Dif"))
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
