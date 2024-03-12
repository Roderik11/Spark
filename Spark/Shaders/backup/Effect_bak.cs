//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using SharpDX;
//using SharpDX.Direct3D11;

//namespace Spark
//{
//    //public enum UpdateFrequency
//    //{
//    //    Immediate,
//    //    PerFrame
//    //}

//    public class Effect
//    {
//        public string Name;
//        public string Filename;
//        public ViewType Flag;
//        public InputLayout Layout;
//        public BlendState BlendState;
//        public DepthStencilState DepthStencilState;
//        public RasterizerState RasterizerState;
//        public ShaderParameters Parameters { get; protected set; }

//        protected Shader Shader;

//        public Effect() { }

//        public Effect(Shader shader)
//        {
//            Shader = shader;
//            Parameters = shader.CreateParameters();
//        }

//        public Effect(ShaderDescription description)
//        {
//            Shader = Shader.Create(description);
//            Parameters = Shader.CreateParameters();
//        }

//        public bool SetParameter<T>(string name, T value)
//        {
//            return Parameters.SetValue(name, value);
//        }

//        public bool SetTexture(string name, params Texture[] value)
//        {
//            return Parameters.SetValue(name, value);
//        }

//        public virtual void Apply()
//        {
//            Engine.Device.ImmediateContext.Rasterizer.State = RasterizerState;
//            //Engine.Device.ImmediateContext.OutputMerger.BlendState = BlendState;
//            //Engine.Device.ImmediateContext.OutputMerger.DepthStencilState = DepthStencilState;

//            Shader.Bind(Parameters);
//            Shader.Apply();
//        }
//    }

//    public class Effect_MeshOpaque : Effect
//    {
//        public Texture Diffuse { get; set; }
//        public Texture Normals { get; set; }
//        public Texture Attributes { get; set; }

//        public Effect_MeshOpaque()
//        {
//            Flag = ViewType.Opaque;

//            Shader = Shader.Create(new ShaderDescription
//            {
//                Flag = ViewType.Opaque,
//                Elements = VertexComplex.InputElements,
//                Filename = "mesh_opaque",
//                VsEntry = "VS",
//                PsEntry = "PS",
//                //HsEntry = "HS",
//                //DsEntry = "DS",
//            });

//            Parameters = Shader.CreateParameters();
//        }

//        public override void Apply()
//        {
//            SetTexture("Textures", Diffuse, Normals, Attributes);

//            base.Apply();
//        }
//    }

//    public class Effect_MeshOpaqueInstanced : Effect
//    {
//        public Texture Diffuse { get; set; }
//        public Texture Normals { get; set; }
//        public Texture Attributes { get; set; }

//        public Effect_MeshOpaqueInstanced()
//        {
//            Flag = ViewType.Opaque;
//            Shader = Shader.Create(new ShaderDescription
//            {
//                Flag = ViewType.Opaque,
//                Elements = VertexComplexInstanced.InputElements,
//                Filename = "mesh_opaque",
//                VsEntry = "VS_Instanced",
//                PsEntry = "PS"
//            });

//            Parameters = Shader.CreateParameters();
//        }

//        public override void Apply()
//        {
//            SetTexture("Textures", Diffuse, Normals, Attributes);

//            base.Apply();
//        }
//    }

//    public class Effect_SkyboxQpaque : Effect
//    {
//        public Texture Diffuse { get; set; }

//        public Effect_SkyboxQpaque()
//        {
//            Flag = ViewType.Opaque;
//            Shader = Shader.Create(new ShaderDescription
//            {
//                Flag = ViewType.Opaque,
//                Elements = VertexComplex.InputElements,
//                Filename = "mesh_skybox",
//                VsEntry = "VS",
//                PsEntry = "PS"
//            });

//            Parameters = Shader.CreateParameters();
//        }

//        public override void Apply()
//        {
//            SetTexture("texDiffuse", Diffuse);

//            base.Apply();
//        }
//    }
//}