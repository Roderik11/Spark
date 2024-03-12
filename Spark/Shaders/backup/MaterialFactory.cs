//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using SharpDX.D3DCompiler;
//using SharpDX.Direct3D11;
//using Refly.CodeDom;
//using System.IO;

//namespace Spark
//{
//    public class MaterialFactory
//    {
//        private class EffectParameter
//        {
//            public string FieldName;
//            public string PropertyName;
//            public string EffectVarName;

//            public Type PropertyType;
//            public EffectVariable Variable;

//            public override string ToString()
//            {
//                return FieldName;
//            }

//            public EffectParameter(EffectVariable var)
//            {
//                PropertyType = var.ToSparkType();
//                Variable = var.ToStrongType();

//                PropertyName = var.Description.Name;
//                FieldName = "_" + PropertyName.ToLower();
//                EffectVarName = "var" + FieldName;
//            }

//            public string GetCommitString()
//            {
//                if (Variable is EffectMatrixVariable)
//                    return string.Format("{0}.SetMatrix({1});", EffectVarName, FieldName);

//                if (Variable is EffectScalarVariable)
//                    return string.Format("{0}.Set({1});", EffectVarName, FieldName);

//                if (Variable is EffectVectorVariable)
//                    return string.Format("{0}.Set({1});", EffectVarName, FieldName);

//                if (Variable is EffectShaderResourceVariable)
//                {
//                    if(Variable.TypeInfo.Description.Elements > 0)
//                        return string.Format("{0}.SetResourceArray({1}.ToResourceArray());", EffectVarName, FieldName);
//                    else
//                        return string.Format("{0}.SetResource({1}.View);", EffectVarName, FieldName);
//                }

//                return null;
//            }
//        }

//        public void CreateShaderCode(string path, string outputPath)
//        {
//            string file = File.ReadAllText(path);
//            string errors = string.Empty;

//            ShaderBytecode bytecode = ShaderBytecode.CompileFromFile(path, "fx_5_0", ShaderFlags.Debug, EffectFlags.None, null, new ShaderInclude(), out errors);
//            SharpDX.Direct3D11.Effect effect = new SharpDX.Direct3D11.Effect(Engine.Device, bytecode);

//            string name = Path.GetFileNameWithoutExtension(path);

//            List<EffectParameter> parameters = new List<EffectParameter>();
//            int count = effect.Description.GlobalVariableCount;
//            for (int i = 0; i < count; i++)
//            {
//                EffectVariable var = effect.GetVariableByIndex(i);
//                EffectParameter param = new EffectParameter(var);

//                parameters.Add(param);
//            }

//            // create namespace
//            NamespaceDeclaration ns = new NamespaceDeclaration("Spark");

//            // adding imports
//            ns.AddImport("SharpDX");
//            ns.AddImport("SharpDX.Direct3D11");

//            // creating User class
//            ClassDeclaration proto = ns.AddClass(string.Format("Material{0}", name));
//            proto.Parent = new StringTypeDeclaration("Material");

//            foreach (EffectParameter param in parameters)
//            {
//                if (param.PropertyType == null) continue;
//                FieldDeclaration varfield = proto.AddField(param.Variable.GetType(), param.EffectVarName);
//            }

//            foreach (EffectParameter param in parameters)
//            {
//                if (param.PropertyType == null) continue;

//                FieldDeclaration field = proto.AddField(param.PropertyType, param.FieldName);
//                PropertyDeclaration prop = proto.AddProperty(field, true, true, false);
//                prop.Attributes = System.CodeDom.MemberAttributes.Public | System.CodeDom.MemberAttributes.Final;

//                //if (textures.ContainsKey(param.ParamName))
//                //    prop.CustomAttributes.Add(new System.CodeDom.CodeAttributeDeclaration("TextureSlotAttribute", new System.CodeDom.CodeAttributeArgument(new System.CodeDom.CodeSnippetExpression(textures[param.ParamName].ToString()))));

//                param.PropertyName = prop.Name;
//            }

//            FieldDeclaration enumfield = proto.AddField(string.Format("{0}_Technique", name), "_technique");
//            PropertyDeclaration enumprop = proto.AddProperty(enumfield, true, true, false);
//            enumprop.Attributes = System.CodeDom.MemberAttributes.Public | System.CodeDom.MemberAttributes.Final;

//            MethodDeclaration commit = proto.AddMethod("Apply");
//            commit.Attributes = System.CodeDom.MemberAttributes.Public | System.CodeDom.MemberAttributes.Override;
//            commit.Signature.AddParam(typeof(Renderer), "renderer", false);

//            //commit.Body.Add(new Refly.CodeDom.Statements.SnippetStatement(string.Format("EffectInstance.SetTechniqueByID((int){0});", enumprop.Name)));

//            foreach (EffectParameter param in parameters)
//            {
//                if (param.PropertyType == null) continue;

//                commit.Body.Add(new Refly.CodeDom.Statements.SnippetStatement(param.GetCommitString()));
//            }

//            commit.Body.Add(new Refly.CodeDom.Statements.SnippetStatement("Effect.GetTechniqueByIndex(0).GetPass(0).Apply();"));

//            EnumDeclaration en = proto.AddEnum(string.Format("{0}_Technique", name), false);
//            int tcount = effect.Description.TechniqueCount;

//            for (int i = 0; i < tcount; i++)
//                en.AddField(effect.GetTechniqueByIndex(i).Description.Name, i);

//            effect.Dispose();

//            Output(ns, @"E:\");
//        }

//        public void Output(NamespaceDeclaration ns, string path)
//        {
//            // output result
//            CodeGenerator gen = new CodeGenerator();
//            gen.Options.VerbatimOrder = true;
//            gen.Options.BlankLinesBetweenMembers = false;

//            // output to C#
//            gen.Provider = CodeGenerator.CsProvider;
//            gen.GenerateCode(path, ns);
//        }
//    }
//}