using System;
using System.Diagnostics;
using System.Windows.Forms;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System.IO;
using System.Collections.Generic;
using SharpDX.Direct2D1;

namespace Spark
{
    public delegate void DrawTextHandler(string text, SharpDX.Vector3 position);

    public delegate void EngineTickHandler(float deltaTime);

    public static class Engine
    {
        internal static SharpDX.DXGI.Factory Factory;

        private static Form Form;
        private static float PhysicsUpdateInterval = 1f / 120f;
        private static float physicsTimerAccum;
        private static int tickCount;

        public static AssetManager Assets { get; private set; }
        public static SharpDX.Direct3D11.Device Device { get; private set; }

        public static SharpDX.WIC.ImagingFactory2 WicFactory { get; private set; }


        public static bool IsEditor { get; private set; }
        public static bool Running { get; private set; }
        public static EngineSettings Settings { get; private set; }
        public static Material DefaultMaterial { get; private set; }

        public static string RootDirectory { get; private set; }

        public static string ResourceDirectory { get; private set; }

        public static event DrawTextHandler OnDrawText;

        public static int TickCount => tickCount;
        //public static event EngineTickHandler OnTick;

        static Engine()
        {
            Settings = new EngineSettings();
            //  SharpDX.Configuration.EnableReleaseOnFinalizer = false;
        }

        public static void DrawText(string text, SharpDX.Vector3 position)
        {
            OnDrawText?.Invoke(text, position);
        }

        static DirectoryInfo CreateDirectory(string path)
        {
            var dir = Path.Combine(RootDirectory, path);

            if (!Directory.Exists(dir))
                return Directory.CreateDirectory(dir);

            return new DirectoryInfo(dir);
        }

        public static void Initialize(string rootPath, bool editor = false)
        {
            IsEditor = editor;
#if DEBUG
            Device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug | DeviceCreationFlags.BgraSupport);
#else
            Device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);
#endif

            //Device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            SharpDX.DXGI.Device dxgidevice = Device.QueryInterface<SharpDX.DXGI.Device>();
            SharpDX.DXGI.Adapter adpater = dxgidevice.GetParent<SharpDX.DXGI.Adapter>();
            Factory = adpater.GetParent<SharpDX.DXGI.Factory>();
            WicFactory = new SharpDX.WIC.ImagingFactory2();

            RootDirectory = rootPath;
            ResourceDirectory = Path.Combine(rootPath, "Resources\\");
            CreateDirectory("Resources\\");

            Assets = new AssetManager(Device);
            Assets.BaseDirectory = ResourceDirectory;

            AssetDatabase.Initialize();
            AssetDatabase.InitializeProject(editor);

            Texture diffuse = Assets.Load<Texture>("checker_grey.dds");
            Texture normal = Assets.Load<Texture>("DefaultNormalMap.dds");
            Texture aoc = Assets.Load<Texture>("white.dds");
            Texture data = Assets.Load<Texture>("DefaultSpecularMap.dds");

            var effect = new Effect("mesh_opaque");
            effect.BlendState = States.BlendNone;
            effect.DepthStencilState = States.ZReadZWriteNoStencil;

            DefaultMaterial = new Material(effect);
            DefaultMaterial.UseInstancing = true;
            DefaultMaterial.Name = "Default Material";
            DefaultMaterial.SetValue("Albedo", diffuse);
            DefaultMaterial.SetValue("Normal", normal);
            DefaultMaterial.SetValue("Occlusion", aoc);
            DefaultMaterial.SetValue("Data", data);
            DefaultMaterial.SetValue("sampData", Samplers.WrappedAnisotropic);
        }

        public static void Run(Form form)
        {
            Form = form;
            Running = true;

            Input.ClearKeyCache();

            Application.Idle += Application_Idle;
            Application.Run(Form);
        }

        private static List<RenderView> renderViews = new List<RenderView>();
        
        public static void Tick()
        {
            // idle end
            Profiler.Update();
            System.Threading.Interlocked.Increment(ref tickCount);

            // physics update at fixed interval
            physicsTimerAccum += Time.SmoothDelta;
            if (physicsTimerAccum >= PhysicsUpdateInterval)
            {
                int loop = (int)(physicsTimerAccum / PhysicsUpdateInterval);

                for (int i = 0; i < loop; i++)
                    Entity.Space.UpdatePhysics(PhysicsUpdateInterval);

                physicsTimerAccum %= PhysicsUpdateInterval;
            }

            var cameras = Entity.GetActiveCameras();
            // find first enabled camera (main camera)
            if (cameras.Count > 0)
            {
                cameras[0].MakeMain();
                cameras[0].Update();
            }

            Entity.Space.UpdateEntities();

            renderViews.Clear();
            renderViews.AddRange(RenderView.All);

            // foreach viewport
            //  foreach camera drawing to that viewport
            //      render camera
            foreach (var viewport in renderViews)
            {
                if (!viewport.Enabled) continue;

                if (viewport.Handle != IntPtr.Zero)
                    Input.SetHandle(viewport.Handle);

                if (viewport.OnRender != null)
                {
                    viewport.OnRender(viewport);
                }
                else
                {
                    viewport.Prepare();

                    foreach (var camera in cameras)
                    {
                        if (camera.Target == viewport)
                            camera.Render();
                    }

                    viewport.Present();
                }
            }

            Input.ClearKeyCache();

            Time.EndTime();
            Time.StartTime();
        }

        private static void Application_Idle(object sender, EventArgs e)
        {
            while (NativeMethods.AppStillIdle)
            {
                if (Running)
                {
                    Tick();
                    System.Threading.Thread.Sleep(0);
                }
                else
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }

    public class EngineSettings
    {
        public bool VSync { get; set; }
        public bool BoundingBoxes { get; set; }
        public bool PhysXShapes { get; set; }
        public bool VisibilityTree { get; set; }
        public bool SSAO { get; set; }
        public bool GlobalWater { get; set; }
        public bool Fog { get; set; }

    }
}