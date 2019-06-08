using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PaginaRota
{
    class RuntimeCompiler
    {
        CompilerResults results;
        string sourceCode;

        public RuntimeCompiler(string source)
        {
            this.sourceCode = source;
        }

        public Assembly GetAssembly()
        {
            return results.CompiledAssembly;
        }

        public CompilerResults CompileSourceCodeDom()
        {
            CodeDomProvider cpd = new CSharpCodeProvider();
            var cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.GenerateExecutable = false;
            results = cpd.CompileAssemblyFromSource(cp, this.sourceCode);
            
            return results;
        }

        public object ExecuteFromAssembly(Assembly assembly, string type, string method)
        {
            Type fooType = assembly.GetType(type);
            MethodInfo printMethod = fooType.GetMethod(method);
            object foo = assembly.CreateInstance(type);
            return printMethod.Invoke(foo, BindingFlags.InvokeMethod, null, null, CultureInfo.CurrentCulture);
        }

    }
}