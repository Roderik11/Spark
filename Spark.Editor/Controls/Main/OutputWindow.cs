using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Spark.Windows;
using Spark;

namespace Spark.Editor
{
    public partial class OutputWindow : DockContent
    {
        public OutputWindow()
        {
            InitializeComponent();

            richTextBox1.KeyDown += new KeyEventHandler(richTextBox1_KeyDown);
            if (DesignMode) return;

           // Output.OnAppend += new GenericEventHandler<string>(Logfile_OnAppend);
        }

        void Logfile_OnAppend(string value)
        {
            if (richTextBox1.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate 
                {
                    richTextBox1.AppendText(value); 
                });
            }
            else
            {
                richTextBox1.AppendText(value);
            }
        }

        void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
        }
    }
}
