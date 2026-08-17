namespace Wayfare.Core.Exceptions;

#region Tool Exceptions

/// <summary>
/// Thrown when loading or compiling dynamic tool assemblies fails.
/// </summary>
public class LoadToolException(string message, Exception? innerException = null) : Exception(message, innerException);

#endregion