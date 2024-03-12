using Squid;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Spark.Editor
{
    [GUIInspector(typeof(AssetReader<Texture>))]
    public class TextureInspector : GenericInspector
    {
        private static TexturePreview texturePreview;

        public TextureInspector(GUIObject target) : base(target)
        {
            var reader = target.Target as AssetReader<Texture>;
            var texture = Engine.Assets.Load<Texture>(reader.Filepath);

            if (texturePreview == null)
                texturePreview = new TexturePreview();

            texturePreview.Bind(texture, reader);
        }

        public override Control GetPreview() => texturePreview;
    }

    public class TexturePreview : Frame, IPreview
    {
        private readonly Frame header;
        private readonly Label lblInfo;
        private readonly ImageControl rawImage;

        private Button btnR;
        private Button btnG;
        private Button btnB;
        private Button btnA;
        private Slider mipSlider;

        public TexturePreview() 
        {
            Dock = DockStyle.Fill;

            header = new Frame
            {
                Style = "category",
                Size = new Point(100, 24),
                Dock = DockStyle.Top,
                Padding = new Margin(0, 0, 24, 0)
            };

            btnA = AddButton("A");
            btnB = AddButton("B");
            btnG = AddButton("G");
            btnR = AddButton("R");

            //mipSlider = new Slider();
            //mipSlider.Size = new Point(120, 20);
            //mipSlider.Orientation = Orientation.Horizontal;
            //mipSlider.Dock = DockStyle.Right;
            //mipSlider.Button.Style = "button";
            //mipSlider.Button.Size = new Point(24, 24);
            //mipSlider.Style = "tooltip";
            //mipSlider.Minimum = 0;
            //mipSlider.Maximum = 4;
            //mipSlider.Steps = 1;
            //header.Controls.Add(mipSlider);

            rawImage = new ImageControl
            {
                Dock = DockStyle.Fill,
                Tiling = TextureMode.StretchAspect
            };

            lblInfo = new Label
            {
                Style = "",
                Size = new Point(100, 24),
                Dock = DockStyle.Bottom,
                TextAlign = Alignment.MiddleCenter
            };

            Controls.Add(header);
            Controls.Add(rawImage);
            GetElements().Add(lblInfo);
        }

        public void Bind(Texture texture, AssetReader<Texture> reader)
        {
            rawImage.Texture = reader.Filepath;
            lblInfo.Text = $"{texture.Description.Format}  {texture.Description.Width}x{texture.Description.Height}";
        }

        public void OnEnable()
        {

        }

        public void OnDisable()
        {

        }

        Button AddButton(string label)
        {
            var btn = new Button
            {
                Dock = DockStyle.Right,
                Size = new Point(24, 24),
                Margin = new Margin(1, 0, 0, 0),
                Text = label
            };
            header.Controls.Add(btn);
            return btn;
        }
    }
}
