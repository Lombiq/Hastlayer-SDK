using Hast.Layer;
using Hast.Transformer.Abstractions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace Hast.TransformerTest
{
    public class DefaultTransformerTest : ITransformer
    {
        public Task<IHardwareDescription> Transform(IEnumerable<string> assemblyPaths, IHardwareGenerationConfiguration configuration)
        {
            var transformerConfiguration = configuration.TransformerConfiguration();

            var firstAssemblyPath = assemblyPaths.First();
            var firstModule = new PEFile(firstAssemblyPath, PEStreamOptions.PrefetchEntireImage);
            var resolver = new UniversalAssemblyResolver(
                firstAssemblyPath,
                true,
                firstModule.Reader.DetectTargetFrameworkId(),
                PEStreamOptions.PrefetchMetadata);

            // When executed as a Windows service not all Hastlayer assemblies references' from transformed assemblies
            // will be found. Particularly loading Hast.Transformer.Abstractions seems to fail. Also, if a remote 
            // transformation needs multiple assemblies those will need to be loaded like this too.
            // So helping the decompiler find them here.
            resolver.AddSearchDirectory(Path.GetDirectoryName(GetType().Assembly.Location));
            //resolver.AddSearchDirectory(_appDataFolder.MapPath("Dependencies"));
            resolver.AddSearchDirectory(AppDomain.CurrentDomain.BaseDirectory);
            foreach (var assemblyPath in assemblyPaths.Select(path => Path.GetDirectoryName(path)).Distinct())
            {
                resolver.AddSearchDirectory(assemblyPath);
            }

            var decompilerSettings = new DecompilerSettings
            {
                AnonymousMethods = false
            };

            var typeSystem = new DecompilerTypeSystem(firstModule, resolver, decompilerSettings);

            // Need to use assembly names instead of paths for the ID, because paths can change (as in the random ones
            // with Remote Worker). Just file names wouldn't be enough because two assemblies can have the same simple
            // name while their full names being different.
            var rawTransformationId = string.Empty;
            //var assemblies = new List<AssemblyDefinition>();

            //foreach (var assemblyPath in assemblyPaths)
            //{
            //    var assembly = AssemblyDefinition.ReadAssembly(Path.GetFullPath(assemblyPath), parameters);
            //    rawTransformationId += "-" + assembly.FullName;
            //    assemblies.Add(assembly);
            //}

            //rawTransformationId +=
            //    string.Join("-", configuration.HardwareEntryPointMemberFullNames) +
            //    string.Join("-", configuration.HardwareEntryPointMemberNamePrefixes) +
            //    _jsonConverter.Serialize(configuration.CustomConfiguration) +
            //    // Adding the assembly name so the Hastlayer version is included too, to prevent stale caches after a 
            //    // Hastlayer update.
            //    GetType().Assembly.FullName;

            //var transformationId = Sha2456Helper.ComputeHash(rawTransformationId);

            //if (configuration.EnableCaching)
            //{
            //    var cachedTransformationContext = _transformationContextCacheService
            //        .GetTransformationContext(assemblyPaths, transformationId);

            //    if (cachedTransformationContext != null) return _engine.Transform(cachedTransformationContext);
            //}

            //var firstAssembly = assemblies.First();

            ////var decompiledContext = new DecompilerContext(firstAssembly.MainModule);

            var decompiler = new CSharpDecompiler(typeSystem, decompilerSettings);
            //decompiler.AddAssembly(firstAssembly);

            //foreach (var assembly in assemblies.Skip(1))
            //{
            //    decompiler.AddAssembly(assembly);
            //}

            var syntaxTree = decompiler.DecompileWholeModuleAsSingleFile();

            // Set this to true to save the unprocessed and processed syntax tree to files. This is useful for debugging
            // any syntax tree-modifying logic and also to check what an assembly was decompiled into.
            var saveSyntaxTree = true;
            if (saveSyntaxTree)
            {
                File.WriteAllText("UnprocessedSyntaxTree.cs", syntaxTree.ToString());
            }

            // We don't want to run all transforms since they would also transform some low-level constructs that are
            // useful to have as simple as possible (e.g. it's OK if we only have while statements in the AST, not for
            // statements mixed in). So we need to remove the problematic transforms.

            //decompiler.AstTransforms.
            //IEnumerable<IAstTransform> pipeline = TransformationPipeline.CreatePipeline(decompiledContext);
            //// We allow the commented out pipeline steps. Must revisit after an ILSpy update.
            //pipeline = pipeline
            //    // Converts e.g. !num6 == 0 expression to num6 != 0 and other simplifications.
            //    //.Without("PushNegation")

            //    // Re-creates delegates e.g. from compiler-generated DisplayClasses.
            //    //.Without("DelegateConstruction")

            //    // Re-creates e.g. for statements from while statements. Instead we use NoForPatternStatementTransform.
            //    .Without("PatternStatementTransform")
            //    .Union(new[] { new NoForPatternStatementTransform(decompiledContext) })

            //    // Converts e.g. num6 = num6 + 1; to num6 += 1.
            //    .Without("ReplaceMethodCallsWithOperators")

            //    // Deals with the unsafe modifier but we don't support PInvoke any way.
            //    .Without("IntroduceUnsafeModifier")

            //    // Re-adds checked() blocks that are used for compile-time overflow checking in C#, see:
            //    // https://msdn.microsoft.com/en-us/library/74b4xzyw.aspx. We don't need this for transformation.
            //    .Without("AddCheckedBlocks")

            //    // Merges separate variable declarations with variable initializations what would make transformation
            //    // more complicated.
            //    .Without("DeclareVariables")

            //    // Removes empty ctors or ctors that can be substituted with field initializers. Also breaks the ctors
            //    // of compiler-generated classes created from F# lambdas from by converting from this:
            //    //     public int input;
            //    //     public Run@32 (int input)
            //    //     {
            //    //         this.input = input;
            //    //         base..ctor();
            //    //     }
            //    //
            //    // To this:
            //    //     public int input = input;
            //    //     public Run@32 (int input)
            //    //     {
            //    //     }
            //    //.Without("ConvertConstructorCallIntoInitializer")

            //    // Converts decimal const fields to more readable variants, e.g. this:
            //    // [DecimalConstant (0, 0, 0u, 0u, 234u)]
            //    // private static readonly decimal a = 234m;
            //    // To this (which is closer to the original):
            //    // private const decimal a = 234m;
            //    //.Without("DecimalConstantTransform")

            //    // Adds using declarations that aren't needed for transformation.
            //    .Without("IntroduceUsingDeclarations")

            //    // Converts ExtensionsClass.ExtensionMethod(this) calls to this.ExtensionMethod(). This would make
            //    // the future transformation of extension methods difficult, since this makes them look like instance
            //    // methods (however those instance methods don't exist).
            //    .Without("IntroduceExtensionMethods")

            //    // These two deal with LINQ elements that we don't support yet any way.
            //    .Without("IntroduceQueryExpressions")
            //    .Without("CombineQueryExpressions")

            //    // Removes an unnecessary BlockStatement level from switch statements.
            //    //.Without("FlattenSwitchBlocks")
            //    ;
            //foreach (var transform in pipeline)
            //{
            //    transform.Run(syntaxTree);
            //}

            return null;
        }
    }
}
