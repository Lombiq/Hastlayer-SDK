using Hast.TestInputs.Dynamic;
using System.Threading.Tasks;
using Xunit;

namespace Hast.DynamicTests.Tests;

public class InlinedCasesTests
{
    [Fact]
    public Task InlinedMultiReturn() =>
        TestExecutor.ExecuteSelectedTestAsync<InlinedCases>(
            g => g.InlinedMultiReturn(null),
            g =>
            {
                g.InlinedMultiReturn(3);
                g.InlinedMultiReturn(-3);
                g.NestedInlinedMultiReturn(3);
                g.NestedInlinedMultiReturn(-3);
            });
}
