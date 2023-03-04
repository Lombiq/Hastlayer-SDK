using Hast.Common.Services;
using Hast.Layer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hast.Transformer.Vhdl.Tests.IntegrationTestingServices;

public abstract class IntegrationTestFixtureBase : IDisposable
{
    private readonly Lazy<Hastlayer> _host;

    protected readonly HastlayerConfiguration _hostConfiguration = new();

    private bool _disposed;

    protected Hastlayer Host => _host.Value;

    protected IntegrationTestFixtureBase()
    {
        _hostConfiguration.Extensions = new List<Assembly>();
        _hostConfiguration.OnServiceRegistration = OnServiceRegistration;
        _host = new Lazy<Hastlayer>(() => Hastlayer.Create(_hostConfiguration));
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing) Host.Dispose();

        _disposed = true;
    }

    private static void OnServiceRegistration(IHastlayerConfiguration configuration, IServiceCollection services)
    {
        services.RemoveImplementations<IHashProvider>();
        services.AddScoped<IHashProvider, VerificationTestHashProvider>();
    }
}
