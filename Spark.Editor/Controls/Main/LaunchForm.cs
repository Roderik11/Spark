using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Spark.Editor
{
    public partial class LaunchForm : Form
    {
        public LaunchForm()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            // Hide();

            //CreateProjectWizard wizard = new CreateProjectWizard();
            //if (wizard.ShowDialog() == DialogResult.OK)
            //{
            //    MainForm form = new MainForm();
            //    form.Show();
            //}
            //else
            //    Show();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Project Files|*.project";
                dialog.AutoUpgradeEnabled = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Editor.Options.LastProjectPath = dialog.FileName;
                    Editor.Options.Save();

                    Editor.Directories.Project = System.IO.Path.GetDirectoryName(Editor.Options.LastProjectPath);
                    Editor.Directories.Create(Editor.Directories.Project);
                    DialogResult = DialogResult.OK;

                    Spark.Engine.Initialize("", true);
                    Spark.Engine.Assets.BaseDirectory = Editor.Directories.Content;
                }
            }
        }

        private void LaunchForm_Load(object sender, EventArgs e)
        {
            if (System.IO.File.Exists(Editor.Options.LastProjectPath))
            {
                btnLastProject.Visible = true;
                btnLastProject.Text = string.Format("Open last project: '{0}'", System.IO.Path.GetFileNameWithoutExtension(Editor.Options.LastProjectPath));
            }
            else btnLastProject.Visible = false;
        }

        private void btnLastProject_Click(object sender, EventArgs e)
        {
            Editor.Directories.Project = System.IO.Path.GetDirectoryName(Editor.Options.LastProjectPath);
            Editor.Directories.Create(Editor.Directories.Project);
            DialogResult = DialogResult.OK;
        }
    }
}
