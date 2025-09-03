#pragma warning disable S2326 // TResponse is intentionally unused in marker interfaces

namespace Flume;

/// <summary>
/// Marker interface to represent a stream request
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IStreamRequest<out TResponse> : IRequest { }