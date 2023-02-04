using Hast.Common.Services;
using Hast.Layer;
using Hast.Transformer.Abstractions;
using Hast.Transformer.Models;
using Hast.Transformer.Services;
using Hast.Transformer.Services.ConstantValuesSubstitution;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Medallion.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace Hast.Transformer;

public class DefaultTransformer : ITransformer
{
    // Set this to true to save the unprocessed and processed syntax tree to files. This is useful for debugging any
    // syntax tree-modifying logic and also to check what an assembly was decompiled into.
    private const bool SaveSyntaxTree =
#if DEBUG
    true;
#else
    false;
#endif

    // Used for turning off language features to make processing easier.
    private static readonly DecompilerSettings _decompilerSettings = new()
    {
        AlwaysShowEnumMemberValues = false,
        AnonymousMethods = false,
        AnonymousTypes = false,
        ArrayInitializers = false,
        Discards = false,
        DoWhileStatement = false,
        Dynamic = false,
        ExpressionTrees = false,
        // Instead of extension methods there are simple static methods.
        ExtensionMethods = false,
        ForStatement = false,
        IntroduceReadonlyAndInModifiers = true,
        IntroduceRefModifiersOnStructs = true,
        // Turn off shorthand form of increment assignments. With this true e.g. x = x * 2 would be x *= 2. The former
        // is easier to transform. Works in conjunction with the disabling of ReplaceMethodCallsWithOperators, see
        // below.
        IntroduceIncrementAndDecrement = false,
        LocalFunctions = false,
        NamedArguments = false,
        NonTrailingNamedArguments = false,
        NullPropagation = false,
        NullableReferenceTypes = false,
        OptionalArguments = false,
        OutVariables = false,
        PatternBasedFixedStatement = false,
        ReadOnlyMethods = true, // Can help const substitution.
        RefExtensionMethods = false,
        SeparateLocalVariableDeclarations = true,
        ShowXmlDocumentation = false,
        StringInterpolation = false,
        TupleComparisons = false,
        TupleConversions = false,
        TupleTypes = false,
        ThrowExpressions = false,
        UseExpressionBodyForCalculatedGetterOnlyProperties = false,
        UseLambdaSyntax = false,
        YieldReturn = false,
    };

    private readonly IEnumerable<IConverter> _converters;
    private readonly ISyntaxTreeCleaner _syntaxTreeCleaner;
    private readonly IConstantValuesSubstitutor _constantValuesSubstitutor;
    private readonly IAppDataFolder _appDataFolder;
    private readonly ITransformationContextCacheService _transformationContextCacheService;
    private readonly IKnownTypeLookupTableFactory _knownTypeLookupTableFactory;
    private readonly ITransformerPostProcessor _transformerPostProcessor;

    public DefaultTransformer(
        IEnumerable<IConverter> converters,
        ISyntaxTreeCleaner syntaxTreeCleaner,
        IConstantValuesSubstitutor constantValuesSubstitutor,
        IAppDataFolder appDataFolder,
        ITransformationContextCacheService transformationContextCacheService,
        IKnownTypeLookupTableFactory knownTypeLookupTableFactory,
        ITransformerPostProcessor transformerPostProcessor)
    {
        _converters = converters;
        _syntaxTreeCleaner = syntaxTreeCleaner;
        _constantValuesSubstitutor = constantValuesSubstitutor;
        _appDataFolder = appDataFolder;
        _transformationContextCacheService = transformationContextCacheService;
        _knownTypeLookupTableFactory = knownTypeLookupTableFactory;
        _transformerPostProcessor = transformerPostProcessor;
    }

    public async Task<IHardwareDescription> TransformAsync(IList<string> assemblyPaths, IHardwareGenerationConfiguration configuration)
    {
        var transformerConfiguration = configuration.TransformerConfiguration();

        // Need to use assembly names instead of paths for the ID, because paths can change. Just file names wouldn't be
        // enough because two assemblies can have the same simple name while their full names being different.
        var transformationIdComponents = new List<string>();

        var decompilers = new List<CSharpDecompiler>();

        foreach (var assemblyPath in assemblyPaths)
        {
            var module = new PEFile(assemblyPath, PEStreamOptions.PrefetchEntireImage);
            transformationIdComponents.Add(module.FullName);

            var resolver = new UniversalAssemblyResolver(
                Path.GetFullPath(assemblyPath),
                throwOnError: true,
                module.Reader.DetectTargetFrameworkId(),
                runtimePack: null,
                PEStreamOptions.PrefetchMetadata);

            // When executed as a Windows service not all Hastlayer assemblies references' from transformed assemblies
            // will be found. Particularly loading Hast.Transformer.Abstractions seems to fail. So helping the
            // decompiler find them here.
            resolver.AddSearchDirectory(Path.GetDirectoryName(GetType().Assembly.Location));
            var dependenciesFolderPath = _appDataFolder.MapPath("Dependencies");
            if (dependenciesFolderPath != null) resolver.AddSearchDirectory(dependenciesFolderPath);
            resolver.AddSearchDirectory(AppDomain.CurrentDomain.BaseDirectory);
            foreach (var searchPath in assemblyPaths.Select(Path.GetDirectoryName).Distinct())
            {
                resolver.AddSearchDirectory(searchPath);
            }

            var typeSystem = new DecompilerTypeSystem(module, resolver, _decompilerSettings);
            var decompiler = new CSharpDecompiler(typeSystem, _decompilerSettings);

            // We don't want to run all transforms since they would also transform some low-level constructs that are
            // useful to have as simple as possible (e.g. it's OK if we only have while statements in the AST, not for
            // statements mixed in). So we need to remove the problematic transforms. Must revisit after an ILSpy
            // update.

            decompiler.ILTransforms
                //// InlineReturnTransform might need to be removed: it creates returns with ternary operators and
                //// introduces multiple return statements.
                ////
                //// Converts simple while loops into for loops. However, all resulting loops are while (true) ones
                //// with a break statement inside.
                //// Not necessary to remove it with ForStatement = DoWhileStatement = false
                //// .Remove<HighLevelLoopTransform>()
                ////
                //// Creates local variables instead of assigning them to DisplayClasses. E.g. instead of:
                ////
                ////      ParallelAlgorithm.<> c__DisplayClass3_0 <> c__DisplayClass3_;
                ////      <> c__DisplayClass3_ = new ParallelAlgorithm.<> c__DisplayClass3_0();
                ////      <> c__DisplayClass3_.input = memory.ReadUInt32(0);
                ////
                //// ...we'd get:
                ////
                ////      uint input;
                ////      input = memory.ReadUInt32(0);
                ////      Func<object, uint> func = default(Func<object, uint>);
                ////      <> c__DisplayClass3_0 @object;
                ////
                //// Note that the DisplayClass is not instantiated either.
                .Remove("TransformDisplayClassUsage");

            decompiler.AstTransforms
                // Replaces op_* methods with operators but these methods are simpler to transform. Works in conjunction
                // with IntroduceIncrementAndDecrement = false, see above.
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

                // Converts ExtensionsClass.ExtensionMethod(this) calls to this.ExtensionMethod(). This would make the
                // future transformation of extension methods difficult, since this makes them look like instance
                // methods (however those instance methods don't exist).
                .Remove<IntroduceExtensionMethods>()

                // These two deal with LINQ elements that we don't support yet any way.
                .Remove<IntroduceQueryExpressions>()
                .Remove<CombineQueryExpressions>();

            decompilers.Add(decompiler);
        }

        // Decompiling and adding the syntax tree ensures that a change of code means a change of hash even if there was
        // no version change in the assembly.
        var syntaxTree = await DecompileTogetherAsync(decompilers);
        await WriteSyntaxTreeAsync(syntaxTree, "UnprocessedSyntaxTree.cs");
        transformationIdComponents.Add($"source code: {syntaxTree}");

        var transformationId = _transformationContextCacheService.BuildTransformationId(
            transformationIdComponents,
            configuration);

        if (configuration.EnableCaching &&
            await _transformationContextCacheService.ExecuteTransformationContextIfAnyAsync(
                assemblyPaths,
                transformationId) is { } hardwareDescription)
        {
            return hardwareDescription;
        }

        // Since this is about known (i.e. .NET built-in) types it doesn't matter which type system we use.
        var knownTypeLookupTable = _knownTypeLookupTableFactory.Create(decompilers.First().TypeSystem);
        var arraySizeHolder = ArraySizeHolder.FromConfiguration(configuration);

        var convertersByName = _converters.ToDictionary(converter => converter.Name ?? converter.GetType().Name);

        // This one needs additional configuration.
        new CustomConverter
        {
            Name = nameof(ConstantValuesSubstitutor),
            ConverterAction = (syntaxTree, configuration, knownTypeLookupTable) =>
            {
                if (!transformerConfiguration.EnableConstantSubstitution) return;
                _constantValuesSubstitutor.SubstituteConstantValues(
                    syntaxTree,
                    arraySizeHolder,
                    configuration,
                    knownTypeLookupTable);
            },
        }
            .WithDependency<UnneededReferenceVariablesRemover>()
            .AddToDictionary(convertersByName);

        var converters = convertersByName
            .Values
            .OrderTopologicallyBy(converter => converter.Dependencies.Select(name => convertersByName[name]));
        foreach (var converter in converters) converter.Convert(syntaxTree, configuration, knownTypeLookupTable);

        // If the conversions removed something let's clean them up here.
        _syntaxTreeCleaner.CleanUnusedDeclarations(syntaxTree, configuration);

        await WriteSyntaxTreeAsync(syntaxTree, "ProcessedSyntaxTree.cs");

        return await _transformerPostProcessor.PostProcessAsync(
            assemblyPaths,
            transformationId,
            syntaxTree,
            configuration,
            knownTypeLookupTable,
            arraySizeHolder);
    }

    // We'd get analyzer violations when SaveSyntaxTree is false in Release mode.
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
    private static async Task WriteSyntaxTreeAsync(SyntaxTree syntaxTree, string fileName)
    {
#pragma warning restore CS0162 // Remove unused parameter
#pragma warning disable CS0162 // Unreachable code detected
        while (SaveSyntaxTree)
        {
            try
            {
                await File.WriteAllTextAsync(fileName, syntaxTree.ToString());
                return;
            }
            catch (IOException)
            {
                // It's no big deal if we can't create it.
            }
        }
#pragma warning restore CS0162 // Unreachable code detected
#pragma warning restore IDE0079 // Remove unnecessary suppression
    }

    private static async Task<SyntaxTree> DecompileTogetherAsync(IEnumerable<CSharpDecompiler> decompilers)
    {
        var decompilerTasks = await Task.WhenAll(decompilers
            .Select(decompiler => Task.Run(() => decompiler.DecompileWholeModuleAsSingleFile(sortTypes: true))));

        // Unlike with the ILSpy v2 libraries multiple unrelated assemblies can't be decompiled into a single AST so we
        // need to decompile them separately and merge them like this.
        var syntaxTree = decompilerTasks[0];
        for (int i = 1; i < decompilerTasks.Length; i++)
        {
            syntaxTree.Members.AddRange(decompilerTasks[i].Members.Select(member => member.Detach()));
        }

        return syntaxTree;
    }
}
