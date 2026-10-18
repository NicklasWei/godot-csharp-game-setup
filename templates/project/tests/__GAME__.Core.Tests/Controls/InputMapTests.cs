using System.Reflection;
using __GAME__.Core.Controls;
using __GAME__.Core.Tests.TestSupport;

namespace __GAME__.Core.Tests.Controls;

/// <summary>
/// Validates the input map in <c>project.godot</c> against <see cref="InputActionNames"/> and the input-parity rule:
/// every action must work on both keyboard/mouse and controller.
/// </summary>
public sealed class InputMapTests
{
    /// <summary>Event types that count as a keyboard/mouse binding.</summary>
    private static readonly string[] KeyboardMouseEvents = ["InputEventKey", "InputEventMouseButton"];

    /// <summary>Event types that count as a controller binding.</summary>
    private static readonly string[] JoypadEvents = ["InputEventJoypadButton", "InputEventJoypadMotion"];

    /// <summary>Gets every action name declared in code.</summary>
    public static TheoryData<string> Actions => [.. InputActionNames.All];

    [Fact]
    public void All_ContainsEveryDeclaredConstant()
    {
        // Arrange
        string[] constants =
        [
            .. typeof(InputActionNames)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => field.GetRawConstantValue())
                .OfType<string>(),
        ];

        // Act & Assert
        Assert.Equal(constants.Order(StringComparer.Ordinal), InputActionNames.All.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void InputMap_Actions_MatchDeclaredNamesExactly()
    {
        // Act
        IReadOnlyDictionary<string, IReadOnlyList<string>> inputMap = GodotProjectFile.ReadInputMap();

        // Assert
        Assert.Equal(InputActionNames.All.Order(StringComparer.Ordinal), inputMap.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Actions))]
    public void InputMap_Action_HasKeyboardOrMouseBinding(string action)
    {
        // Arrange
        IReadOnlyDictionary<string, IReadOnlyList<string>> inputMap = GodotProjectFile.ReadInputMap();

        // Act
        bool hasBinding =
            inputMap.TryGetValue(action, out IReadOnlyList<string>? events) && events.Any(KeyboardMouseEvents.Contains);

        // Assert
        Assert.True(hasBinding, $"Action '{action}' has no keyboard or mouse binding.");
    }

    [Theory]
    [MemberData(nameof(Actions))]
    public void InputMap_Action_HasJoypadBinding(string action)
    {
        // Arrange
        IReadOnlyDictionary<string, IReadOnlyList<string>> inputMap = GodotProjectFile.ReadInputMap();

        // Act
        bool hasBinding =
            inputMap.TryGetValue(action, out IReadOnlyList<string>? events) && events.Any(JoypadEvents.Contains);

        // Assert
        Assert.True(hasBinding, $"Action '{action}' has no controller binding.");
    }
}
