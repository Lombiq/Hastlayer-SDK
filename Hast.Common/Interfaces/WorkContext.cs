﻿using System;
using System.Web;

namespace Hast.Common.Interfaces
{
    /// <summary>
    /// A work context for work context scope
    /// </summary>
    public abstract class WorkContext
    {
        /// <summary>
        /// Resolves a registered dependency type.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <returns>An instance of the dependency if it could be resolved.</returns>
        public virtual T Resolve<T>() => (T)Resolve(typeof(T));

        /// <summary>
        /// Resolves a registered dependency type.
        /// </summary>
        /// <param name="serviceType">The type of the dependency.</param>
        /// <returns>An instance of the dependency if it could be resolved.</returns>
        public abstract object Resolve(Type serviceType);

        /// <summary>
        /// Tries to resolve a registered dependency type.
        /// </summary>
        /// <typeparam name="T">The type of the dependency.</typeparam>
        /// <param name="service">An instance of the dependency if it could be resolved.</param>
        /// <returns>True if the dependency could be resolved, false otherwise.</returns>
        public virtual bool TryResolve<T>(out T service)
        {
            var success = TryResolve(typeof(T), out object serviceObject);
            service = success ? (T)serviceObject : default;
            return success;
        }

        /// <summary>
        /// Tries to resolve a registered dependency type.
        /// </summary>
        /// <param name="serviceType">The type of the dependency.</param>
        /// <param name="service">An instance of the dependency if it could be resolved.</param>
        /// <returns>True if the dependency could be resolved, false otherwise.</returns>
        public virtual bool TryResolve(Type serviceType, out object service)
        {
            try
            {
                service = Resolve(serviceType);
                return true;
            }
            catch
            {
                service = default;
                return false;
            }
        }

        public abstract T GetState<T>(string name);
        public abstract void SetState<T>(string name, T value);

        /*
        /// <summary>
        /// The http context corresponding to the work context
        /// </summary>
        public HttpContextBase HttpContext
        {
            get { return GetState<HttpContextBase>("HttpContext"); }
            set { SetState("HttpContext", value); }
        }

        /// <summary>
        /// The Layout shape corresponding to the work context
        /// </summary>
        public dynamic Layout
        {
            get { return GetState<dynamic>("Layout"); }
            set { SetState("Layout", value); }
        }

        /// <summary>
        /// Settings of the site corresponding to the work context
        /// </summary>
        public ISite CurrentSite
        {
            get { return GetState<ISite>("CurrentSite"); }
            set { SetState("CurrentSite", value); }
        } 

        /// <summary>
        /// The user, if there is any corresponding to the work context
        /// </summary>
        public IUser CurrentUser
        {
            get { return GetState<IUser>("CurrentUser"); }
            set { SetState("CurrentUser", value); }
        }

        /// <summary>
        /// The theme used in the work context
        /// </summary>
        public ExtensionDescriptor CurrentTheme
        {
            get { return GetState<ExtensionDescriptor>("CurrentTheme"); }
            set { SetState("CurrentTheme", value); }
        } */

        /// <summary>
        /// Active culture of the work context
        /// </summary>
        public string CurrentCulture
        {
            get { return GetState<string>("CurrentCulture"); }
            set { SetState("CurrentCulture", value); }
        }

        /// <summary>
        /// Active calendar of the work context
        /// </summary>
        public string CurrentCalendar
        {
            get { return GetState<string>("CurrentCalendar"); }
            set { SetState("CurrentCalendar", value); }
        }

        /// <summary>
        /// Time zone of the work context
        /// </summary>
        public TimeZoneInfo CurrentTimeZone
        {
            get { return GetState<TimeZoneInfo>("CurrentTimeZone"); }
            set { SetState("CurrentTimeZone", value); }
        }
    }
}
