using Hast.TestInputs.Dynamic;
using System.Threading.Tasks;
using Xunit;

namespace Hast.DynamicTests.Tests;

public class CastExpressionCasesTests
{
    [Fact]
    public Task AllNumberCastingVariations() =>
        TestExecutor.ExecuteSelectedTestAsync<CastExpressionCases>(
            caseSelector: c => c.AllNumberCastingVariations(null),
            testExecutor: c =>
            {
                c.AllNumberCastingVariations(long.MinValue + 1);
                c.AllNumberCastingVariations(123);
                c.AllNumberCastingVariations(124);
                c.AllNumberCastingVariations(long.MaxValue);
            });
}
