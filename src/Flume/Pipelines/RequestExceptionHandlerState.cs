namespace Flume.Pipelines;

/// <summary>
/// Represents the state of an exception handler
/// </summary>
/// <typeparam name="TResponse">The type of response</typeparam>
public class RequestExceptionHandlerState<TResponse>
{
    /// <summary>
    /// Gets a value indicating whether the exception has been handled
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Gets the response set by the exception handler
    /// </summary>
    public TResponse? Response { get; private set; }

    /// <summary>
    /// Sets the response and marks the exception as handled
    /// </summary>
    /// <param name="response">The response to set</param>
    public void SetHandled(TResponse response)
    {
        Response = response;
        Handled = true;
    }

    /// <summary>
    /// Marks the exception as handled without setting a response
    /// </summary>
    public void SetHandled()
    {
        Handled = true;
    }
}
