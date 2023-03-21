using Microsoft.Extensions.DependencyInjection;

namespace Moq;

public static class MoqExtensions
{
    public static IServiceCollection AddMock<T>(this IServiceCollection services)
        where T : class =>
        services.AddSingleton<T>(_ => new Mock<T>().Object);

    public static void ForceMock<T>(this Mock<T> mock, IServiceCollection services)
        where T : class
    {
        services.RemoveImplementations<T>();
        services.AddSingleton(mock.Object);
    }
}
