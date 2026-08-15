namespace Wayfare.Core.Exceptions;

public class LoadToolException(string message, Exception? innerException = null) : Exception(message, innerException);
