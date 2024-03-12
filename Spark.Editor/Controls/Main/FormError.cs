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
    public partial class FormError : Form
    {
        public FormError()
        {
            InitializeComponent();
        }

        private FormError(Exception exception)
        {
            InitializeComponent();

            txtMessage.Text = exception.Message;
            txtStackTrace.Text = exception.StackTrace;
        }

        public static void Show(Exception exception)
        {
            using (FormError dlg = new FormError(exception))
            {
                dlg.ShowDialog();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }
    }
}
