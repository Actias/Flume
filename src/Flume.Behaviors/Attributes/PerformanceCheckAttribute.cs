namespace Flume.Behaviors.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class PerformanceCheckAttribute(
    long executionWarningInMilliseconds = PerformanceCheckAttribute.DefaultExecutionWarningInMilliseconds,
    long? executionErrorInMilliseconds = PerformanceCheckAttribute.DefaultExecutionErrorInMilliseconds
) : Attribute
{
    public const long DefaultExecutionWarningInMilliseconds = 1000;

    public const long DefaultExecutionErrorInMilliseconds = 30000;

    public long ExecutionWarningInMilliseconds { get; } = executionWarningInMilliseconds;

    public long? ExecutionErrorInMilliseconds { get; } = executionErrorInMilliseconds;
}