using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace __GAME__.Game.Common;

/// <summary>
/// Converts between <c>System.Numerics</c> types (used by Core) and Godot math types (used by the game layer). This
/// is the only sanctioned crossing point for vector math between the layers (ADR 0001).
/// </summary>
/// <remarks>All conversions are allocation-free component copies.</remarks>
public static class NumericsInterop
{
    /// <summary>Converts a Core vector to a Godot vector.</summary>
    /// <param name="value">The Core vector.</param>
    /// <returns>The equivalent Godot vector.</returns>
    public static Vector2 ToGodot(this NumericsVector2 value) => new(value.X, value.Y);

    /// <summary>Converts a Godot vector to a Core vector.</summary>
    /// <param name="value">The Godot vector.</param>
    /// <returns>The equivalent Core vector.</returns>
    public static NumericsVector2 ToNumerics(this Vector2 value) => new(value.X, value.Y);

    /// <summary>Converts a Core vector to a Godot vector.</summary>
    /// <param name="value">The Core vector.</param>
    /// <returns>The equivalent Godot vector.</returns>
    public static Vector3 ToGodot(this NumericsVector3 value) => new(value.X, value.Y, value.Z);

    /// <summary>Converts a Godot vector to a Core vector.</summary>
    /// <param name="value">The Godot vector.</param>
    /// <returns>The equivalent Core vector.</returns>
    public static NumericsVector3 ToNumerics(this Vector3 value) => new(value.X, value.Y, value.Z);

    /// <summary>Converts a Core quaternion to a Godot quaternion.</summary>
    /// <param name="value">The Core quaternion.</param>
    /// <returns>The equivalent Godot quaternion.</returns>
    public static Quaternion ToGodot(this NumericsQuaternion value) => new(value.X, value.Y, value.Z, value.W);

    /// <summary>Converts a Godot quaternion to a Core quaternion.</summary>
    /// <param name="value">The Godot quaternion.</param>
    /// <returns>The equivalent Core quaternion.</returns>
    public static NumericsQuaternion ToNumerics(this Quaternion value) => new(value.X, value.Y, value.Z, value.W);
}
