namespace Flume.Comparison.MediatR;

/// <summary>
/// Sample request/response for MediatR
/// </summary>
public record MediatRRequest(string Message) : global::MediatR.IRequest<string>;
