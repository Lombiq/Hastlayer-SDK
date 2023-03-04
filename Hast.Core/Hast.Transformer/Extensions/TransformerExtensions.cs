using Hast.Common.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Transformer;

public enum Language
{
    CSharp,
    VisualBasic,
}

public static class TransformerExtensions
{
    public static Task<IHardwareDescription> TransformAsync(
        this ITransformer transformer,
        string sourceCode,
        Language dotNetLanguage,
        IHardwareGenerationConfiguration configuration,
        IHashProvider hashProvider)
    {
        CompilerResults result;
        var providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
        var parameters = new CompilerParameters
        {
            GenerateInMemory = false,
            TreatWarningsAsErrors = false,
            OutputAssembly = hashProvider.ComputeHash("DynamicHastAssembly", sourceCode),
        };

        switch (dotNetLanguage)
        {
            case Language.CSharp:
                using (var csharpCompiler = new CSharpCodeProvider(providerOptions))
                    result = csharpCompiler.CompileAssemblyFromSource(parameters, sourceCode);
                break;
            case Language.VisualBasic:
                using (var vbCompiler = new VBCodeProvider(providerOptions))
                    result = vbCompiler.CompileAssemblyFromSource(parameters, sourceCode);
                break;
            default:
                throw new ArgumentException("Unsupported .NET language.");
        }

        if (result.Errors.HasErrors)
        {
            var builder = new StringBuilder();
            foreach (var item in result.Errors) builder.Append(Environment.NewLine + item);
            throw new ArgumentException("The provided source code is invalid and has the following errors: " + builder);
        }

        return transformer.TransformAsync(new[] { result.CompiledAssembly }, configuration);
    }
}
