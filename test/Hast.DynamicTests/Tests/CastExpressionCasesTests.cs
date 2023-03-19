using Hast.TestInputs.Dynamic;
using System.Threading.Tasks;
using Xunit;

namespace Hast.DynamicTests.Tests;

public class CastExpressionCasesTests
{
    [Fact]
    public Task AllNumberCastingVariations() =>
        TestExecutor.ExecuteSelectedTestAsync<CastExpressionCases>(
            c => c.AllNumberCastingVariations(null),
            c =>
            {
                c.AllNumberCastingVariations(long.MinValue + 1);
                c.AllNumberCastingVariations(123);
                c.AllNumberCastingVariations(124);
                c.AllNumberCastingVariations(long.MaxValue);
            });
}
