using System.Globalization;

namespace __GAME__.Wiki.Sources;

/// <summary>
/// Readable names for Godot's serialized input codes: keys (<c>Key</c>), mouse buttons (<c>MouseButton</c>), and
/// controller buttons and axes (<c>JoyButton</c>, <c>JoyAxis</c>), using the values Godot 4 writes to
/// <c>project.godot</c>. Unknown codes print as numbers rather than guessing.
/// </summary>
internal static class InputNames
{
    /// <summary>Godot's flag for non-printable keys (<c>Key.Special</c>).</summary>
    private const long Special = 1 << 22;

    /// <summary>The non-printable keys from Escape (special 0x01) to Scroll Lock (special 0x1B), in code order.</summary>
    private static readonly string[] SpecialKeys =
    [
        "Escape",
        "Tab",
        "Backtab",
        "Backspace",
        "Enter",
        "Keypad Enter",
        "Insert",
        "Delete",
        "Pause",
        "Print Screen",
        "SysReq",
        "Clear",
        "Home",
        "End",
        "Left",
        "Up",
        "Right",
        "Down",
        "Page Up",
        "Page Down",
        "Shift",
        "Ctrl",
        "Meta",
        "Alt",
        "Caps Lock",
        "Num Lock",
        "Scroll Lock",
    ];

    /// <summary>The keypad operator keys from special 0x81 to 0x85, in code order.</summary>
    private static readonly string[] KeypadOperators =
    [
        "Keypad *",
        "Keypad /",
        "Keypad -",
        "Keypad .",
        "Keypad +",
    ];

    /// <summary>The mouse buttons from 1, in code order.</summary>
    private static readonly string[] MouseButtons =
    [
        "Left mouse button",
        "Right mouse button",
        "Middle mouse button",
        "Mouse wheel up",
        "Mouse wheel down",
        "Mouse wheel left",
        "Mouse wheel right",
        "Mouse button 4",
        "Mouse button 5",
    ];

    /// <summary>The controller buttons from 0 (SDL layout, Xbox labels), in code order.</summary>
    private static readonly string[] JoypadButtons =
    [
        "A (bottom face button)",
        "B (right face button)",
        "X (left face button)",
        "Y (top face button)",
        "Back",
        "Guide",
        "Start",
        "Left stick press",
        "Right stick press",
        "Left shoulder",
        "Right shoulder",
        "D-pad up",
        "D-pad down",
        "D-pad left",
        "D-pad right",
        "Misc",
        "Paddle 1",
        "Paddle 2",
        "Paddle 3",
        "Paddle 4",
        "Touchpad",
    ];

    /// <summary>
    /// Names a keyboard key.
    /// </summary>
    /// <param name="code">The key code (<c>physical_keycode</c> or <c>keycode</c>).</param>
    /// <returns>The name, e.g. <c>W</c>, <c>Space</c>, <c>Escape</c>, or <c>F5</c>.</returns>
    public static string Key(long code)
    {
        if (code == 32)
        {
            return "Space";
        }

        if (code is > 32 and < 127)
        {
            return ((char)code).ToString(CultureInfo.InvariantCulture).ToUpperInvariant();
        }

        long special = code - Special;
        return special switch
        {
            >= 0x01 and <= 0x1B => SpecialKeys[special - 1],
            >= 0x1C and <= 0x3E => "F" + (special - 0x1B).ToString(CultureInfo.InvariantCulture),
            >= 0x81 and <= 0x85 => KeypadOperators[special - 0x81],
            >= 0x86 and <= 0x8F => "Keypad " + (special - 0x86).ToString(CultureInfo.InvariantCulture),
            _ => "Key " + code.ToString(CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// Names a mouse button.
    /// </summary>
    /// <param name="index">The button index (1 is the left button).</param>
    /// <returns>The name.</returns>
    public static string MouseButton(long index) =>
        index >= 1 && index <= MouseButtons.Length
            ? MouseButtons[index - 1]
            : "Mouse button " + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Names a controller button.
    /// </summary>
    /// <param name="index">The button index.</param>
    /// <returns>The name.</returns>
    public static string JoypadButton(long index) =>
        index >= 0 && index < JoypadButtons.Length
            ? JoypadButtons[index]
            : "Button " + index.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Names a controller axis direction.
    /// </summary>
    /// <param name="axis">The axis index.</param>
    /// <param name="value">The bound direction (its sign matters; triggers are positive).</param>
    /// <returns>The name, e.g. <c>Left stick left</c> or <c>Right trigger</c>.</returns>
    public static string JoypadAxis(long axis, double value) =>
        axis switch
        {
            0 => value < 0 ? "Left stick left" : "Left stick right",
            1 => value < 0 ? "Left stick up" : "Left stick down",
            2 => value < 0 ? "Right stick left" : "Right stick right",
            3 => value < 0 ? "Right stick up" : "Right stick down",
            4 => "Left trigger",
            5 => "Right trigger",
            _ => "Axis " + axis.ToString(CultureInfo.InvariantCulture),
        };
}
