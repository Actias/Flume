namespace Flume.Comparison.SimpleMediator;

/// <summary>
/// Sample request/response for Flume
/// </summary>
public record FlumeRequest(string Message) : IRequest<string>;
