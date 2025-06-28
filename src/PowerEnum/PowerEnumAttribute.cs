namespace PowerEnum;

/// <summary>
/// Marks a class as a PowerEnum.
/// </summary>
/// <remarks>
/// A PowerEnum is a 'smart enum' which is a strongly-typed,
/// object-oriented analog to an ordinary Enum.
/// <example>
/// <para>
/// Here is an example of how to create a PowerEnum:
/// <code>
/// [PowerEnum]
/// public partial class FavouriteColour
/// {
///     private partial FavouriteColour(string reason);
///     
///     public static FavouriteColour Red { get; } = new("Because it's cool");
///     public static FavouriteColour Green { get; } = new("Because it's friendly");
///     public static FavouriteColour Blue { get; } = new("Because it's awesome");
/// }
/// </code>
/// </para>
/// <para>
/// The <c>FavouriteColour</c> PowerEnum can then be used like so:
/// </para>
/// <para>
/// <code>
/// // Each item has a unique numeric value:
/// var thisIsZero = FavouriteColour.Red.Value;
/// var thisIsOne = FavouriteColour.Green.Value;
/// var thisIsTwo = FavouriteColour.Blue.Value;
/// 
/// // Each item has a unique name as well:
/// string red = FavouriteColour.Red.Name;     // contains "Red"
/// string green = FavouriteColour.Green.Name; // contains "Green"
/// string blue = FavouriteColour.Blue.Name;   // contains "Blue"
/// 
/// // Each item can have additional data:
/// string becauseItsCool = FavouriteColour.Red.Reason; // contains "Because it's cool"
/// 
/// // Get an item from numeric value or name:
/// var thisIsRed = FavouriteColour.FromValue(0);
/// var thisIsAlsoRed = FavouriteColour.FromName("Red");
/// 
/// // Get an array of all items:
/// var allItems = FavouriteColour.Items;
/// var firstColour = allItems.First();
/// </code>
/// </para>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class PowerEnumAttribute : Attribute { }
