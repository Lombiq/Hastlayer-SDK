using System;
using System.Runtime.Serialization;

namespace Hast.Catapult.Exceptions;

/// <summary>
/// An exception which is fired when an FpgaCoreLib function returns something other than the SUCCESS status.
/// </summary>
[Serializable]
public class CatapultFunctionResultException : Exception
{
    /// <summary>
    /// Gets the status returned by the native function call.
    /// </summary>
    public CatapultConstants.Status Status { get; private set; }

    public CatapultFunctionResultException() { }

    public CatapultFunctionResultException(CatapultConstants.Status status, string message)
        : this(string.IsNullOrWhiteSpace(message) ? status.ToString() : message) => Status = status;

    public CatapultFunctionResultException(string message)
        : base(message) { }

    public CatapultFunctionResultException(string message, Exception inner)
        : base(message, inner) { }

    protected CatapultFunctionResultException(
      SerializationInfo info,
      StreamingContext context)
        : base(info, context)
    { }
}
