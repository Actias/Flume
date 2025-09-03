using System;

#pragma warning disable IDE0060 // It's recommended to keep the unused parameter 'Value' for clarity and consistency

namespace Flume;

/// <summary>
/// Represents a void return type for requests that don't return a value
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// Gets the single instance of Unit
    /// </summary>
    public static readonly Unit Value;

    /// <summary>
    /// Determines if this Unit equals another Unit
    /// </summary>
    /// <param name="other">The other Unit to compare</param>
    /// <returns>Always true since all Unit values are equal</returns>
    public bool Equals(Unit other) => true;
    
    /// <summary>
    /// Determines if this Unit equals another object
    /// </summary>
    /// <param name="obj">The object to compare</param>
    /// <returns>True if the object is a Unit, false otherwise</returns>
    public override bool Equals(object? obj) => obj is Unit;
    
    /// <summary>
    /// Gets the hash code for Unit
    /// </summary>
    /// <returns>Always 0 since all Unit values are equal</returns>
    public override int GetHashCode() => 0;
    
    /// <summary>
    /// Gets the string representation of Unit
    /// </summary>
    /// <returns>Always "()"</returns>
    public override string ToString() => "()";

    /// <summary>
    /// Equality operator for Unit values
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>Always true since all Unit values are equal</returns>
    public static bool operator ==(Unit left, Unit right) => true;
    
    /// <summary>
    /// Inequality operator for Unit values
    /// </summary>
    /// <param name="left">Left operand</param>
    /// <param name="right">Right operand</param>
    /// <returns>Always false since all Unit values are equal</returns>
    public static bool operator !=(Unit left, Unit right) => false;
}
