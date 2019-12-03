using Hast.Common.Helpers;
using Hast.Layer;
using Hast.Transformer.Abstractions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.TransformerTest
{
    public class DefaultTransformerTest : ITransformer
    {
        public Task<IHardwareDescription> Transform(IEnumerable<string> assemblyPaths, IHardwareGenerationConfiguration configuration)
        {
            var transformerConfiguration = configuration.TransformerConfiguration();

            // Need to use assembly names instead of paths for the ID, because paths can change (as in the random ones
            // with Remote Worker). Just file names wouldn't be enough because two assemblies can have the same simple
            // name while their full names being different.
            var rawTransformationId = string.Empty;

            var decompilers = new List<CSharpDecompiler>();

            foreach (var assemblyPath in assemblyPaths)
            {
                var module = new PEFile(assemblyPath, PEStreamOptions.PrefetchEntireImage);
                rawTransformationId += "-" + module.FullName;

                var resolver = new UniversalAssemblyResolver(
                    Path.GetFullPath(assemblyPath),
                    true,
                    module.Reader.DetectTargetFrameworkId(),
                    PEStreamOptions.PrefetchMetadata);

                // When executed as a Windows service not all Hastlayer assemblies references' from transformed assemblies
                // will be found. Particularly loading Hast.Transformer.Abstractions seems to fail. Also, if a remote 
                // transformation needs multiple assemblies those will need to be loaded like this too.
                // So helping the decompiler find them here.
                resolver.AddSearchDirectory(Path.GetDirectoryName(GetType().Assembly.Location));
                //resolver.AddSearchDirectory(_appDataFolder.MapPath("Dependencies"));
                resolver.AddSearchDirectory(AppDomain.CurrentDomain.BaseDirectory);
                foreach (var searchPath in assemblyPaths.Select(path => Path.GetDirectoryName(path)).Distinct())
                {
                    resolver.AddSearchDirectory(searchPath);
                }

                var decompilerSettings = new DecompilerSettings
                {
                    AnonymousMethods = false,
                    // Turn off shorthand form of increment assignments. With this true e.g. x = x * 2 would be x *= 2. The
                    // former is easier to transform. Works in conjunction with the disabling of 
                    // ReplaceMethodCallsWithOperators, see below.
                    IntroduceIncrementAndDecrement = false
                };

                var typeSystem = new DecompilerTypeSystem(module, resolver, decompilerSettings);
                var decompiler = new CSharpDecompiler(typeSystem, decompilerSettings);

                // We don't want to run all transforms since they would also transform some low-level constructs that are
                // useful to have as simple as possible (e.g. it's OK if we only have while statements in the AST, not for
                // statements mixed in). So we need to remove the problematic transforms.
                // Must revisit after an ILSpy update.

                decompiler.ILTransforms
                    // Might need to be removed:
                    // - InlineReturnTransform creates returns with ternary operators and introduces multiple return 
                    //   statements.
                    // - TransformDisplayClassUsage creates local variables instead of assigning them to DisplayClasses.

                    // Converts simple while loops into for loops. However, all resulting loops are while (true) ones with
                    // a break statement inside.
                    .Remove<HighLevelLoopTransform>()
                    ;

                decompiler.AstTransforms
                    // Replaces op_* methods with operators but these methods are simpler to transform. Works in
                    // conjunction with IntroduceIncrementAndDecrement = false, see above.
                    .Remove<ReplaceMethodCallsWithOperators>()

                    // Converts e.g. num6 = num6 + 1; to num6 += 1.
                    .Remove("PrettifyAssignments")

                    // Deals with the unsafe modifier but we don't support PInvoke any way.
                    .Remove<IntroduceUnsafeModifier>()

                    // Re-adds checked() blocks that are used for compile-time overflow checking in C#, see:
                    // https://msdn.microsoft.com/en-us/library/74b4xzyw.aspx. We don't need this for transformation.
                    .Remove<AddCheckedBlocks>()

                    // Adds using declarations that aren't needed for transformation.
                    .Remove<IntroduceUsingDeclarations>()

                    // Converts ExtensionsClass.ExtensionMethod(this) calls to this.ExtensionMethod(). This would make
                    // the future transformation of extension methods difficult, since this makes them look like instance
                    // methods (however those instance methods don't exist).
                    .Remove<IntroduceExtensionMethods>()

                    // These two deal with LINQ elements that we don't support yet any way.
                    .Remove<IntroduceQueryExpressions>()
                    .Remove<CombineQueryExpressions>()
                    ;

                decompilers.Add(decompiler);
            }

            rawTransformationId +=
                string.Join("-", configuration.HardwareEntryPointMemberFullNames) +
                string.Join("-", configuration.HardwareEntryPointMemberNamePrefixes) +
                //_jsonConverter.Serialize(configuration.CustomConfiguration) +
                // Adding the assembly name so the Hastlayer version is included too, to prevent stale caches after a 
                // Hastlayer update.
                GetType().Assembly.FullName;

            var transformationId = Sha2456Helper.ComputeHash(rawTransformationId);

            //if (configuration.EnableCaching)
            //{
            //    var cachedTransformationContext = _transformationContextCacheService
            //        .GetTransformationContext(assemblyPaths, transformationId);

            //    if (cachedTransformationContext != null) return _engine.Transform(cachedTransformationContext);
            //}

            var decompilerTasks = decompilers
                .Select(decompiler => Task.Run(() => decompiler.DecompileWholeModuleAsSingleFile()))
                .ToArray();

            Task.WaitAll(decompilerTasks);

            // Unlike with the ILSpy v2 libraries multiple unrelated assemblies can't be decompiled into a single AST
            // so we need to decompile them separately and merge them like this.
            var syntaxTree = decompilerTasks[0].Result;
            for (int i = 1; i < decompilerTasks.Length; i++)
            {
                syntaxTree.Members.AddRange(decompilerTasks[i].Result.Members.Select(member => member.Detach()));
            }

            // Set this to true to save the unprocessed and processed syntax tree to files. This is useful for debugging
            // any syntax tree-modifying logic and also to check what an assembly was decompiled into.
            var saveSyntaxTree = true;
            if (saveSyntaxTree)
            {
                File.WriteAllText("UnprocessedSyntaxTree.cs", syntaxTree.ToString());
            }

            return null;
        }
    }
}
