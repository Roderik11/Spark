using SharpDX;
using System.IO;
using SharpDX.Direct3D11;
using System.Diagnostics;

namespace Spark
{
    [AssetImporter(".dds", ".tga", ".jpg", ".bmp", ".png")]
    public class TextureImporter : AssetImporter<Texture>
    {
        public TextureCompression Compression;
        public TextureSize MaximumSize;

        public bool GenerateMips = true;
        public bool PowerOfTwo;
        public bool FlipVertical;
        public bool FlipHorizontal;
        public bool PremultiplyAlpha;
        public bool sRGB;

        public void Import(FileInfo file, DirectoryInfo destination, string guid)
        {
            var rootDir = AssetDatabase.Assets;
            var relativePath = file.FullName.Replace(rootDir.FullName, string.Empty);
            var noExtension = Path.GetFileNameWithoutExtension(relativePath);

            Debug.Log("Importing Texture:" + relativePath);

            if (file.Name.EndsWith(".dds"))
            {
                File.Copy(file.FullName, Path.Combine(destination.FullName, guid + ".dds"), true);
                return;
            }

            var info = new ProcessStartInfo("CMD.exe")
            {
                Arguments = $"/C texconv -y -f DXT5 -r \"{file.FullName}\" -o {destination}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process();
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (sender, args) =>
            {
                Debug.Log(args.Data);
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                Debug.Log(args.Data);
            };

            process.StartInfo = info;
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var clearName = Path.Combine(destination.FullName, noExtension + ".dds");
            var guidName = Path.Combine(destination.FullName, guid + ".dds");

            if (File.Exists(guidName))
                File.Delete(guidName);

            if (File.Exists(clearName))
                File.Move(clearName, guidName);
        }
    }
}

