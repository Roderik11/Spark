using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Spark.Editor
{
    public struct PromptResult
    {
        public DialogResult DialogResult;
        public string Value;

        public PromptResult(DialogResult result, string value)
        {
            DialogResult = result;
            Value = value;
        }
    }

    public partial class Prompt : Form
    {
        public string HelpText
        {
            get { return lblHelp.Text; }
            set { lblHelp.Text = value; }
        }

        public string Value
        {
            get { return txtText.Text; }
            set { txtText.Text = value; }
        }

        public string Title
        {
            get { return Text; }
            set { Text = value; }
        }

        public Prompt()
        {
            InitializeComponent();
        }

        public static PromptResult Show(string title, string help, string text)
        {
            Prompt box = new Prompt();

            box.Title = title;
            box.HelpText = help;
            box.Value = text;
            box.txtText.SelectAll();
            return new PromptResult(box.ShowDialog(), box.Value);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void txtText_TextChanged(object sender, EventArgs e)
        {
            btnOK.Enabled = txtText.Text != string.Empty;
        }
    }
}