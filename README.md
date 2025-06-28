# PowerEnum

PowerEnum is a 'smart enum' for C#, offering a strongly-typed, object-oriented alternative to traditional enums. It enables you to associate additional data with each enum item and provides convenient methods for accessing and manipulating enum values.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Fabs (someonestolemyusername)

## Installation

You can install PowerEnum via NuGet:

### Using NuGet Package Manager

```
Install-Package PowerEnum
```

### Using .NET CLI

```
dotnet add package PowerEnum
```

## Usage

To create a PowerEnum, define a partial class with the `[PowerEnum]` attribute. Then, define the enum items using one of the following two methods:

### Defining items with static properties

In this approach, you explicitly define static properties for each enum item within the class:

```csharp
[PowerEnum]
public partial class FavouriteColour
{
    private partial FavouriteColour(string reason);

    public static FavouriteColour Red { get; } = new("Because it's cool");
    public static FavouriteColour Green { get; } = new("Because it's friendly");
    public static FavouriteColour Blue { get; } = new("Because it's awesome");
}
```

### Defining items with a static constructor

Alternatively, you can use a static constructor to initialize the enum items. The PowerEnum code generator will automatically create the corresponding static properties (`Red`, `Green`, and `Blue`) and add XML documentation:

```csharp
[PowerEnum]
public partial class FavouriteColour
{
    private partial FavouriteColour(string reason);

    static FavouriteColour()
    {
        Red = new("Because it's cool");
        Green = new("Because it's friendly");
        Blue = new("Because it's awesome");
    }
}
```

In this method, the properties `Red`, `Green`, and `Blue` are generated automatically by the code generator.

### Item constructors and instance properties

If you want to store any additional data for each item, you can use a partial constructor, or implement your own. When you use a partial constructor, each parameter of the constructor is automatically exposed as a get-only property on the item:

```csharp
[PowerEnum]
public partial class FavouriteColour
{
    private partial FavouriteColour(string reason);

    // The code generator will create a property similar to this:
    //
    // public string Reason { get; } // exposes the 'reason' constructor parameter

    static FavouriteColour()
    {
        Red = new("Because it's cool");
        Green = new("Because it's friendly");
        Blue = new("Because it's awesome");
    }
}
```

If you need additional functionality inside your constructor, simply implement your own constructor:

```csharp
[PowerEnum]
public partial class FavouriteColour
{
    private FavouriteColour(string reason)
    {
        Reason = reason + " (a reason)";
    }

    public string Reason { get; }

    static FavouriteColour()
    {
        Red = new("Because it's cool");
        Green = new("Because it's friendly");
        Blue = new("Because it's awesome");
    }
}
```

Of course, having a constructor is not required if you don't want to store any instance data:

```csharp
[PowerEnum]
public partial class FavouriteColour
{
    static FavouriteColour()
    {
        Red = new();
        Green = new();
        Blue = new();
    }
}
```

### Using the PowerEnum

Once your PowerEnum is defined using either method, you can use it as follows:

```csharp
// Access numeric values
var redValue = FavouriteColour.Red.Value; // 0
var greenValue = FavouriteColour.Green.Value; // 1
var blueValue = FavouriteColour.Blue.Value; // 2

// Access names
string redName = FavouriteColour.Red.Name; // "Red"
string greenName = FavouriteColour.Green.Name; // "Green"
string blueName = FavouriteColour.Blue.Name; // "Blue"

// Access additional data
string redReason = FavouriteColour.Red.Reason; // "Because it's cool"

// Retrieve items by value or name
var redFromValue = FavouriteColour.FromValue(0); // FavouriteColour.Red
var redFromName = FavouriteColour.FromName("Red"); // FavouriteColour.Red

// Get all items
var allColours = FavouriteColour.Items; // array of all FavouriteColour items
```

Note that both methods of defining the enum items provide the same functionality. The static constructor approach offers the advantage of automatic property generation and XML documentation, while the static properties method provides explicit control over property definitions. You can even combine both methods if desired.

## Why Use PowerEnum?

PowerEnum provides several benefits over standard C# enums:

- **Additional Data**: Attach extra information to each enum item, like the `reason` in the example.
- **Strong Typing**: Eliminates reliance on magic numbers or strings, enhancing code safety.
- **Convenience Methods**: Retrieve items by value or name and access all items via the `Items` property.

## Requirements

PowerEnum targets netstandard2.0. This means it can be used on .NET 4.6.1 or newer; .NET Core 2.0 or newer; and many other platforms.

Even though PowerEnum leverages C# source generators, it is compatible with legacy .NET 4 projects as long as your MSBuild or Visual Studio version is new enough to support the .NET 7 SDK.
