using System;

namespace Hast.Common.Interfaces
{
    public interface IWorkContextAccessor : ISingletonDependency
    {
        WorkContext GetContext();
        IWorkContextScope CreateWorkContextScope();
    }

    public interface ILogicalWorkContextAccessor : IWorkContextAccessor
    {
        WorkContext GetLogicalContext();
    }

    public interface IWorkContextStateProvider : IDependency
    {
        Func<WorkContext, T> Get<T>(string name);
    }

    public interface IWorkContextScope : ISingletonDependency, IDisposable
    {
        WorkContext WorkContext { get; }
        TService Resolve<TService>();
        bool TryResolve<TService>(out TService service);
    }
}
