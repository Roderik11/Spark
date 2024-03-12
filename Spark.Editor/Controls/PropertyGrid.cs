using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using GFX = System.Drawing.Graphics;

namespace Spark.Editor
{
    public partial class PropertyGrid : UserControl
    {
        private System.Windows.Forms.PropertyGrid grid;
        private TableLayout table;
        private List<Label> cache = new List<Label>();

        private object _selectedObject;

        public object SelectedObject
        {
            get { return _selectedObject; }
            set
            {
                if (_selectedObject == value) return;
                _selectedObject = value;
                
               //BuildTable();
                BuildPanels();
               // BuildPropertygrid();
            }
        }

        public PropertyGrid()
        {
            InitializeComponent();
            DoubleBuffered = true;

            // table.CellPaint += tableLayoutPanel1_CellPaint;
        }

        private void BuildPropertygrid()
        {
            if (grid == null)
            {
                grid = new System.Windows.Forms.PropertyGrid();
                grid.Dock = DockStyle.Fill;
                Controls.Add(grid);
            }

            grid.SelectedObject = Editor.SelectedObject;
        }

        private void BuildTable()
        {
            if (table != null)
            {
                foreach (Control control in table.Controls)
                    ControlPool.ReleaseControl(control);

                table.Parent = null;
                table = null;
            }

            if (_selectedObject == null)
                return;

            table = new TableLayout();
            table.ColumnCount = 3;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.Dock = DockStyle.Top;
            table.AutoSize = true;
            table.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;

            table.SuspendLayout();

            PropertyInfo[] infos = Reflector.GetProperties(_selectedObject.GetType());
            // table.RowCount = infos.Length;

            int row = 0;

            foreach (PropertyInfo info in infos)
            {
                //table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                table.RowCount++;

                Label label1 = ControlPool.GetControl<Label>();
                label1.Text = info.Name;
                label1.TextAlign = ContentAlignment.MiddleLeft;
                label1.Dock = DockStyle.Left;

                TextBox label2 = ControlPool.GetControl<TextBox>();
                label2.Text = info.Name;
                label2.Dock = DockStyle.Fill;

                table.Controls.Add(label1, 1, row);
                table.Controls.Add(label2, 2, row);

                row++;
            }

            table.ResumeLayout(false);
            table.PerformLayout();

            Controls.Add(table);
        }

        private void BuildPanels()
        {
            SuspendLayout();

            foreach (Control control in Controls)
            {
                ControlPool.ReleaseControl(control);

                foreach(Control child in control.Controls)
                    ControlPool.ReleaseControl(child);
            }

            Controls.Clear();

            if (_selectedObject == null)
            {
                ResumeLayout(false);
                PerformLayout();
                return;
            }

            if (_selectedObject is Entity entity)
            {
                foreach (var component in entity.GetComponents())
                {
                    var map = Reflector.GetMapping(component.GetType());

                    foreach (var field in map)
                    {
                        if (!field.CanWrite) continue;
                        if (field.Ignored) continue;

                        Panel panel = ControlPool.GetControl<Panel>();
                        panel.Dock = DockStyle.Top;
                        panel.MinimumSize = new System.Drawing.Size(100, 28);
                        panel.AutoSize = true;

                        Label label1 = ControlPool.GetControl<Label>();
                        label1.Text = field.Name;
                        label1.Dock = DockStyle.Left;
                        //label1.AutoSize = true;

                        TextBox label2 = ControlPool.GetControl<TextBox>();
                        label2.Text = $"{field.GetValue(component)}";
                        label2.Dock = DockStyle.Fill;
                        panel.Controls.Add(label2);
                        panel.Controls.Add(label1);
                        Controls.Add(panel);
                    }

                    var header = ControlPool.GetControl<Label>();
                    header.Text = component.GetType().Name;
                    header.Dock = DockStyle.Top;
                    Controls.Add(header);
                }
            }
            else
            {
                PropertyInfo[] infos = Reflector.GetProperties(_selectedObject.GetType());

                for (int i = 0; i < 1; i++)
                {
                    foreach (PropertyInfo info in infos)
                    {
                        Panel panel = ControlPool.GetControl<Panel>();
                        panel.Dock = DockStyle.Top;
                        panel.MinimumSize = new System.Drawing.Size(100, 28);
                        panel.AutoSize = true;

                        Label label1 = ControlPool.GetControl<Label>();
                        label1.Text = info.Name;
                        label1.Dock = DockStyle.Left;
                        //label1.AutoSize = true;

                        TextBox label2 = ControlPool.GetControl<TextBox>();
                        label2.Text = info.Name;
                        label2.Dock = DockStyle.Fill;
                        panel.Controls.Add(label2);
                        panel.Controls.Add(label1);

                        Controls.Add(panel);
                    }
                }
            }

            ResumeLayout(false);
            PerformLayout();
        }

        private void tableLayoutPanel1_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            GFX g = e.Graphics;
            Rectangle r = e.CellBounds;

            using (Pen pen = new Pen(Color.CornflowerBlue, 0 /*1px width despite of page scale, dpi, page units*/ ))
            {
                pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Left;
                // define border style
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                
                //r.Offset(-1,-1);
                //r.Inflate(1, 1);

                // decrease border rectangle height/width by pen's width for last row/column cell
                //if (e.Row == (tableLayoutPanel1.RowCount - 1))
                //{
                //    r.Height -= 1;
                //}

                //if (e.Column == (tableLayoutPanel1.ColumnCount - 1))
                //{
                //    r.Width -= 1;
                //}

                // use graphics mehtods to draw cell's border
                e.Graphics.DrawRectangle(pen, r);
            }
        }
    }
}
