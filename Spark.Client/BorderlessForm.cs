using System;
using System.Drawing;
using System.Windows.Forms;

namespace Spark.Client
{
    public partial class Borderless : SharpDX.Windows.RenderForm
    {
        #region 'Drag'

        int posX;
        int posY;
        bool drag;

        private void panel1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                    maximized = false;
                }
                else
                {
                    this.WindowState = FormWindowState.Maximized;
                    maximized = true;
                }
            }
        }

        private void buttonClos_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonMax_Click(object sender, EventArgs e)
        {
            if (maximized)
            {
                maximized = false;
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                maximized = true;
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void buttonMin_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                drag = true;
                posX = Cursor.Position.X - this.Left;
                posY = Cursor.Position.Y - this.Top;
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                this.Top = System.Windows.Forms.Cursor.Position.Y - posY;
                this.Left = System.Windows.Forms.Cursor.Position.X - posX;
            }
            this.Cursor = Cursors.Default;
        }

        #endregion

        #region 'Resize'

        bool onFullScreen;
        bool maximized;
        bool on_MinimumSize;
        short minimumWidth = 350;
        short minimumHeight = 26;
        short borderSpace = 20;
        short borderDiameter = 3;

        bool onBorderRight;
        bool onBorderLeft;
        bool onBorderTop;
        bool onBorderBottom;
        bool onCornerTopRight;
        bool onCornerTopLeft;
        bool onCornerBottomRight;
        bool onCornerBottomLeft;

        bool movingRight;
        bool movingLeft;
        bool movingTop;
        bool movingBottom;
        bool movingCornerTopRight;
        bool movingCornerTopLeft;
        bool movingCornerBottomRight;
        bool movingCornerBottomLeft;

        public Borderless()
        {
            MouseMove += Borderless_MouseMove;
            MouseDown += Borderless_MouseDown;
            MouseUp += Borderless_MouseUp;
        }
        private void Borderless_MouseUp(object sender, MouseEventArgs e)
        {
            stopResizer();
        }

        private void Borderless_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (onBorderRight) { movingRight = true; } else { movingRight = false; }
                if (onBorderLeft) { movingLeft = true; } else { movingLeft = false; }
                if (onBorderTop) { movingTop = true; } else { movingTop = false; }
                if (onBorderBottom) { movingBottom = true; } else { movingBottom = false; }
                if (onCornerTopRight) { movingCornerTopRight = true; } else { movingCornerTopRight = false; }
                if (onCornerTopLeft) { movingCornerTopLeft = true; } else { movingCornerTopLeft = false; }
                if (onCornerBottomRight) { movingCornerBottomRight = true; } else { movingCornerBottomRight = false; }
                if (onCornerBottomLeft) { movingCornerBottomLeft = true; } else { movingCornerBottomLeft = false; }
            }
        }

        private void Borderless_MouseMove(object sender, MouseEventArgs e)
        {
            if (onFullScreen | maximized) { return; }

            if (this.Width <= minimumWidth) { this.Width = (minimumWidth + 5); on_MinimumSize = true; }
            if (this.Height <= minimumHeight) { this.Height = (minimumHeight + 5); on_MinimumSize = true; }
            if (on_MinimumSize) { stopResizer(); } else { startResizer(); }


            if ((Cursor.Position.X > ((this.Location.X + this.Width) - borderDiameter))
                & (Cursor.Position.Y > (this.Location.Y + borderSpace))
                & (Cursor.Position.Y < ((this.Location.Y + this.Height) - borderSpace)))
            { this.Cursor = Cursors.SizeWE; onBorderRight = true; }

            else if ((Cursor.Position.X < (this.Location.X + borderDiameter))
                & (Cursor.Position.Y > (this.Location.Y + borderSpace))
                & (Cursor.Position.Y < ((this.Location.Y + this.Height) - borderSpace)))
            { this.Cursor = Cursors.SizeWE; onBorderLeft = true; }

            else if ((Cursor.Position.Y < (this.Location.Y + borderDiameter))
                & (Cursor.Position.X > (this.Location.X + borderSpace))
                & (Cursor.Position.X < ((this.Location.X + this.Width) - borderSpace)))
            { this.Cursor = Cursors.SizeNS; onBorderTop = true; }

            else if ((Cursor.Position.Y > ((this.Location.Y + this.Height) - borderDiameter))
                & (Cursor.Position.X > (this.Location.X + borderSpace))
                & (Cursor.Position.X < ((this.Location.X + this.Width) - borderSpace)))
            { this.Cursor = Cursors.SizeNS; onBorderBottom = true; }

            else if ((Cursor.Position.X == ((this.Location.X + this.Width) - 1))
                & (Cursor.Position.Y == this.Location.Y))
            { this.Cursor = Cursors.SizeNESW; onCornerTopRight = true; }
            else if ((Cursor.Position.X == this.Location.X)
                & (Cursor.Position.Y == this.Location.Y))
            { this.Cursor = Cursors.SizeNWSE; onCornerTopLeft = true; }

            else if ((Cursor.Position.X == ((this.Location.X + this.Width) - 1))
                & (Cursor.Position.Y == ((this.Location.Y + this.Height) - 1)))
            { this.Cursor = Cursors.SizeNWSE; onCornerBottomRight = true; }

            else if ((Cursor.Position.X == this.Location.X)
                & (Cursor.Position.Y == ((this.Location.Y + this.Height) - 1)))
            { this.Cursor = Cursors.SizeNESW; onCornerBottomLeft = true; }

            else
            {
                onBorderRight = false;
                onBorderLeft = false;
                onBorderTop = false;
                onBorderBottom = false;
                onCornerTopRight = false;
                onCornerTopLeft = false;
                onCornerBottomRight = false;
                onCornerBottomLeft = false;
                this.Cursor = Cursors.Default;
            }
        }

        private void startResizer()
        {
            if (movingRight)
            {
                this.Width = Cursor.Position.X - this.Location.X;
            }

            else if (movingLeft)
            {
                this.Width = ((this.Width + this.Location.X) - Cursor.Position.X);
                this.Location = new Point(Cursor.Position.X, this.Location.Y);
            }

            else if (movingTop)
            {
                this.Height = ((this.Height + this.Location.Y) - Cursor.Position.Y);
                this.Location = new Point(this.Location.X, Cursor.Position.Y);
            }

            else if (movingBottom)
            {
                this.Height = (Cursor.Position.Y - this.Location.Y);
            }

            else if (movingCornerTopRight)
            {
                this.Width = (Cursor.Position.X - this.Location.X);
                this.Height = ((this.Location.Y - Cursor.Position.Y) + this.Height);
                this.Location = new Point(this.Location.X, Cursor.Position.Y);
            }

            else if (movingCornerTopLeft)
            {
                this.Width = ((this.Width + this.Location.X) - Cursor.Position.X);
                this.Location = new Point(Cursor.Position.X, this.Location.Y);
                this.Height = ((this.Height + this.Location.Y) - Cursor.Position.Y);
                this.Location = new Point(this.Location.X, Cursor.Position.Y);
            }

            else if (movingCornerBottomRight)
            {
                this.Size = new Size(Cursor.Position.X - this.Location.X, Cursor.Position.Y - this.Location.Y);
            }

            else if (movingCornerBottomLeft)
            {
                this.Width = ((this.Width + this.Location.X) - Cursor.Position.X);
                this.Height = (Cursor.Position.Y - this.Location.Y);
                this.Location = new Point(Cursor.Position.X, this.Location.Y);
            }
        }

        private void stopResizer()
        {
            movingRight = false;
            movingLeft = false;
            movingTop = false;
            movingBottom = false;
            movingCornerTopRight = false;
            movingCornerTopLeft = false;
            movingCornerBottomRight = false;
            movingCornerBottomLeft = false;
            this.Cursor = Cursors.Default;
            System.Threading.Thread.Sleep(300);
            on_MinimumSize = false;
        }

        #endregion

        //private void timer1_Tick(object sender, EventArgs e)
        //{
        //    panel1.Width = this.Width - 6;
        //    panel1.Location = new Point(3, 3);
        //    buttonClos.Location = new Point(panel1.Width - 23, 3);
        //    buttonMax.Location = new Point(panel1.Width - 43, 3);
        //    buttonMin.Location = new Point(panel1.Width - 63, 3);
        //}

    }
}