using Hast.Transformer.Vhdl.Constants;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.ArchitectureComponents;

[System.Diagnostics.DebuggerDisplay("{Name}")]
public abstract class ArchitectureComponentBase : IArchitectureComponent
{
    public string Name { get; private set; }
    public IList<Variable> LocalVariables { get; private set; } = new List<Variable>();
    public IList<Alias> LocalAliases { get; private set; } = new List<Alias>();
    public IList<AttributeSpecification> LocalAttributeSpecifications { get; } = new List<AttributeSpecification>();
    public IList<Variable> GlobalVariables { get; private set; } = new List<Variable>();
    public IList<Signal> InternallyDrivenSignals { get; private set; } = new List<Signal>();
    public IList<Signal> ExternallyDrivenSignals { get; private set; } = new List<Signal>();
    public IList<AttributeSpecification> GlobalAttributeSpecifications { get; } = new List<AttributeSpecification>();

    public IDictionary<EntityDeclaration, int> OtherMemberMaxInvocationInstanceCounts { get; private set; } =
        new Dictionary<EntityDeclaration, int>();

    public DependentTypesTable DependentTypesTable { get; private set; } = new DependentTypesTable();

    protected readonly IList<IMultiCycleOperation> _multiCycleOperations = new List<IMultiCycleOperation>();
    public IEnumerable<IMultiCycleOperation> MultiCycleOperations => _multiCycleOperations;

    protected ArchitectureComponentBase(string name) => Name = name;

    public abstract IVhdlElement BuildDeclarations();

    public abstract IVhdlElement BuildBody();

    protected IBlockElement BuildDeclarationsBlock(IVhdlElement beginWith = null, IVhdlElement endWith = null)
    {
        var declarationsBlock = new LogicalBlock(new LineComment(Name + " declarations start"));

        if (beginWith != null)
        {
            declarationsBlock.Add(beginWith);
        }

        if (InternallyDrivenSignals.Any() || ExternallyDrivenSignals.Any())
        {
            declarationsBlock.Add(new LineComment("Signals:"));
        }

        declarationsBlock.Body.AddRange(InternallyDrivenSignals.Union(ExternallyDrivenSignals));

        foreach (var variable in GlobalVariables)
        {
            variable.Shared = true;
        }

        if (GlobalVariables.Any())
        {
            declarationsBlock.Add(new LineComment("Shared (global) variables:"));
        }

        declarationsBlock.Body.AddRange(GlobalVariables);

        declarationsBlock.Body.AddRange(GlobalAttributeSpecifications);

        if (endWith != null)
        {
            declarationsBlock.Add(endWith);
        }

        declarationsBlock.Add(new LineComment(Name + " declarations end"));

        return declarationsBlock.Body.Count > 2 ? declarationsBlock : new InlineBlock();
    }

    protected Process BuildProcess(IVhdlElement notInReset, IVhdlElement inReset = null)
    {
        var process = new Process { Name = Name.ToExtendedVhdlId() };

        process.Declarations.AddRange(LocalVariables.Cast<IVhdlElement>().ToList());
        process.Declarations.AddRange(LocalAliases);
        process.Declarations.AddRange(LocalAttributeSpecifications);

        var ifInResetBlock = new InlineBlock(new LineComment("Synchronous reset"));

        // Re-setting all internally driven signals and variables to their initial value.
        ifInResetBlock.Body.AddRange(InternallyDrivenSignals
            .Where(signal => signal.InitialValue != null)
            .Select(signal =>
                new Assignment { AssignTo = signal.ToReference(), Expression = signal.InitialValue }));
        ifInResetBlock.Body.AddRange(LocalVariables
            .Union(GlobalVariables)
            .Where(variable => variable.InitialValue != null)
            .Select(variable =>
                new Assignment { AssignTo = variable.ToReference(), Expression = variable.InitialValue }));

        if (inReset != null)
        {
            ifInResetBlock.Add(inReset);
        }

        // Only add the ifInReset block if it contains anything apart from the one comment line.
        if (ifInResetBlock.Body.Take(2).Count() == 2)
        {
            var resetIf = new IfElse
            {
                Condition = new Binary
                {
                    Left = CommonPortNames.Reset.ToVhdlSignalReference(),
                    Operator = BinaryOperator.Equality,
                    Right = Value.OneCharacter,
                },
                True = ifInResetBlock,
            };
            if (notInReset != null)
            {
                resetIf.Else = notInReset;
            }

            process.Add(resetIf);
        }
        else if (notInReset != null)
        {
            process.Add(notInReset);
        }

        return process;
    }
}
