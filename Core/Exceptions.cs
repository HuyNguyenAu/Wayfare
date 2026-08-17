namespace Wayfare.Core;

#region Tool Exceptions

public class LoadToolException(string message, Exception? innerException = null) : Exception(message, innerException);

#endregion