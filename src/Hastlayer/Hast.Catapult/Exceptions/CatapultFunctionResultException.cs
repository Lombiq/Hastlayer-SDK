using System;

namespace Hast.Catapult.Exceptions;

/// <summary>
/// An exception which is fired when an FpgaCoreLib function returns something other than the SUCCESS status.
/// </summary>
public class CatapultFunctionResultException : Exception
{
    /// <summary>
    /// Gets the status returned by the native function call.
    /// </summary>
    public Constants.Catapult.Status Status { get; private set; }

    public CatapultFunctionResultException() { }

    public CatapultFunctionResultException(Constants.Catapult.Status status, string message)
        : this(string.IsNullOrWhiteSpace(message) ? status.ToString() : message) => Status = status;

    public CatapultFunctionResultException(string message)
        : base(message) { }

    public CatapultFunctionResultException(string message, Exception inner)
        : base(message, inner) { }
}
