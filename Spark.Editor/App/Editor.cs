﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spark;
using System.Windows.Forms;
using System.Collections;
using SharpDX;

namespace Spark.Editor
{
    public static class Editor
    {
        public static Options Options { get; set; }
        public static Directories Directories { get; set; }

        public static Camera MainCamera { get; private set; }
        public static Camera Camera1 { get; private set; }
        public static Camera Camera2 { get; private set; }
        public static Camera Camera3 { get; private set; }

        public static event Action SelectionChanged;

        private static object _selectedObject;

        public static object SelectedObject
        {
            get { return _selectedObject; }
            set
            {
                _selectedObject = value;

                if (SelectionChanged != null)
                    SelectionChanged();
            }
        }

        public static void LoadScene()
        {
            CreateCameras();
        }

        static void CreateCameras()
        {
            var camera = new Entity("MainCamera");
            MainCamera = camera.AddComponent<Camera>();
            camera.AddComponent<EditorCamera>();
            camera.Transform.Position = new Vector3(0, 5, -10);
            camera.Tag = 0;
            
            camera = new Entity("Camera1");
            Camera1 = camera.AddComponent<Camera>();
            camera.AddComponent<EditorCamera>();
            camera.Transform.Position = new Vector3(0, 5, -10);
            camera.Tag = 1;

            camera = new Entity("Camera2");
            Camera2 = camera.AddComponent<Camera>();
            camera.AddComponent<EditorCamera>();
            camera.Transform.Position = new Vector3(0, 5, -10);
            camera.Tag = 2;

            camera = new Entity("Camera3");
            Camera3 = camera.AddComponent<Camera>();
            camera.AddComponent<EditorCamera>();
            camera.Transform.Position = new Vector3(0, 5, -10);
            camera.Tag = 3;
        }

        static Editor()
        {
            Options = new Options();
            Directories = new Directories();
        }

    }

    public class Directories
    {
        public Directories()
        {
            Project = string.Empty;
        }

        public string Project { get; set; }

        public string Content
        {
            get { return System.IO.Path.Combine(Project, "Content\\"); }
        }

        public string Library
        {
            get { return System.IO.Path.Combine(Project, "Library\\"); }
        }

        public string GUI
        {
            get { return System.IO.Path.Combine(Project, "GUI\\"); }
        }

        public string Scenes
        {
            get { return System.IO.Path.Combine(Project, "Scenes\\"); }
        }

        public string Protocol
        {
            get { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "protocol\\"); }
        }

        public string Brushes
        {
            get { return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "brushes\\"); }
        }

        public void Create(string path)
        {
            Project = path;

            try
            {
                System.IO.Directory.Delete(Protocol, true);
            }
            catch { }

            System.Reflection.PropertyInfo[] infos = Reflector.GetProperties(GetType());

            foreach (System.Reflection.PropertyInfo  info in infos)
            {
                string value = info.GetValue(this, null).ToString();

                if (!System.IO.Directory.Exists(value))
                    System.IO.Directory.CreateDirectory(value);
            }
        }
    }
}
