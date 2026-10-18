namespace __GAME__.Core.Content;

/// <summary>
/// A validated, immutable definition loaded from <c>content/</c>: what game code uses. Its <see cref="Id"/> is
/// permanent (saves and other content refer to it), so it is never renamed or reused (ADR 0007).
/// </summary>
public interface IContentDefinition
{
    /// <summary>
    /// Gets the permanent ID, <c>&lt;prefix&gt;.&lt;snake_case_name&gt;</c>, e.g. <c>item.iron_sword</c>. The name part
    /// is the file name, which the loader enforces.
    /// </summary>
    string Id { get; }
}
