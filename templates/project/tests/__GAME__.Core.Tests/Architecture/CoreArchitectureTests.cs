using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using __GAME__.Core.Content;

namespace __GAME__.Core.Tests.Architecture;

/// <summary>
/// Guards the Core architecture rules that the compiler can't (Coding Standards §3.1, ADR 0001): Core never
/// references Godot, and holds no mutable static state.
/// </summary>
public sealed class CoreArchitectureTests
{
    /// <summary>The Core assembly.</summary>
    private static readonly Assembly Core = typeof(ContentLoader).Assembly;

    [Fact]
    public void CoreAssembly_ReferencesNoGodotAssembly()
    {
        // Act
        string[] godot =
        [
            .. Core.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .Where(name => name.StartsWith("Godot", StringComparison.OrdinalIgnoreCase)),
        ];

        // Assert
        Assert.Empty(godot);
    }

    [Fact]
    public void CoreTypes_HaveNoMutableStaticFields()
    {
        // Act
        string[] mutable =
        [
            .. Core.GetTypes()
                .Where(type => !IsGenerated(type))
                .SelectMany(type =>
                    type.GetFields(
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                    )
                )
                .Where(field => !field.IsInitOnly && !field.IsLiteral && !IsGenerated(field))
                .Select(field => $"{field.DeclaringType?.FullName}.{field.Name}"),
        ];

        // Assert
        Assert.Empty(mutable);
    }

    /// <summary>
    /// Gets whether a member comes from the compiler or a source generator (lambda caches, regex and JSON
    /// generators), which the rule doesn't cover.
    /// </summary>
    /// <param name="member">The type or field.</param>
    /// <returns><see langword="true"/> for generated code.</returns>
    private static bool IsGenerated(MemberInfo member)
    {
        for (MemberInfo? current = member; current is not null; current = current.DeclaringType)
        {
            if (
                current.Name.StartsWith('<')
                || current.IsDefined(typeof(CompilerGeneratedAttribute), false)
                || current.IsDefined(typeof(GeneratedCodeAttribute), false)
            )
            {
                return true;
            }
        }

        return false;
    }
}
