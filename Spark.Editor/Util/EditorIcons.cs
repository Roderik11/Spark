using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Spark.Editor.Properties;

namespace Spark.Editor
{
    class EditorIcons
    {
        public const int FolderClosed = 0;
        public const int FolderOpen = 1;
        public const int Document = 2;
        public const int Script = 3;
        public const int Texture = 4;
        public const int Prototype = 5;
        public const int ProtoInstance = 6;

        public static ImageList Images;

        static EditorIcons()
        {
            Images = new ImageList();
            Images.ColorDepth = ColorDepth.Depth32Bit;
            Images.Images.Add(Resources.Folder_closed);
            Images.Images.Add(Resources.Folder_open);
            Images.Images.Add(Resources.Document);
            Images.Images.Add(Resources.CSCodefile);
            Images.Images.Add(Resources.genericpic);
            Images.Images.Add(Resources.cube);
            Images.Images.Add(Resources.cube2);
        }
    }
}
