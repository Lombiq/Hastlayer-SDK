using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Hast.Transformer.Abstractions.Configuration
{
    [DebuggerDisplay("{MemberNamePrefix + \": \" + MaxInvocationInstanceCount}")]
    public class MemberInvocationInstanceCountConfiguration
    {
        /// <summary>
        /// Gets the prefix of the member's name. Use the same convention as with 
        /// <see cref="HardwareGenerationConfiguration.HardwareEntryPointMemberNamePrefixes"/>. For lambda expressions use
        /// the pattern "Hast.Samples.SampleAssembly.PrimeCalculator.ParallelizedArePrimeNumbers.LambdaExpression.0",
        /// i.e. specify the name prefix of the calling member, then add ".LambdaExpression" and finally add the lambda's
        /// index inside the calling member (so if it's the first lambda in the member then it should have the index 0,
        /// if it's the second, the index 1 and so on).
        /// </summary>
        public string MemberNamePrefix { get; private set; }

        /// <summary>
        /// Gets or sets the maximal recursion depth of the member. When using (even indirectly) recursive invocations
        /// between members set the maximal depth here.
        /// </summary>
        /// <example>
        /// A value of 3 would mean that the member can invoke itself three times recursively, i.e. there is the
        /// member, then it calls itself (depth 1), then it calls itself (depth 2), then it calls itself (depth 3)
        /// before returning.
        /// </example>
        public ushort MaxRecursionDepth { get; set; }

        private ushort _maxDegreeOfParallelism;
        /// <summary>
        /// Gets or sets the maximal degree of parallelism that will be attempted to build into the generated hardware
        /// when constructs suitable for hardware-level parallelisation are found.
        /// </summary>
        /// <example>
        /// A value of 3 would mean that maximally 3 instances will be able to be executed in parallel.
        /// </example>
        public ushort MaxDegreeOfParallelism
        {
            get { return _maxDegreeOfParallelism; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("The max degree of parallelism should be at least 1, otherwise the member wouldn't be transformed at all.");
                }

                _maxDegreeOfParallelism = value;
            }
        }

        // But why do I need to cast to uint? http://stackoverflow.com/questions/10065287/why-is-ushort-ushort-equal-to-int#comment58098182_10157517
        public int MaxInvocationInstanceCount { get { return (MaxRecursionDepth + 1) * MaxDegreeOfParallelism; } }


        /// <summary>
        /// Constructs a new <see cref="MemberInvocationInstanceCountConfiguration"/> object.
        /// </summary>
        /// <param name="memberNamePrefix">
        /// The prefix of the member's name. Use the same convention as with <see cref="MemberNamePrefix"/>.
        /// </param>
        public MemberInvocationInstanceCountConfiguration(string memberNamePrefix)
        {
            MemberNamePrefix = memberNamePrefix;
            MaxDegreeOfParallelism = 1;
        }
    }


    public class MemberInvocationInstanceCountConfigurationForMethod<T> : MemberInvocationInstanceCountConfiguration
    {
        /// <summary>
        /// Constructs a new <see cref="MemberInvocationInstanceCountConfiguration"/> object for a method (or methods)
        /// with the given name prefix.
        /// </summary>
        /// <param name="methodNamePrefix">The prefix of the method's name (or methods' names).</param>
        public MemberInvocationInstanceCountConfigurationForMethod(
            string methodNamePrefix) : base(typeof(T).FullName + "." + methodNamePrefix) { }

        /// <summary>
        /// Constructs a new <see cref="MemberInvocationInstanceCountConfiguration"/> object for a method.
        /// </summary>
        /// <param name="expression">An expression with a call to the method.</param>
        public MemberInvocationInstanceCountConfigurationForMethod(
            Expression<Action<T>> expression) : base(expression.GetMethodSimpleName()) { }

        /// <summary>
        /// Constructs a new <see cref="MemberInvocationInstanceCountConfiguration"/> object for a lambda expression
        /// inside a method.
        /// </summary>
        /// <param name="expression">An expression with a call to the method.</param>
        /// <param name="lambdaExpressionIndex">
        /// The lambda expression's zero-based index (i.e. is it the 0th, or 1st lambda in the method?).
        /// </param>
        public MemberInvocationInstanceCountConfigurationForMethod(
            Expression<Action<T>> expression, int lambdaExpressionIndex)
            : base(expression.GetMethodSimpleName() + ".LambdaExpression." + lambdaExpressionIndex.ToString()) { }
    }
}
