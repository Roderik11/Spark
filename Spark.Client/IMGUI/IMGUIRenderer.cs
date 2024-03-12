using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using Spark;
using SharpDX;

namespace Spark.Client
{
    public class IMGUIRenderer : IDisposable
    {
        public SpriteBatch Spritebatch;

        private int FontIndex;
        private int TextureIndex;

        private Dictionary<int, Font> Fonts = new Dictionary<int, Font>();
        private Dictionary<string, int> FontLookup = new Dictionary<string, int>();
        private Dictionary<int, Texture> Textures = new Dictionary<int, Texture>();
        private Dictionary<string, int> TextureLookup = new Dictionary<string, int>();
        private Dictionary<string, Squid.Font> FontTypes = new Dictionary<string, Squid.Font>();

        public IMGUIRenderer()
        {
            Spritebatch = new SpriteBatch();
            FontTypes.Add(Squid.Font.Default, new Squid.Font { Name = "myriad_12", Family = "Myriad", Size = 14, Bold = true, International = true });
            FontTypes.Add("myriad_12", new Squid.Font { Name = "myriad_12", Family = "Myriad", Size = 14, Bold = true, International = true });
            FontTypes.Add("myriad_14", new Squid.Font { Name = "myriad_14", Family = "Myriad", Size = 14, Bold = true, International = true });
            FontTypes.Add("myriad_15", new Squid.Font { Name = "myriad_15", Family = "Myriad", Size = 14, Bold = true, International = true });
            FontTypes.Add("segoe_wp_30", new Squid.Font { Name = "segoe_wp_30", Family = "Segoe", Size = 14, Bold = true, International = true });
            Squid.Gui.AlwaysScissor = true;
        }

        public int GetTexture(string name)
        {
            if (TextureLookup.ContainsKey(name))
                return TextureLookup[name];

            string filename = name;
            string[] files = System.IO.Directory.GetFiles(Engine.Assets.BaseDirectory, System.IO.Path.GetFileName(name), System.IO.SearchOption.AllDirectories);

            if (files.Length > 0)
                filename = files[0];

            Texture texture = Engine.Assets.Load<Texture>(name);

            TextureIndex++;

            TextureLookup.Add(name, TextureIndex);
            Textures.Add(TextureIndex, texture);

            return TextureIndex;
        }

        public int GetFont(string name)
        {
            if (FontLookup.ContainsKey(name))
                return FontLookup[name];

            if (!FontTypes.ContainsKey(name))
                return -1;

            Squid.Font type = FontTypes[name];
            Spark.Font font = Spark.Font.LoadFont(type.Name);
             
            FontIndex++;

            FontLookup.Add(name, FontIndex);
            Fonts.Add(FontIndex, font);

            return FontIndex;
        }

        public SharpDX.Point GetTextSize(string text, int font)
        {
            if (string.IsNullOrEmpty(text))
                return new SharpDX.Point();

            Font f = Fonts[font];
            var size = f.GetTextSize(text);

            return new SharpDX.Point(size.X, size.Y);
        }

        public Squid.Point GetTextureSize(int texture)
        {
            Texture tex = Textures[texture];
            return new Squid.Point(tex.Resource.Description.Width, tex.Resource.Description.Height);
        }

        public void DrawBox(int x, int y, int w, int h, int color)
        {
            Spritebatch.Draw(x, y, w, h, new RectF(0, 0, 1, 1), new Color4(color), null);
        }

        public void DrawText(string text, int x, int y, int font, int color)
        {
            if (!Fonts.ContainsKey(font))
                return;

            Font f = Fonts[font];
            f.DrawString(Spritebatch, text, x, y, color);
        }

        public void DrawTexture(int texture, int x, int y, int w, int h, RectF rect, int color)
        {
            if (!Textures.ContainsKey(texture))
                return;

            Spritebatch.Draw(x, y, w, h, rect, new Color4(color), Textures[texture]);
        }

        public void DrawTexture(int texture, int x, int y, int w, int h, int color)
        {
            if (!Textures.ContainsKey(texture))
            {
                 Spritebatch.Draw(x, y, w, h, new RectF(0, 0, w, h) , new Color4(color), null);
                return;
            }

            Spritebatch.Draw(x, y, w, h, new RectF(0, 0, w, h) , new Color4(color), Textures[texture]);
        }

        public void DrawTexture(Texture texture, int x, int y, int w, int h, int color)
        {
            Spritebatch.Draw(x, y, w, h, new RectF(0, 0, w, h), new Color4(color), texture);
        }

        public void DrawTexture(Texture texture, int x, int y, int w, int h, RectF rect, int color)
        {
            Spritebatch.Draw(x, y, w, h, rect, new Color4(color), texture);
        }

        public void Scissor(int x1, int y1, int x2, int y2)
        {
            Spritebatch.Scissor = new SharpDX.Rectangle(x1, y1, x2, y2);
        }

        public void StartBatch()
        {
            if (isBatching)
                return;

            Spritebatch.Start();
            isBatching = true;
        }

        private bool isBatching;

        public void EndBatch(bool final)
        {
            if (!final) 
                return;

            Spritebatch.End();
            isBatching = false;
        }

        private void Dispose(bool disposed)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
