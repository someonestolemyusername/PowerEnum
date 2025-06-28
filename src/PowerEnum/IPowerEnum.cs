namespace PowerEnum;

/// <summary>
/// The base interface implemented by all PowerEnum items.
/// </summary>
/// <remarks>
/// Note that the instance methods/properties pertain to a single item,
/// not the overall enum itself.
/// </remarks>
/// <typeparam name="TEnum">
/// The base class for this particular PowerEnum.
/// </typeparam>
public interface IPowerEnum<TEnum>
    where TEnum : IPowerEnum<TEnum>
{
    /// <summary>
    /// Gets the name of this item.
    /// </summary>
    /// <value>
    /// The name of this item.
    /// </value>
    string Name { get; }

    /// <summary>
    /// Gets the numeric value of this item.
    /// </summary>
    /// <remarks>
    /// Items are numbered from zero.
    /// </remarks>
    /// <value>
    /// The numeric value of this item.
    /// </value>
    int Value { get; }

    /// <summary>
    /// Gets a string representation of this item.
    /// </summary>
    /// <remarks>
    /// By default, this returns the item's Name as would be returned from <see cref="Name"/>.
    /// </remarks>
    /// <returns>
    /// A string representation of this item.
    /// </returns>
    string ToString();
}
