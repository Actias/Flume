using System;

namespace Flume.Internal;

/// <summary>
/// Specifies the order in which a notification handler should be executed
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderAttribute"/> class.
/// </remarks>
/// <param name="order">The order value. Lower values are executed first.</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class OrderAttribute(int order) : Attribute
{
    /// <summary>
    /// Gets the order value
    /// </summary>
    public int Order { get; } = order;
}
