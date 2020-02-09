using System;
using System.Collections.Generic;
using System.Text;

namespace Hast.Common.Interfaces
{
    /// <summary>
    /// Base interface for services that are instantiated per unit of work (i.e. web request).
    /// </summary>
    public interface IDependency
    {
    }

    /// <summary>
    /// Base interface for services that are instantiated per shell/tenant.
    /// </summary>
    public interface ISingletonDependency : IDependency
    {
    }

    /// <summary>
    /// Base interface for services that may *only* be instantiated in a unit of work.
    /// This interface is used to guarantee they are not accidentally referenced by a singleton dependency.
    /// </summary>
    public interface IUnitOfWorkDependency : IDependency
    {
    }

    /// <summary>
    /// Base interface for services that are instantiated per usage.
    /// </summary>
    public interface ITransientDependency : IDependency
    {
    }


    public abstract class Component : IDependency
    {
        protected Component()
        {
            throw new NotImplementedException();
            //Logger = NullLogger.Instance;
            //T = NullLocalizer.Instance;
        }

        //public ILogger Logger { get; set; }
        //public Localizer T { get; set; }
    }
}
