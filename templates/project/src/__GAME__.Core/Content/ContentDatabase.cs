using System.Diagnostics.CodeAnalysis;

namespace __GAME__.Core.Content;

/// <summary>
/// Every valid content definition, keyed by permanent ID. Built once at boot by <see cref="ContentLoader"/> and
/// passed down to whatever needs it; never mutated afterwards.
/// </summary>
/// <remarks>
/// Lookups by ID are O(1) and allocation-free. <see cref="All"/> and <see cref="AllOf{T}"/> allocate: call them at
/// load time and keep the result.
/// </remarks>
public sealed class ContentDatabase
{
    /// <summary>The definitions, keyed by ID.</summary>
    private readonly Dictionary<string, IContentDefinition> _definitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentDatabase"/> class.
    /// </summary>
    /// <param name="definitions">The definitions; their IDs must be unique.</param>
    /// <exception cref="ArgumentNullException"><paramref name="definitions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Two definitions share an ID.</exception>
    public ContentDatabase(IEnumerable<IContentDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _definitions = new Dictionary<string, IContentDefinition>(StringComparer.Ordinal);
        foreach (IContentDefinition definition in definitions)
        {
            if (!_definitions.TryAdd(definition.Id, definition))
            {
                throw new ArgumentException($"The content ID \"{definition.Id}\" appears twice.", nameof(definitions));
            }
        }
    }

    /// <summary>Gets a database with no definitions. It is immutable, so sharing it is safe.</summary>
    public static ContentDatabase Empty { get; } = new([]);

    /// <summary>Gets the number of definitions.</summary>
    public int Count => _definitions.Count;

    /// <summary>Gets every definition, ordered by ID. Allocates a new list on each call.</summary>
    public IReadOnlyList<IContentDefinition> All =>
        [.. _definitions.Values.OrderBy(definition => definition.Id, StringComparer.Ordinal)];

    /// <summary>
    /// Finds a definition by ID and type.
    /// </summary>
    /// <typeparam name="T">The definition type expected.</typeparam>
    /// <param name="id">The permanent ID.</param>
    /// <param name="definition">The definition, when found with that type.</param>
    /// <returns><see langword="true"/> when a <typeparamref name="T"/> with that ID exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    public bool TryGet<T>(string id, [NotNullWhen(true)] out T? definition)
        where T : class, IContentDefinition
    {
        ArgumentNullException.ThrowIfNull(id);
        definition = _definitions.TryGetValue(id, out IContentDefinition? found) ? found as T : null;
        return definition is not null;
    }

    /// <summary>
    /// Gets a definition that must exist: one that code or validated content refers to.
    /// </summary>
    /// <typeparam name="T">The definition type expected.</typeparam>
    /// <param name="id">The permanent ID.</param>
    /// <returns>The definition.</returns>
    /// <exception cref="KeyNotFoundException">No <typeparamref name="T"/> has that ID (a programmer error).</exception>
    public T Get<T>(string id)
        where T : class, IContentDefinition =>
        TryGet(id, out T? definition)
            ? definition
            : throw new KeyNotFoundException($"No {typeof(T).Name} has the ID \"{id}\".");

    /// <summary>
    /// Gets every definition of one type, ordered by ID. Allocates; call at load time and keep the result.
    /// </summary>
    /// <typeparam name="T">The definition type.</typeparam>
    /// <returns>The definitions.</returns>
    public IReadOnlyList<T> AllOf<T>()
        where T : class, IContentDefinition =>
        [.. _definitions.Values.OfType<T>().OrderBy(definition => definition.Id, StringComparer.Ordinal)];
}
