namespace Flume.Comparison.Flume;

/// <summary>
/// Sample request/response for Flume
/// </summary>
public record FlumeRequest(string Message) : IRequest<string>;
