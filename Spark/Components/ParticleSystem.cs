using System.Collections.Generic;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace Spark
{
    /// <summary>
    /// particle system using compute shader
    /// </summary>
    public class ParticleSystem : Component, IDraw, IUpdate
    {
        private ConstantBufferBox gsbuffer0;
        private ConstantBufferBox gsbuffer1;

        private GeometryShader gsShader;
        private ComputeShader csShader;
        private StructuredBuffer<Particle> pBuffer;
        private ConstantBufferBox cbuffer;
        private VertexBufferBinding emptyBinding;
        private Effect Effect;
        private float time;

        private int Stride;

        private struct Particle
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Color4 Color;
            //public float Age;
            //public float Scale;
        }

        public ParticleSystem()
        {
        }

        protected override void Awake()
        {
            const string geometryShaderSource = @"

            cbuffer cbChangesEveryFrame : register(b0) {
                float3 camPosition;
	            matrix viewMatrix;
	            float time;
            }

            cbuffer cbChangesOnResize : register(b1){
	            float quadLength;
	            matrix projMatrix;
            }

            struct GS_INPUT{
	            float4	pos		: SV_POSITION;
	            float3	color	: COLOR;
	            float2  tex0	: TEXCOORD0;
            };

            struct PS_INPUT{
	            float4	pos		: SV_POSITION;
	            float3	color	: COLOR;
	            float2  tex0	: TEXCOORD0;
            };

            [maxvertexcount(4)]
            void GS_Main(point GS_INPUT p[1], inout TriangleStream<PS_INPUT> triStream){
	            PS_INPUT p1 = (PS_INPUT)0;
	            /*
	            float4	viewPos			= float4(p[0].pos);
	            viewPos					= mul(viewPos, viewMatrix);
	            viewPos					/= viewPos.w;

	            float3 v					= viewPos.xyz + float3(1,1,0) * quadLength ;
	            float4 vTemp				= {v.xyz, 1.0f};
	            p1.pos						= mul(vTemp, projMatrix);
	            p1.tex0.x = 1.0f; p1.tex0.y = 0.0f;
	            triStream.Append(p1);

	            v							= viewPos.xyz + float3(1,-1,0) * quadLength ;
	            vTemp						= float4(v.xyz, 1.0f);
	            p1.pos						= mul(vTemp, projMatrix);
	            p1.tex0.x = 1.0f; p1.tex0.y = 1.0f;
	            triStream.Append(p1);

	            v							= viewPos.xyz + float3(-1,1,0) * quadLength;
	            vTemp						= float4(v.xyz, 1.0f);
	            p1.pos						= mul(vTemp, projMatrix);
	            p1.tex0.x = 0.0f; p1.tex0.y = 0.0f;
	            triStream.Append(p1);

	            v							= viewPos.xyz + float3(-1,-1,0) * quadLength ;
	            vTemp						= float4(v.xyz, 1.0f);
	            p1.pos						= mul(vTemp, projMatrix);
	            p1.tex0.x = 0.0f; p1.tex0.y = 1.0f;
	            triStream.Append(p1);
	            */

                float3 normal			= p[0].pos - camPosition;
	            normal					= mul(normal, viewMatrix);

                float3 rightAxis		= cross(float3(0.0f, 1.0f, 0.0f), normal);
                float3 upAxis			= cross(normal, rightAxis);
                rightAxis				= normalize(rightAxis);
                upAxis					= normalize(upAxis);

                float4 rightVector		= float4(rightAxis.xyz, 1.0f);
                float4 upVector         = float4(upAxis.xyz, 1.0f);
	            p[0].pos				= mul(p[0].pos, viewMatrix);

                p1.pos = p[0].pos+rightVector*(quadLength)+upVector*(quadLength);
                p1.tex0 = float2(1.0f, 0.0f);
                p1.pos = mul(p1.pos, projMatrix);
	            p1.color = p[0].color;
                triStream.Append(p1);

                p1.pos = p[0].pos+rightVector*(quadLength)+upVector*(-quadLength);
                p1.tex0 = float2(1.0f, 1.0f);
                p1.pos = mul(p1.pos, projMatrix);
	            p1.color = p[0].color;
                triStream.Append(p1);

                p1.pos = p[0].pos+rightVector*(-quadLength)+upVector*(quadLength);
                p1.tex0 = float2(0.0f, 0.0f);
                p1.pos = mul(p1.pos, projMatrix);
	            p1.color = p[0].color;
                triStream.Append(p1);

                p1.pos = p[0].pos+rightVector*(-quadLength)+upVector*(-quadLength);
                p1.tex0 = float2(0.0f, 1.0f);
                p1.pos = mul(p1.pos, projMatrix);
	            p1.color = p[0].color;
                triStream.Append(p1);
            }
            ";

            // Source code for the simple integration compute shader
            const string integrateSource = @"

                struct Particle
                {
                    float3 position;
                    float3 velocity;
                    float4 color;
                    //float age;
                };

                cbuffer params
                {
                    float elapsed;
                    float time;
                }

                RWStructuredBuffer<Particle> particles;

                [numthreads(32, 32, 1)]
                void Integrate(uint3 groupID : SV_GroupID, uint3 threadID : SV_DispatchThreadID)
                {
                    float id = threadID.x * 1024 + threadID.y;

                    float3 v = float3(0,0,0);
                    v.y = sin(time + id * 0.0002f) * 0.5f;
                    v.x = cos(time + id * 0.0003f) * 0.5f;
                    particles[id].position = particles[id].velocity + v;

                    float c = 0.5f + v.y;
                    particles[id].color = float4(c, c, c, 1);

                    //particles[id].velocity = float3(0, 1, 0) * time;
                    //particles[id].position += particles[id].velocity * elapsed;
                    //particles[id].age += elapsed;
                }
            ";

            emptyBinding = new VertexBufferBinding();
            Stride = Utilities.SizeOf<Particle>();
            Effect = new Effect("particles");

            // Compile compute shader
            using (ShaderBytecode integrateBytecode = ShaderBytecode.Compile(integrateSource, "Integrate", "cs_5_0", ShaderFlags.None, EffectFlags.None, ""))
            {
                csShader = new ComputeShader(Engine.Device, integrateBytecode);
                ShaderReflection reflection = new ShaderReflection(integrateBytecode);
                cbuffer = new ConstantBufferBox(reflection.GetConstantBuffer(0));
            }

            // Compile geometry shader
            //using (ShaderBytecode bc = ShaderBytecode.Compile(geometryShaderSource, "GS_Main", "gs_5_0", ShaderFlags.None, EffectFlags.None, ""))
            //{
            //    gsShader = new GeometryShader(Engine.Device, bc);
            //    ShaderReflection reflection = new ShaderReflection(bc);
            //    gsbuffer0 = new ConstantBufferBox(reflection.GetConstantBuffer(0));
            //    gsbuffer1 = new ConstantBufferBox(reflection.GetConstantBuffer(1));
            //}

            List<Particle> particles = new List<Particle>();

            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    for (int z = 0; z < 32; z++)
                    {
                        particles.Add(new Particle()
                        {
                            Position = new Vector3((float)x * 0.1f, (float)y * 0.1f, (float)z * 0.1f),
                            Velocity = new Vector3(x, y, z),
                            Color = Color.White
                        });
                    }
                }
            }

            pBuffer = new StructuredBuffer<Particle>(Engine.Device.ImmediateContext, particles);

            base.Awake();
        }

        public void Update()
        {
            time += Time.Delta;
            if (time > 36) time = 0;

            cbuffer.SetParameter("elapsed", Time.Delta);
            cbuffer.SetParameter("time", time);
            cbuffer.Commit();

            Engine.Device.ImmediateContext.ComputeShader.Set(csShader);
            Engine.Device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, pBuffer.UnorderedAccess);
            Engine.Device.ImmediateContext.ComputeShader.SetConstantBuffer(0, cbuffer.Buffer);
            Engine.Device.ImmediateContext.Dispatch(32, 32, 1);

            Engine.Device.ImmediateContext.ComputeShader.Set(null);
            Engine.Device.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, null);
        }

        public void Draw()
        {
            CommandBuffer.Enqueue(RenderPass.Transparent, DrawParticles);
        }

        private void DrawParticles()
        {
            Engine.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, emptyBinding);
            Engine.Device.ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;

            Effect.SetParameter("camPosition", Camera.Main.WorldPosition);
            Effect.SetParameter("viewMatrix", Camera.MainCamera.View);
            Effect.SetParameter("time", Time.Delta);
            Effect.SetParameter("quadLength", 10f);
            Effect.SetParameter("projMatrix", Camera.MainCamera.Projection);
            Effect.Apply();

            //gsbuffer0.SetParameter("camPosition", Camera.Main.WorldPosition);
            //gsbuffer0.SetParameter("viewMatrix", Camera.MainCamera.View);
            //gsbuffer0.SetParameter("time", Time.ElapsedSeconds);
            //gsbuffer0.Commit();

            //gsbuffer1.SetParameter("quadLength", 10f);
            //gsbuffer1.SetParameter("projMatrix", Camera.MainCamera.Projection);
            //gsbuffer1.Commit();

            //Engine.Device.ImmediateContext.GeometryShader.Set(gsShader);
            //Engine.Device.ImmediateContext.GeometryShader.SetConstantBuffer(0, gsbuffer0.Buffer);
            //Engine.Device.ImmediateContext.GeometryShader.SetConstantBuffer(1, gsbuffer1.Buffer);

            Engine.Device.ImmediateContext.VertexShader.SetShaderResource(0, pBuffer.ShaderResource);
            Engine.Device.ImmediateContext.Draw(pBuffer.Count, 0);
        }
    }

}