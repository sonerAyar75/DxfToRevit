namespace DxfToRevit.Core.Import;

/// <summary>
/// Typed values shared during an import: module options set by the host before planning (for
/// example the column options), and data one module hands to the next (for example the host walls).
/// At most one value per type.
/// </summary>
public sealed class ImportBag
{
    private readonly Dictionary<Type, object> _values = new();

    /// <summary>Stores a value, replacing the previous value of the same type.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="value">Value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public void Set<T>(T value) where T : class =>
        _values[typeof(T)] = value ?? throw new ArgumentNullException(nameof(value));

    /// <summary>The value of a type, or <c>null</c> when none was stored.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    public T? Get<T>() where T : class =>
        _values.TryGetValue(typeof(T), out object? value) ? (T)value : null;

    /// <summary>The value of a type, or <paramref name="fallback"/> when none was stored.</summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="fallback">Value used when none was stored.</param>
    public T GetOrDefault<T>(T fallback) where T : class => Get<T>() ?? fallback;
}
