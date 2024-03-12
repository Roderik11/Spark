using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squid;
using System.Collections;
using System.Diagnostics;
using Spark.Graph;
using System.Globalization;
using System.Reflection;
using System.Security.Policy;
using System.Runtime.InteropServices;

namespace Spark.Editor
{
    public class DebugControl : Frame
    {
        private readonly VirtualList VirtualList;
        private readonly Label lblDetails;
        private Frame toolbar;
        private SplitContainer split;
        private SearchBox searchbox;
        private Frame bottombar;

        public DebugControl()
        {
            Size = new Point(100, 100);

            toolbar = new Frame
            {
                Style = "frame",
                Size = new Point(16, 28),
                Dock = DockStyle.Top,
                Margin = new Margin(0, 0, 0, 1)
            };

            bottombar = new Frame
            {
                Style = "frame",
                Size = new Point(16, 28),
                Dock = DockStyle.Bottom,
                Margin = new Margin(0, 1, 0, 0)
            };

            searchbox = new SearchBox
            {
                Size = new Point(200, 16),
                Dock = DockStyle.Left,
                Margin = new Margin(28, 4, 4, 4)
            };

            lblDetails = new Label
            {
                Size = new Point(200, 100),
                Dock = DockStyle.Fill,
                Style = "multiline",
                TextWrap = true,
                BBCodeEnabled = true,
                LinkColor = ColorInt.ARGB(1, .2f, .6f, 1f),
                Leading = 4
            };
            lblDetails.LinkClicked += LblDetails_LinkClicked;

            split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.RetainAspect = false;
            split.Orientation = Orientation.Vertical;
            split.SplitFrame1.Size = new Point(200, 200);
            split.SplitButton.Size = new Point(4, 4);
            split.SplitButton.Style = "frame";
            split.SplitButton.Margin = new Margin(1, 0, 1, 0);

            VirtualList = new VirtualList();
            VirtualList.Dock = DockStyle.Fill;
            VirtualList.Scrollbar.ButtonDown.Visible = false;
            VirtualList.Scrollbar.ButtonUp.Visible = false;
            VirtualList.Scrollbar.Slider.Ease = false;
            VirtualList.Scrollbar.Slider.MinHandleSize = 64;
            VirtualList.CreateItem = CreateNode;
            VirtualList.BindItem = BindNode;
            VirtualList.ItemHeight = 28;
            VirtualList.DataSource = Debug.Lines;


            toolbar.Controls.Add(searchbox);

            split.SplitFrame1.Controls.Add(VirtualList);
            split.SplitFrame2.Controls.Add(lblDetails);

            Controls.Add(toolbar);
            Controls.Add(bottombar);
            Controls.Add(split);

            Debug.OnLog += Debug_OnLog;
        }

        private void LblDetails_LinkClicked(string href)
        {
            var parts = href.Split(':');
            string name = parts[0] + ":" + parts[1];
            int line = Convert.ToInt32(parts[2]);
            
            if(OpenFileAtLine(name, line))
                return;

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo("devenv.exe", $"/edit {name}");
            process.StartInfo = startInfo;
            process.Start();
        }

        private bool OpenFileAtLine(string file, int line)
        {
            try
            {
                object vs = Marshal.GetActiveObject("VisualStudio.DTE");
                object ops = vs.GetType().InvokeMember("ItemOperations", BindingFlags.GetProperty, null, vs, null);
                object window = ops.GetType().InvokeMember("OpenFile", BindingFlags.InvokeMethod, null, ops, new object[] { file });
                object selection = window.GetType().InvokeMember("Selection", BindingFlags.GetProperty, null, window, null);
                selection.GetType().InvokeMember("GotoLine", BindingFlags.InvokeMethod, null, selection, new object[] { line, true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Debug_OnLog(string arg1, System.Diagnostics.StackTrace arg2)
        {
            VirtualList.Refresh();
        }

        private void Result_MouseClick(Control sender, MouseEventArgs args)
        {
            var logentry = (Debug.LogEntry)sender.Tag;
            var text = MakePretty(logentry.StackTrace);
            lblDetails.Text = text;
        }

        private Control CreateNode(int index)
        {
            var result = new Button
            {
                Style = "item",
                Size = new Point(100, VirtualList.ItemHeight),
                Dock = DockStyle.Top,
                Text = Debug.Lines[index].Message,
                Tag = Debug.Lines[index],
            };

            result.MouseClick += Result_MouseClick;

            return result;
        }
        
        private string MakePretty(StackTrace trace)
        {
            string text = trace.GetFrames().StackFramesToString();
            string reg = "{0}:line {1}";

            for (int i = 0; i < trace.FrameCount; i++)
            {
                var frame = trace.GetFrame(i);
                var filename = frame.GetFileName();

                if (string.IsNullOrEmpty(filename))
                    continue;
            
                var line = frame.GetFileLineNumber();

                string find = string.Format(CultureInfo.InstalledUICulture, reg, filename, line);
                text = text.Replace(find, $"[url={filename}:{line}]{find}[/url]");
            }

            return text;
        }

        private void BindNode(Control control, int index)
        {
            var label = control as Button;
            label.Text = Debug.Lines[index].Message;
            label.Tag = Debug.Lines[index];
        }
    }
}
