using System.Collections.Generic;
using Spark;

namespace Spark.Client
{
    public class ProjectWindow : EditorWindow
    {
        private List<AssetInfo> Files = new List<AssetInfo>();
        private Dictionary<string, Thumbnail> thumbnails = new Dictionary<string, Thumbnail>();
        private int scroll;

        protected override void OnEnable()
        {
            var files = System.IO.Directory.GetFiles(Engine.Assets.BaseDirectory);
            foreach (var file in files)
            {
                var filename = System.IO.Path.GetFileName(file);
                var assetType = AssetManager.GetAssetType(filename);

                if (assetType != null)
                {
                    var info = new AssetInfo(file, assetType);
                    this.Files.Add(info);
                }
            }
        }

        string searchTerm = string.Empty;

        protected override void OnGUI()
        {
            IMGUI.BeginGroup(position.x, position.y, position.w, 30);
            searchTerm = IMGUI.TextField("Search:", searchTerm);
            IMGUI.EndGroup();

            IMGUI.BeginArea(position.x, position.y + 30, position.w, position.h - 30, scroll, windowColor);

            bool applyFilter = !string.IsNullOrEmpty(searchTerm);

            foreach (var file in Files)
            {
                if (applyFilter)
                {
                    if (!file.Name.Contains(searchTerm))
                        continue;
                }

                var texture = GetThumbnail(file.FileInfo.FullName);

                if (IMGUI.BigTile($"{file.Name}\n{file.AssetType.Name}\n{file.FileSize}", texture, true))
                {

                }
            }

            scroll = IMGUI.EndArea();
        }

        private Texture GetThumbnail(string file)
        {
            if (!thumbnails.TryGetValue(file, out var thumbnail))
            {
                thumbnail = new Thumbnail(file);
                thumbnail.Load();
                thumbnails.Add(file, thumbnail);
            }

            return thumbnail.texture;
        }
    }
}
