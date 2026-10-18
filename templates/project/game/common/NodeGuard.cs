using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Godot;

namespace __GAME__.Game.Common;

/// <summary>
/// Fail-fast checks for node wiring, called from <c>_Ready</c>. A missing export or a skipped <c>Initialize</c> call
/// is a programmer error, so these throw immediately with a clear message.
/// </summary>
public static class NodeGuard
{
    /// <summary>
    /// Ensures an <c>[Export]</c> reference was assigned in the scene.
    /// </summary>
    /// <typeparam name="T">The exported reference type.</typeparam>
    /// <param name="owner">The node that declares the export; used in the error message.</param>
    /// <param name="value">The exported value to check.</param>
    /// <param name="memberName">The export's member name. Filled in by the compiler.</param>
    /// <exception cref="InvalidOperationException">The export is not assigned.</exception>
    public static void RequireExport<T>(
        Node owner,
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string memberName = ""
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (value is null)
        {
            throw new InvalidOperationException(
                $"{owner.GetType().Name} '{owner.Name}': required export '{memberName}' is not assigned in the scene."
            );
        }
    }

    /// <summary>
    /// Ensures a node's <c>Initialize</c> method ran before it entered the scene tree, by checking a dependency it
    /// stores.
    /// </summary>
    /// <typeparam name="T">The dependency type.</typeparam>
    /// <param name="owner">The node being checked; used in the error message.</param>
    /// <param name="dependency">A dependency that <c>Initialize</c> stores.</param>
    /// <exception cref="InvalidOperationException"><paramref name="dependency"/> is <see langword="null"/>.</exception>
    public static void RequireInitialized<T>(Node owner, [NotNull] T? dependency)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (dependency is null)
        {
            throw new InvalidOperationException(
                $"{owner.GetType().Name} '{owner.Name}': Initialize(...) must be called before the node enters the tree."
            );
        }
    }
}
