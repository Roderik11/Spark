using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System.Collections;

namespace Spark
{
    public class EffectParameter2
    {
    }

    public class EffectTextureParameter : EffectParameter2
    {
        public int Slot;
        public int Count;
        public ShaderResourceView[] Textures;
    }

    public class ShaderParameter
    {
        public string Name;
        public object Value;

        internal Type Type;
        internal bool IsDirty;

        public ShaderParameter Clone()
        {
            ShaderParameter result = new ShaderParameter();
            result.Name = Name;
            result.Value = null;
            result.IsDirty = false;
            result.Type = Type;
            return result;
        }
    }
}

//    public class ShaderParameters
//    {
//        private Dictionary<string, ShaderParameter> data = new Dictionary<string, ShaderParameter>();

//        public ShaderParameters() { }

//        public bool SetValue(string name, object value)
//        {
//            if (!data.ContainsKey(name)) return false;
//            data[name].Value = value;
//            data[name].IsDirty = true;
//            return true;
//        }

//        internal void Add(ShaderParameter param)
//        {
//            if (data.ContainsKey(param.Name)) return;
//            data.Add(param.Name, param);
//        }

//        public IEnumerator GetEnumerator()
//        {
//            return data.Values.GetEnumerator();
//        }
//    }
//}