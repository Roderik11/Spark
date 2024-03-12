namespace Spark.Editor
{
    partial class Viewport
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Viewport
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 264);
            this.CloseButton = false;
            this.CloseButtonVisible = false;
            this.DockAreas = ((Spark.Windows.DockAreas)(((((Spark.Windows.DockAreas.DockLeft | Spark.Windows.DockAreas.DockRight)
                        | Spark.Windows.DockAreas.DockTop)
                        | Spark.Windows.DockAreas.DockBottom)
                        | Spark.Windows.DockAreas.Document)));
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Viewport";
            this.Text = "Viewport";
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Viewport_MouseDown);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
