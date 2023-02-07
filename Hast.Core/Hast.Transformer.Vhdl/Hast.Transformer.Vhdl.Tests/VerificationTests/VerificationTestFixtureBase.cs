using Hast.Common.Models;
using Hast.Layer;
using Hast.Transformer.Abstractions;
using Hast.Transformer.Vhdl.Abstractions.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Tests.VerificationTests;

public abstract class VerificationTestFixtureBase : VhdlTransformingTestFixtureBase
{
    // Uncomment to make Shouldly open KDiff for diffing (or something else than what comes from the default order:
    // https://github.com/VerifyTests/DiffEngine/blob/main/docs/diff-tool.order.md). Note that until the
    // https://github.com/VerifyTests/DiffEngine/issues/267 issue is fixed, if KDiff was installed into the path
    // %ProgramFiles%\KDiff3\bin\kdiff3.exe for you, you'll need to add a symlink to the file under
    // %ProgramFiles%\KDiff3.
    ////static VerificationTestFixtureBase() => DiffEngine.DiffTools.UseOrder(true, DiffEngine.DiffTool.KDiff3);

    protected override Task<VhdlHardwareDescription> TransformAssembliesToVhdlAsync(
        ITransformer transformer,
        IList<Assembly> assemblies,
        Action<HardwareGenerationConfiguration> configurationModifier = null,
        string deviceName = null) =>
        base.TransformAssembliesToVhdlAsync(
            transformer,
            assemblies,
            configuration =>
            {
                configuration.VhdlTransformerConfiguration().VhdlGenerationConfiguration =
                    VhdlGenerationConfiguration.Debug;
                configurationModifier?.Invoke(configuration);
            },
            deviceName);
}
