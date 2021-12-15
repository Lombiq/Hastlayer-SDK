using System;
using Hast.Layer;

namespace Hast.Communication
{
    public class ProxyGenerator : IProxyGenerator
    {
        private readonly IMemberInvocationHandlerFactory _memberInvocationHandlerFactory;
        private readonly Castle.DynamicProxy.ProxyGenerator _proxyGenerator;

        public ProxyGenerator(IMemberInvocationHandlerFactory memberInvocationHandlerFactory)
        {
            _memberInvocationHandlerFactory = memberInvocationHandlerFactory;
            _proxyGenerator = new Castle.DynamicProxy.ProxyGenerator();
        }

        public T CreateCommunicationProxy<T>(
            IHardwareRepresentation hardwareRepresentation,
            T target,
            IProxyGenerationConfiguration configuration) where T : class
        {
            var memberInvocationHandler = _memberInvocationHandlerFactory.CreateMemberInvocationHandler(
                hardwareRepresentation, target, configuration);
            var interceptor = new MemberInvocationInterceptor(memberInvocationHandler);

            return typeof(T).IsInterface
                ? _proxyGenerator.CreateInterfaceProxyWithTarget(target, interceptor)
                : _proxyGenerator.CreateClassProxyWithTarget(target, interceptor);
        }

        [Serializable]
        public class MemberInvocationInterceptor : Castle.DynamicProxy.IInterceptor
        {
            private readonly MemberInvocationHandler _memberInvocationHandler;

            public MemberInvocationInterceptor(MemberInvocationHandler memberInvocationHandler)
            {
                _memberInvocationHandler = memberInvocationHandler;
            }

            public void Intercept(Castle.DynamicProxy.IInvocation invocation)
            {
                _memberInvocationHandler(invocation);
            }
        }
    }
}
