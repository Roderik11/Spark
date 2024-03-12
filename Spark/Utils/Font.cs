using System;
using System.Collections.Generic;
using System.Xml;
using SharpDX;

namespace Spark
{
    public class Font : IDisposable
    {
        private Texture texture;
        private Dictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>();

        public int Height;
        public int Spacing = 0;

        public struct Glyph
        {
            public RectF Rect;

            public int Height;
            public int Width;

            public int OffsetX;
            public int OffsetY;

            public int Advance;
        }

        public void DrawString(SpriteBatch batch, string str, int x, int y, int color)
        {
            if (string.IsNullOrEmpty(str))
                return;

            int iCurrentX = x;
            int iCurrentY = y;

            Color4 col = new Color4(color);

            foreach (char c in str)
            {
                if (c == '\n')
                {
                    iCurrentY += Height;
                    iCurrentX = x;
                }
                else if (Glyphs.ContainsKey(c))
                {
                    Glyph glyph = Glyphs[c];

                    if (c != ' ')
                    {
                        batch.Draw(iCurrentX, iCurrentY + glyph.OffsetY, glyph.Width, glyph.Height, glyph.Rect, col, texture);
                    }

                    iCurrentX += glyph.Advance - Spacing;
                }
            }
        }

        public System.Drawing.Point GetTextSize(string text)
        {
            System.Drawing.Point p = new System.Drawing.Point();

            p.Y = Height;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    p.Y += Height;
                }
                else if (Glyphs.ContainsKey(c))
                {
                    Glyph uv = Glyphs[c];
                    p.X += uv.Advance - Spacing;
                }
            }

            return p;
        }

        private static string UnescapeXML(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            string returnString = s;
            returnString = returnString.Replace("&apos;", "'");
            returnString = returnString.Replace("&quot;", "\"");
            returnString = returnString.Replace("&gt;", ">");
            returnString = returnString.Replace("&lt;", "<");
            returnString = returnString.Replace("&amp;", "&");

            return returnString;
        }

        private static string FixFont(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            string returnString = s;
            returnString = returnString.Replace("\"'\"", "\"&apos;\"");
            returnString = returnString.Replace("\"\"", "\"&quot;\"");
            returnString = returnString.Replace("\">\"", "\"&gt;\"");
            returnString = returnString.Replace("\"<\"", "\"&lt;\"");
            returnString = returnString.Replace("\"&\"", "\"&amp;\"");

            return returnString;
        }

        public static Font LoadFont(string name)
        {
            Font ret = new Font();
            ret.texture = Engine.Assets.Load<Texture>(name + ".dds");

            XmlDocument doc = new XmlDocument();
            doc.Load(Engine.Assets.BaseDirectory + name + ".xml");

            int maxHeight = 0;
            int minOffsetY = int.MaxValue;

            Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();

            XmlNodeList nodes = doc.SelectNodes("descendant::Char");

            int fontHeight = Convert.ToInt32(doc.SelectSingleNode("Font").Attributes["height"].Value);

            foreach (XmlNode node in nodes)
            {
                string[] arr = node.Attributes["rect"].Value.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] arr2 = node.Attributes["offset"].Value.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                int offx = Convert.ToInt32(arr2[0]);
                int offy = Convert.ToInt32(arr2[1]);

                int x = Math.Abs(Convert.ToInt32(arr[0]));
                int y = Math.Abs(Convert.ToInt32(arr[1]));

                int width = Convert.ToInt32(Convert.ToSingle(arr[2]));
                int height = Convert.ToInt32(Convert.ToSingle(arr[3]));
                int advance = Convert.ToInt32(node.Attributes["width"].Value);

                maxHeight = Math.Max(maxHeight, height);
                minOffsetY = Math.Min(minOffsetY, offy);

                string code = UnescapeXML(node.Attributes["code"].Value);

                try
                {
                    char c = char.Parse(code);

                    // Debug.Log("Character found (" + c + "): " + code);

                    Glyph glyph = new Glyph();
                    glyph.Rect = new RectF(x, y, width, height);
                    glyph.Advance = advance;
                    glyph.OffsetX = offx;
                    glyph.OffsetY = offy;
                    glyph.Width = width;
                    glyph.Height = height;

                    glyphs.Add(c, glyph);
                }
                catch
                {
                    throw new Exception("Font XML file contains malformed data");
                }
            }

            ret.Height = maxHeight;

            int baseline = fontHeight - maxHeight;

            foreach (char c in glyphs.Keys)
            {
                Glyph uv = glyphs[c];
                uv.OffsetY -= minOffsetY;

                ret.Glyphs.Add(c, uv);
            }

            return ret;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (texture != null)
                texture.Dispose();

            texture = null;
        }

        #endregion IDisposable Members
    }
}