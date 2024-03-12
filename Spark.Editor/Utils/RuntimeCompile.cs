using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using System.Reflection;

namespace Spark.Editor
{
    public static class RuntimeCompile
    {
        public static string TempDir = AppDomain.CurrentDomain.BaseDirectory + "cache\\";
        public static string CompiledDir = TempDir + "cmp\\";
        public static string SourceDir = TempDir + "src\\";

        private static Dictionary<string, Assembly> ScriptAssemblies = new Dictionary<string, Assembly>();
        private static Dictionary<string, Assembly> ShaderAssemblies = new Dictionary<string, Assembly>();

        public static Component CreateScript(string filename)
        {
            string key = Path.GetFileNameWithoutExtension(filename);

            if (ScriptAssemblies.ContainsKey(key))
            {
                Component instance = ScriptAssemblies[key].CreateInstance(key) as Component;
                //if (instance != null)
                //    instance.Filename = Path.GetFileName(filename);

                return instance;
            }

            CompilerResults compile = CompileScript(filename);

            if (compile.Errors.HasErrors)
            {
                Debug.Log(string.Format("Script compile errors in: {0}", key));

                foreach (CompilerError ce in compile.Errors)
                    Debug.Log(string.Format("Error {0} Ln{2} {1}", ce.ErrorNumber, ce.ErrorText, ce.Line, ce));

                return null;
            }

            return compile.CompiledAssembly.CreateInstance(key) as Component;
        }

        //public static Shader ReCreateShader(string filename)
        //{
        //    string key = "Effect_" + Path.GetFileNameWithoutExtension(filename);
        //    Shader result = null;

        //    ShaderAssemblies.Remove(key);

        //    Trashcan can = new Trashcan();
        //    can.CreateShaderCode(filename, SourceDir);

        //    CompilerResults compile = CompileShader(SourceDir + key + ".cs");

        //    if (compile.Errors.HasErrors)
        //    {
        //        Debug.Log(string.Format("Shader compile errors in: {0}", key));

        //        foreach (CompilerError ce in compile.Errors)
        //            Debug.Log(string.Format("Error {0} Ln{2} {1}", ce.ErrorNumber, ce.ErrorText, ce.Line, ce));

        //        return null;
        //    }

        //    result = compile.CompiledAssembly.CreateInstance("Shaders." + key) as Shader;

        //    if (result != null) result.Filename = Path.GetFileName(filename);

        //    return result;
        //}

        //public static Shader CreateShader(string filename)
        //{
        //    string key = "Effect_" + Path.GetFileNameWithoutExtension(filename);
        //    Shader result = null;

        //    if (ShaderAssemblies.ContainsKey(key))
        //    {
        //        result = ShaderAssemblies[key].CreateInstance("Shaders." + key) as Shader;
        //        if (result != null) result.Filename = Path.GetFileName(filename);
        //        return result;
        //    }

        //    Trashcan can = new Trashcan();
        //    can.CreateShaderCode(filename, SourceDir);

        //    CompilerResults compile = CompileShader(SourceDir + key + ".cs");

        //    if (compile.Errors.HasErrors)
        //    {
        //        Debug.Log(string.Format("Compile errors in: {0}", key));

        //        foreach (CompilerError ce in compile.Errors)
        //            Debug.Log(string.Format("Error {0} Ln{2} {1}", ce.ErrorNumber, ce.ErrorText, ce.Line, ce));

        //        return null;
        //    }

        //    result = compile.CompiledAssembly.CreateInstance("Shaders." + key) as Shader;

        //    if (result != null) result.Filename = Path.GetFileName(filename);

        //    return result;
        //}

        public static CompilerResults CompileScript(string filename)
        {
            string key = Path.GetFileNameWithoutExtension(filename);

            CompilerParameters compilerParams = new CompilerParameters();
            string outfile = CompiledDir + Path.GetFileNameWithoutExtension(filename) + "Assembly.dll";
            string code = File.ReadAllText(filename);

            compilerParams.GenerateInMemory = true;
            compilerParams.TreatWarningsAsErrors = false;
            compilerParams.GenerateExecutable = false;
            compilerParams.CompilerOptions = "/optimize";
            compilerParams.OutputAssembly = outfile;

            string[] references = { "System.dll", "Overdose.dll", "MTV3D65.dll" };

            compilerParams.ReferencedAssemblies.AddRange(references);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults result = provider.CompileAssemblyFromSource(compilerParams, code);

            if (!result.Errors.HasErrors)
            {
                Debug.Log(string.Format("Script compiled: {0}", Path.GetFileName(filename)));

                if (!ScriptAssemblies.ContainsKey(key))
                    ScriptAssemblies.Add(key, result.CompiledAssembly);
                else
                    ScriptAssemblies[key] = result.CompiledAssembly;
            }
            else
            {
                Debug.Log(string.Format("Script compile error in: {0}", Path.GetFileName(filename)));
            }

            return result;
        }

        public static CompilerResults CompileScript(string filename, string code)
        {
            string key = Path.GetFileNameWithoutExtension(filename);

            CompilerParameters compilerParams = new CompilerParameters();
            string outfile = CompiledDir + Path.GetFileNameWithoutExtension(filename) + "Assembly.dll";

            compilerParams.GenerateInMemory = true;
            compilerParams.TreatWarningsAsErrors = false;
            compilerParams.GenerateExecutable = false;
            compilerParams.CompilerOptions = "/optimize";
            compilerParams.OutputAssembly = outfile;

            string[] references = { "System.dll", "Overdose.dll", "MTV3D65.dll" };

            compilerParams.ReferencedAssemblies.AddRange(references);

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults result = provider.CompileAssemblyFromSource(compilerParams, code);

            if (!result.Errors.HasErrors)
            {
                Debug.Log(string.Format("Script compiled: {0}", Path.GetFileName(filename)));

                if (!ScriptAssemblies.ContainsKey(key))
                    ScriptAssemblies.Add(key, result.CompiledAssembly);
                else
                    ScriptAssemblies[key] = result.CompiledAssembly;
            }
            else
            {
                Debug.Log(string.Format("Script compile error in: {0}", Path.GetFileName(filename)));
            }

            return result;
        }

        //public static CompilerResults CompileShader(string filename)
        //{
        //    string key = Path.GetFileNameWithoutExtension(filename);

        //    CompilerParameters compilerParams = new CompilerParameters();
        //    string outfile = CompiledDir + Path.GetFileNameWithoutExtension(filename) + "Assembly.dll";
        //    string code = File.ReadAllText(filename);

        //    compilerParams.GenerateInMemory = true;
        //    compilerParams.TreatWarningsAsErrors = false;
        //    compilerParams.GenerateExecutable = false;
        //    compilerParams.CompilerOptions = "/optimize";
        //    compilerParams.OutputAssembly = outfile;

        //    string[] references = { "System.dll", "Overdose.dll", "MTV3D65.dll" };

        //    compilerParams.ReferencedAssemblies.AddRange(references);

        //    CSharpCodeProvider provider = new CSharpCodeProvider();
        //    CompilerResults result = provider.CompileAssemblyFromSource(compilerParams, code);

        //    if (!result.Errors.HasErrors)
        //    {
        //        Debug.Log(string.Format("Shader compiled: {0}", Path.GetFileName(filename)));

        //        if (!ShaderAssemblies.ContainsKey(key))
        //            ShaderAssemblies.Add(key, result.CompiledAssembly);
        //        else
        //            ShaderAssemblies[key] = result.CompiledAssembly;
        //    }
        //    else
        //    {
        //        Debug.Log(string.Format("Shader compile error in: {0}", Path.GetFileName(filename)));
        //    }

        //    return result;
        //}

        //public static void CompileAllScripts()
        //{
        //    if (!Directory.Exists(CompiledDir))
        //        Directory.CreateDirectory(CompiledDir);

        //    AppDomain.CurrentDomain.AppendPrivatePath(CompiledDir);
        //    AppDomain.CurrentDomain.SetShadowCopyFiles();

        //    string[] files = Directory.GetFiles(Editor.ContentDirectory, "*.cs", SearchOption.AllDirectories);
        //    foreach (string file in files)
        //        CompileScript(file);
        //}

        //public static void CompileAllShaders()
        //{
        //    if (!Directory.Exists(SourceDir))
        //        Directory.CreateDirectory(SourceDir);

        //    string[] files = Directory.GetFiles(Editor.ContentDirectory, "*.fx", SearchOption.AllDirectories);

        //    Trashcan can = new Trashcan();

        //    foreach (string file in files)
        //        can.CreateShaderCode(file, SourceDir);

        //    files = Directory.GetFiles(SourceDir, "*.cs", SearchOption.TopDirectoryOnly);
        //    foreach (string file in files)
        //        CompileShader(file);
        //}

        public static void DeleteAssemblies()
        {
            if (Directory.Exists(CompiledDir))
                Directory.Delete(CompiledDir, true);
        }
    }
}
