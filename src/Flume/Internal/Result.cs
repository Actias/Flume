using System;

#pragma warning disable

namespace Flume.Internal;

/// <summary>
/// Result type for handling success/failure scenarios without exceptions
/// </summary>
/// <typeparam name="T">Type of the success value</typeparam>
public readonly struct Result<T>
{
    /// <summary>
    /// Represents the value stored in the current instance. This value may be null.
    /// </summary>
    /// <remarks>The value is read-only and cannot be modified after the instance is created.  Use this field
    /// to access the encapsulated data directly.</remarks>
    public readonly T? Value;

    /// <summary>
    /// 
    /// </summary>
    public readonly bool IsSuccess;
    public readonly string? Error;
    
    private Result(T? value, bool isSuccess, string? error)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static Result<T> Success(T value) => new(value, true, null);
    
    /// <summary>
    /// Create a failed result
    /// </summary>
    public static Result<T> Failure(string error) => new(default, false, error);
    
    /// <summary>
    /// Create a failed result with exception
    /// </summary>
    public static Result<T> Failure(Exception exception) => new(default, false, exception.Message);
    
    /// <summary>
    /// Implicit conversion from value to success result
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
    
    /// <summary>
    /// Implicit conversion from string error to failure result
    /// </summary>
    public static implicit operator Result<T>(string error) => Failure(error);
    
    /// <summary>
    /// Implicit conversion from exception to failure result
    /// </summary>
    public static implicit operator Result<T>(Exception exception) => Failure(exception);
    
    /// <summary>
    /// Deconstruct the result into its components
    /// </summary>
    public void Deconstruct(out bool isSuccess, out T? value, out string? error)
    {
        isSuccess = IsSuccess;
        value = Value;
        error = Error;
    }
    
    /// <summary>
    /// Match on success or failure
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
    
    /// <summary>
    /// Execute action on success
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value!);
        }
        return this;
    }
    
    /// <summary>
    /// Execute action on failure
    /// </summary>
    public Result<T> OnFailure(Action<string> action)
    {
        if (!IsSuccess)
        {
            action(Error!);
        }
        return this;
    }
    
    /// <summary>
    /// Map the success value to a new type
    /// </summary>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess ? Result<TResult>.Success(mapper(Value!)) : Result<TResult>.Failure(Error!);
    }
    
    /// <summary>
    /// Flat map the result to another result
    /// </summary>
    public Result<TResult> FlatMap<TResult>(Func<T, Result<TResult>> mapper)
    {
        return IsSuccess ? mapper(Value!) : Result<TResult>.Failure(Error!);
    }
}

/// <summary>
/// Result type for operations that don't return a value
/// </summary>
public readonly struct Result
{
    public readonly bool IsSuccess;
    public readonly string? Error;
    
    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static Result Success() => new(true, null);
    
    /// <summary>
    /// Create a failed result
    /// </summary>
    public static Result Failure(string error) => new(false, error);
    
    /// <summary>
    /// Create a failed result with exception
    /// </summary>
    public static Result Failure(Exception exception) => new(false, exception.Message);
    
    /// <summary>
    /// Implicit conversion from string error to failure result
    /// </summary>
    public static implicit operator Result(string error) => Failure(error);
    
    /// <summary>
    /// Implicit conversion from exception to failure result
    /// </summary>
    public static implicit operator Result(Exception exception) => Failure(exception);
    
    /// <summary>
    /// Deconstruct the result into its components
    /// </summary>
    public void Deconstruct(out bool isSuccess, out string? error)
    {
        isSuccess = IsSuccess;
        error = Error;
    }
    
    /// <summary>
    /// Match on success or failure
    /// </summary>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error!);
    }
    
    /// <summary>
    /// Execute action on success
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }
    
    /// <summary>
    /// Execute action on failure
    /// </summary>
    public Result OnFailure(Action<string> action)
    {
        if (!IsSuccess)
        {
            action(Error!);
        }
        return this;
    }
}
