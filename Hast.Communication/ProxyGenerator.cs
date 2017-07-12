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


        public T CreateCommunicationProxy<T>(IHardwareRepresentation hardwareRepresentation, T target, IProxyGenerationConfiguration configuration) where T : class
        {
            var memberInvocationHandler = _memberInvocationHandlerFactory.CreateMemberInvocationHandler(hardwareRepresentation, target, configuration);
            if (typeof(T).IsInterface)
            {
                return _proxyGenerator.CreateInterfaceProxyWithTarget(target, new MemberInvocationInterceptor(memberInvocationHandler));
            }

            return _proxyGenerator.CreateClassProxyWithTarget(target, new MemberInvocationInterceptor(memberInvocationHandler));
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
