using Hast.Common.Models;
using Hast.Layer;
using Hast.Synthesis.Services;
using Hast.Transformer.Models;
using Hast.Transformer.Services;
using Hast.Transformer.Vhdl.Models;
using Hast.Transformer.Vhdl.Tests.IntegrationTestingServices;
using Hast.Xilinx.Drivers;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.Tests;

public abstract class VhdlTransformingTestFixtureBase : IntegrationTestFixtureBase
{
    protected virtual bool UseStubMemberSuitabilityChecker => true;
    protected virtual string DeviceName => Nexys4DdrDriver.Nexys4Ddr;

    protected VhdlTransformingTestFixtureBase()
    {
        _hostConfiguration.Extensions = _hostConfiguration.Extensions
            .Union(new[]
                {
                    typeof(DefaultTransformer).Assembly,
                    typeof(MemberIdTable).Assembly,
                    typeof(IDeviceDriverSelector).Assembly,
                    typeof(Nexys4DdrDriver).Assembly,
                });

        _hostConfiguration.OnServiceRegistration += (_, services) =>
        {
            if (UseStubMemberSuitabilityChecker)
            {
                services.RemoveImplementations<IMemberSuitabilityChecker>();
                services.AddSingleton<IMemberSuitabilityChecker>(new StubMemberSuitabilityChecker());
            }
        };
    }

    protected virtual async Task<VhdlHardwareDescription> TransformAssembliesToVhdlAsync(
        ITransformer transformer,
        IList<Assembly> assemblies,
        Action<HardwareGenerationConfiguration> configurationModifier = null,
        string deviceName = null)
    {
        deviceName ??= DeviceName;
        var configuration = new HardwareGenerationConfiguration(deviceName) { EnableCaching = false };
        configurationModifier?.Invoke(configuration);
        return (VhdlHardwareDescription)await transformer.TransformAsync(assemblies, configuration);
    }

    private sealed class StubMemberSuitabilityChecker : IMemberSuitabilityChecker
    {
        public bool IsSuitableHardwareEntryPointMember(
            EntityDeclaration member,
            ITypeDeclarationLookupTable typeDeclarationLookupTable) =>
            (member.HasModifier(Modifiers.Public) && member.FindFirstParentTypeDeclaration().HasModifier(Modifiers.Public)) ||
            member.Modifiers == Modifiers.None;
    }
}
