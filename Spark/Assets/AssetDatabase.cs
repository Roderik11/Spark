using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using System.IO;
using SharpDX.Direct3D11;
using System.Collections;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;
using System.Net;
using Spark.Graph;

namespace Spark
{
    public class Thumbnail
    {
        public readonly string filename;
        public Texture texture;
        public bool Finished;

        public event Action OnFinishedLoading;

        public Action<Thumbnail> CreateThumbnail;

        public Thumbnail(string filename)
        {
            this.filename = filename;
        }

        public void Load()
        {
            var assetType = AssetManager.GetAssetType(filename);

            if (assetType == typeof(Texture))
                Task.Factory.StartNew(() => LoadTextureAsync());
            else if (assetType == typeof(Mesh))
                CreateThumbnail(this);
            else
                Finished = true;
        }

        private void LoadTextureAsync()
        {
            texture = CreateTextureThumbnail(filename);
            Finished = true;
            OnFinishedLoading?.Invoke();
        }

        private Texture CreateTextureThumbnail(string filename)
        {
            Texture texture = null;
            
            try
            {
                if (!Path.IsPathRooted(filename))
                    filename = Path.Combine(AssetDatabase.Assets.FullName, filename);

                Texture2D resource = Texture.FromFile(Engine.Device, filename);

                texture = new Texture
                {
                    Resource = resource,
                };
            }
            catch { }

            return texture;
        }
    }

    public static class AssetDatabase
    {
        public static DirectoryInfo Assets;
        public static DirectoryInfo Library;
        public static DirectoryInfo Meta;
        public static DirectoryInfo Packaged;
        public static DirectoryInfo Thumbnails;

        private static Dictionary<string, string> pathToGuid = new Dictionary<string, string>();
        private static Dictionary<string, string> guidToPath = new Dictionary<string, string>();
        private static Dictionary<string, string> guidToThumb = new Dictionary<string, string>();

        private static Dictionary<string, MetaFile> pathToMeta = new Dictionary<string, MetaFile>();

        const string black = "black.dds";

        public static bool UsePackedAssets = true;

        public static void InitializeProject(bool isEditor)
        {
            //   - get FileInfo of all resources
            var extensions = AssetManager.GetAllExtensions().ToArray();
            var files = Assets.GetFilesFiltered(extensions);

            CreateMetaData(files);
            
            if(isEditor) ImportFiles(files);
            if(isEditor) CreateThumbnails(files); 
        }

        private static void CreateMetaData(IEnumerable<FileInfo> files)
        {
            var rootDir = Assets;
            var metaDir = Meta;

            var fileHash = new HashSet<string>(files.Select(fi => fi.FullName.Replace(rootDir.FullName, string.Empty)));

            // - get all existing meta files
            var metaFiles = metaDir.GetFilesFiltered(".meta");

            // - read all existing meta files
            foreach (var file in metaFiles)
            {
                var text = File.ReadAllText(file.FullName);
                var json = JSON.Deserialize(text);
                var metaData = JSONSerializer.Deserialize<MetaFile>(json);

                if (!fileHash.Contains(metaData.Path))
                {
                    // meta file has no matching asset -> delete it
                    file.Delete();
                    continue;
                }

                pathToMeta.Add(metaData.Path.ToLowerInvariant(), metaData);
                pathToGuid.Add(metaData.Path.ToLowerInvariant(), metaData.Guid);
                guidToPath.Add(metaData.Guid, metaData.Path);
            }

            // - create meta files for new assets
            foreach (var file in files)
            {
                var relativePath = file.FullName.Replace(rootDir.FullName, string.Empty);
                var toLower = relativePath.ToLowerInvariant();

                // - meta file exists -> skip
                if (pathToMeta.TryGetValue(toLower, out var metaData))
                    continue;

                var guid = Guid.NewGuid().ToString("N");
                var importerType = AssetManager.GetImporterType(file.Name);
                var importer = Activator.CreateInstance(importerType) as AssetReader;
                importer.Filepath = relativePath;

                metaData = new MetaFile
                {
                    Guid = guid,
                    Path = relativePath,
                    AssetReader = importer
                };

                pathToMeta.Add(toLower, metaData);
                pathToGuid.Add(toLower, metaData.Guid);
                guidToPath.Add(metaData.Guid, metaData.Path);

                var metaJson = JSONSerializer.Serialize(metaData).ToText();
                var metaPath = Path.Combine(metaDir.FullName, $"{guid}.meta");

                File.WriteAllText(metaPath, metaJson);

                Debug.Log("Creating Meta file" + relativePath);
            }
        }

        private static void ImportFiles(IEnumerable<FileInfo> files)
        {
            var rootDir = Assets;
            var packDir = Packaged;

            var importedFiles = packDir.GetFilesFiltered(".dds", ".smesh");
            var importedHash = new Dictionary<string, FileInfo>();
            foreach(var file in importedFiles)
                importedHash.Add(Path.GetFileNameWithoutExtension(file.FullName), file);

            foreach (var file in files)
            {
                var relativePath = file.FullName.Replace(rootDir.FullName, string.Empty);
                var toLower = relativePath.ToLowerInvariant();
                var noExtension = Path.GetFileNameWithoutExtension(relativePath);
                var metaData = pathToMeta[toLower];
                var assetType = AssetManager.GetAssetType(relativePath);
                bool finished = false;
                MeshPacker meshPacker = new MeshPacker();

                if (importedHash.TryGetValue(metaData.Guid, out var importedFile))
                {
                    if (DateTime.Compare(file.LastWriteTimeUtc, importedFile.LastWriteTimeUtc) <= 0)
                        continue;
                }
                
                if (assetType == typeof(Texture))
                {
                    Debug.Log("Importing Texture:" + relativePath);

                    if(file.Name.EndsWith(".dds"))
                    {
                        File.Copy(file.FullName, Path.Combine(Packaged.FullName, metaData.Guid + ".dds"), true);
                        continue;
                    }

                    // if pow2 then dxt5 else what format?
                    var info = new ProcessStartInfo("CMD.exe");
                    info.Arguments = $"/C texconv -f DXT5 -r \"{file.FullName}\" -pow2 -o {Packaged}";
                    //info.CreateNoWindow = true;
                    //info.UseShellExecute = false;
                 
                    var process = new Process();
                    process.EnableRaisingEvents = true;
                    process.StartInfo = info;
                    process.Exited += (a, b) =>
                    {
                        finished = true;
                    };
                    process.Start();
                
                    while (!finished) { }

                    var clearName = Path.Combine(Packaged.FullName, noExtension + ".dds");
                    var guidName = Path.Combine(Packaged.FullName, metaData.Guid + ".dds");
                    if(File.Exists(guidName))
                        File.Delete(guidName);

                    File.Move(clearName, guidName);
                }

                if (assetType == typeof(Mesh))
                {
                    Debug.Log("Importing Mesh:" + relativePath);

                    var scale = relativePath.EndsWith(".dae") ? 0.01f : 1f;
                    var mod = ("Scale", scale);

                    bool usepack = UsePackedAssets;
                    UsePackedAssets = false;
                    var mesh = Engine.Assets.Load<Mesh>(relativePath, mod);
                    UsePackedAssets = usepack;

                    if (mesh != null)
                    {
                        var path = Path.Combine(Packaged.FullName, metaData.Guid + ".smesh");
                        if (File.Exists(path))
                            File.Delete(path);

                        FileStream filestream = File.Create(path);
                        BinaryWriter writer = new BinaryWriter(filestream);
                        meshPacker.Pack(writer, mesh);
                        filestream.Flush();
                        filestream.Close();
                    }
                }

            }
        }

        private static void CreateThumbnails(IEnumerable<FileInfo> files)
        {
            var rootDir = Assets;
            var thumbsDir = Thumbnails;

            var thumbnails = thumbsDir.GetFilesFiltered(".png");
            var thumbHash = new Dictionary<string, FileInfo>();

            foreach (var file in thumbnails)
            {
                var relativePath = file.FullName.Replace(thumbsDir.FullName, string.Empty);
                var noExtension = Path.GetFileNameWithoutExtension(relativePath);
                if (thumbHash.ContainsKey(noExtension)) continue;
                thumbHash.Add(noExtension, file);
                guidToThumb.Add(noExtension, noExtension);
            }

            ThumbnailFactory factory = new ThumbnailFactory();

            foreach (var file in files)
            {
                var relativePath = file.FullName.Replace(rootDir.FullName, string.Empty);
                var toLower = relativePath.ToLowerInvariant();
                var metaData = pathToMeta[toLower];
                var assetType = AssetManager.GetAssetType(relativePath);

                if (thumbHash.TryGetValue(metaData.Guid, out var thumbnailInfo))
                    continue;

                Debug.Log("Generating Thumbnail:" + relativePath);

                Texture thumbnailTexture = null;

                if (assetType == typeof(Texture))
                    thumbnailTexture = CreateTextureThumbnail(relativePath);

                if (assetType == typeof(Mesh))
                    thumbnailTexture = factory.CreateThumbnail(relativePath);

                if (thumbnailTexture == null)
                    continue;

                var path = Path.Combine(thumbsDir.FullName, $"{metaData.Guid}.png");
                thumbnailTexture.Save(path, 128, 128);
                guidToThumb.Add(metaData.Guid, metaData.Guid);
            }

            factory.Dispose();
        }

        public static string GuidToPath(string guid)
        {
            if (guidToPath.TryGetValue(guid, out var path))
                return path;

            return string.Empty;
        }

        public static string PathToGuid(string path)
        {
            if (path == null) return string.Empty;

            path = path.Replace(Assets.FullName + "\\", string.Empty);
            path = path.Replace(Thumbnails.FullName + "\\", string.Empty);
            path = path.Replace(Packaged.FullName + "\\", string.Empty);

            path = path.Replace(Assets.FullName, string.Empty);
            path = path.Replace(Thumbnails.FullName, string.Empty);
            path = path.Replace(Packaged.FullName, string.Empty);

            path = path.Replace("/", "\\").ToLowerInvariant();

            if (pathToGuid.TryGetValue(path, out var guid))
                return guid;

            return string.Empty;
        }

        public static string GetThumbnail(IAsset asset)
        {
            if(asset == null) return black;
            if (string.IsNullOrEmpty(asset.Path)) return black;
            var guid = PathToGuid(asset.Path);
            if(string.IsNullOrEmpty(guid)) return black;
            if(guidToThumb.ContainsKey(guid))
                return $"@{guid}.png";

            return black;
        }

        public static AssetReader GetAssetReader(string path)
        {
            path = path.Replace(Assets.FullName + "\\", string.Empty);
            path = path.Replace(Thumbnails.FullName + "\\", string.Empty);
            path = path.Replace(Packaged.FullName + "\\", string.Empty);

            path = path.Replace(Assets.FullName, string.Empty);
            path = path.Replace(Thumbnails.FullName, string.Empty);
            path = path.Replace(Packaged.FullName, string.Empty);

            path = path.Replace("/", "\\").ToLowerInvariant();
                    
            if (pathToMeta.TryGetValue(path, out var meta))
            {
                meta.AssetReader.Filepath = path;
                return meta.AssetReader;
            }

            return null;
        }

        public static void Initialize()
        {
            Assets = new DirectoryInfo(Engine.ResourceDirectory);
            Library = CreateDirectory("Library");
            Meta = CreateDirectory(Path.Combine(Library.Name, "Meta"));
            Thumbnails = CreateDirectory(Path.Combine(Library.Name, "Thumbs"));
            Packaged = CreateDirectory(Path.Combine(Library.Name, "Packed"));
        }

        static DirectoryInfo CreateDirectory(string path)
        {
            var dir = Path.Combine(Engine.RootDirectory, path);

            if (!Directory.Exists(dir))
                return Directory.CreateDirectory(dir);

            return new DirectoryInfo(dir);
        }

        private static Texture CreateTextureThumbnail(string file)
        {
            return Engine.Assets.Load<Texture>(file);
        }
    }

    public class ThumbnailFactory : IDisposable
    {
        private Material material;
        private RenderView viewport;
        private AssetManager contentManager;

        public ThumbnailFactory(int size = 128)
        {
            var shader = new Effect("mesh_forward");
            material = new Material(shader);

            var texture = Engine.Assets.Load<Texture>("checker_grey.dds");
            material.SetParameter("Albedo", texture);
            material.SetParameter("linearSampler", Samplers.WrappedAnisotropic);

            viewport = new RenderView(size, size);
            contentManager = new AssetManager(Engine.Device);
            contentManager.BaseDirectory = Engine.ResourceDirectory;
        }

        public Texture CreateThumbnail(string path)
        {
            var mesh = contentManager.Load<Mesh>(path);
            if (mesh == null) return null;

            Graphics.SetTargets(viewport.DepthBufferTarget, viewport.BackBufferTarget);
            Graphics.SetViewport(new ViewportF(0, 0, viewport.Size.X, viewport.Size.Y, 0.0f, 1.0f));
            Graphics.SetScissorRectangle(0, 0, (int)viewport.Size.X, (int)viewport.Size.Y);
            Graphics.ClearRenderTargetView(viewport.BackBufferTarget, new Color4(0, 0, 0, 1));
            Graphics.ClearDepthStencilView(viewport.DepthBufferTarget, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);
            Graphics.SetDepthStencilState(States.ZReadZWriteNoStencil);
            Graphics.SetBlendState(States.BlendNone, null, 0xfffffff);

            material.SetParameter("MaterialColor", new Vector3(.8f, .8f, .8f));
            material.SetParameter("LightColor", new Vector3(1, 1, 1));
            material.SetParameter("LightDirection", Vector3.Normalize(new Vector3(1, -1, 1)));

            var bounds = mesh.BoundingBox;
            var distance = bounds.Size.Length();

            var camerapos = bounds.Center - Vector3.ForwardLH * distance * 1.25f;
            var view = Matrix.LookAtLH(camerapos, bounds.Center, Vector3.Up);
            var proj = Camera.ProjectionMatrix(new Vector2(viewport.Size.X, viewport.Size.Y), 45, 0.1f, Math.Max(2000, distance * 2));
            var meshPosition = -bounds.Center;// Vector3.Zero;
            var meshRotation = Quaternion.RotationYawPitchRoll(-MathUtil.DegreesToRadians(25), 0, 0);

            var rotatedOrigin = Vector3.TransformCoordinate(meshPosition, Matrix.RotationQuaternion(meshRotation));
            meshPosition = rotatedOrigin + bounds.Center;

            material.SetParameter("View", view);
            material.SetParameter("Projection", proj);
            material.SetParameter("CameraPosition", camerapos);
            material.SetParameter("World", mesh.RootRotation * (Matrix.RotationQuaternion(meshRotation) * Matrix.Translation(meshPosition)));
            material.Apply();

            foreach (var part in mesh.MeshParts)
            {
                if (part.Enabled)
                    mesh.DrawImmediate(part, material, Engine.Device.ImmediateContext);
            }

            var result = viewport.BackBufferTexture.RamCopy();
            return result;
        }

        public void Dispose()
        {
            Engine.Device.ImmediateContext.Flush();
            Engine.Device.ImmediateContext.ClearState();

            contentManager.Dispose();
            viewport.Dispose();
        }
    }

    public class MetaFile
    {
        public string Guid;
        public string Path;

        public AssetReader AssetReader;
    }
}
