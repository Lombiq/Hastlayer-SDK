using Hast.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Common.Services
{
    public class CoreWorkContext : WorkContext
    {
        private readonly IServiceProvider _serviceProvider;

        public CoreWorkContext(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override T GetState<T>(string name)
        {
            throw new NotImplementedException();
        }

        public override void SetState<T>(string name, T value)
        {
            throw new NotImplementedException();
        }

        public override T Resolve<T>() => _serviceProvider.GetRequiredService<T>();

        public override object Resolve(Type serviceType) => _serviceProvider.GetRequiredService(serviceType);
    }

    public class CoreWorkContextAccessor : IWorkContextAccessor
    {
        private readonly IServiceProvider _serviceProvider;

        public CoreWorkContextAccessor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IWorkContextScope CreateWorkContextScope() => new CoreWorkContextScope(_serviceProvider);

        public WorkContext GetContext() => new CoreWorkContext(_serviceProvider);
    }

    public class CoreWorkContextScope : IWorkContextScope
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceScope _serviceScope;

        private bool isDisposed = false;

        public WorkContext WorkContext => new CoreWorkContext(_serviceProvider);

        public CoreWorkContextScope(IServiceProvider serviceProvider)
        {
            _serviceScope = serviceProvider.CreateScope();
        }

        public TService Resolve<TService>() => _serviceScope.ServiceProvider.GetService<TService>();

        public bool TryResolve<TService>(out TService service)
        {
            try
            {
                service = Resolve<TService>();
                return true;
            }
            catch
            {
                service = default;
                return false;
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            _serviceScope.Dispose();
            isDisposed = true;
        }
    }
}
