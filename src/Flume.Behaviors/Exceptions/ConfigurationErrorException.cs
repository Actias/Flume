namespace Flume.Behaviors.Exceptions;

/// <inheritdoc />
public sealed class ConfigurationErrorException(string message, Exception? innerException)
    : Exception(message, innerException)
{
    public ConfigurationErrorException() : this("A configuration error has occurred.") {}

    public ConfigurationErrorException(string message) : this(message, null) {}
}