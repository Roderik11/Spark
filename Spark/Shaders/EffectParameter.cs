using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spark
{
    public enum UpdateRate
    {
        None,
        PerFrame,
        PerObject
    }

    public abstract class EffectParameter
    {
        public bool Annotated;

        public UpdateRate Frequency;

        public abstract object GetValue();

        public abstract void SetValue(object value);

        public abstract void Apply();

        public abstract void Commit();

        public SharpDX.Direct3D11.EffectVariable Variable { get; private set; }

        public virtual void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable) { }
    }

    public class EffectParameter<T> : EffectParameter
    {
        private bool changed;
        private T value;

        public T Value
        {
            get { return value; }
            set
            {
                this.value = value;
                changed = true;
            }
        }


        public override object GetValue()
        {
            return Value;
        }

        public override void SetValue(object value)
        {
            Value = (T)value;
        }

        public override void Apply()
        {
            if (changed || Frequency == UpdateRate.PerObject)
                Commit();

            changed = false;
        }

        public override void Commit() { }
    }

    public class BoolParameter : EffectParameter<bool>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class BoolArrayParameter : EffectParameter<bool[]>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class UIntParameter : EffectParameter<uint>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class UIntArrayParameter : EffectParameter<uint[]>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class IntParameter : EffectParameter<int>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class IntArrayParameter : EffectParameter<int[]>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class FloatParameter : EffectParameter<float>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class FloatArrayParameter : EffectParameter<float[]>
    {
        private SharpDX.Direct3D11.EffectScalarVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsScalar();
        }
    }

    public class MatrixParameter : EffectParameter<SharpDX.Matrix>
    {
        private SharpDX.Direct3D11.EffectMatrixVariable effectVar;

        public override void Commit()
        {
            effectVar.SetMatrix(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsMatrix();
        }
    }

    public class MatrixArrayParameter : EffectParameter<SharpDX.Matrix[]>
    {
        private SharpDX.Direct3D11.EffectMatrixVariable effectVar;

        public override void Commit()
        {
            effectVar.SetMatrix(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsMatrix();
        }
    }

    public class TextureParameter : EffectParameter<Texture>
    {
        private SharpDX.Direct3D11.EffectShaderResourceVariable effectVar;

        public override void Commit()
        {
            effectVar.SetResource(Value?.View);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsShaderResource();
        }
    }

    public class RWTextureParameter : EffectParameter<Texture>
    {
        private SharpDX.Direct3D11.EffectUnorderedAccessViewVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value?.UnorderedAccess);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsUnorderedAccessView();
        }
    }

    public class TextureListParameter : EffectParameter<List<Texture>>
    {
        private SharpDX.Direct3D11.EffectShaderResourceVariable effectVar;

        public override void Commit()
        {
            effectVar.SetResourceArray(Value?.ToResourceArray());
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsShaderResource();
        }
    }

    public class Vector2Parameter : EffectParameter<SharpDX.Vector2>
    {
        private SharpDX.Direct3D11.EffectVectorVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsVector();
        }
    }

    public class Vector2ArrayParameter : EffectParameter<SharpDX.Vector2[]>
    {
        private SharpDX.Direct3D11.EffectVectorVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsVector();
        }
    }

    public class Vector3Parameter : EffectParameter<SharpDX.Vector3>
    {
        private SharpDX.Direct3D11.EffectVectorVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsVector();
        }
    }

    public class Vector3ArrayParameter : EffectParameter<SharpDX.Vector3[]>
    {
        private SharpDX.Direct3D11.EffectVectorVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsVector();
        }
    }

    public class Vector4Parameter : EffectParameter<SharpDX.Vector4>
    {
        private SharpDX.Direct3D11.EffectVectorVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsVector();
        }
    }

    public class Vector4ArrayParameter : EffectParameter<SharpDX.Vector4[]>
    {
        private SharpDX.Direct3D11.EffectVectorVariable effectVar;

        public override void Commit()
        {
            effectVar.Set(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsVector();
        }
    }

    public class SamplerParameter : EffectParameter<SharpDX.Direct3D11.SamplerState>
    {
        private SharpDX.Direct3D11.EffectSamplerVariable effectVar;

        public override void Commit()
        {
            effectVar.SetSampler(0, Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsSampler();
        }
    }

    public class StructuredBufferParameter : EffectParameter<SharpDX.Direct3D11.ShaderResourceView>
    {
        private SharpDX.Direct3D11.EffectShaderResourceVariable effectVar;

        public override void Commit()
        {
            effectVar.SetResource(Value);
        }

        public override void Initialize(SharpDX.Direct3D11.EffectVariable effectVariable)
        {
            effectVar = effectVariable.AsShaderResource();
        }
    }
}
