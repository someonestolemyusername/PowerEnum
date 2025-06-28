using PowerEnum.Internal;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PowerEnum;

/// <summary>
/// Provides static access to PowerEnum functionality.
/// <para>
/// If you know the class of your enum at compile time,
/// consider using <see cref="PowerEnum{TPowerEnum}"/>
/// instead for improved performance.
/// </para>
/// </summary>
public static class PowerEnum
{
    /// <summary>
    /// Gets a list of items in the provided PowerEnum class.
    /// </summary>
    /// <param name="powerEnumType">
    /// The <see cref="Type"/> of a PowerEnum class (i.e. a 
    /// <c>class</c> that has the <c>[PowerEnum]</c> attribute).
    /// </param>
    /// <returns>
    /// A list of items. The result is castable to
    /// <c>System.Collections.Generic.IReadOnlyList&lt;<paramref name="powerEnumType"/>&gt;</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IReadOnlyList<object> GetItems(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
        Type powerEnumType)
    {
        try
        {
            var memberName = GetPrivateSharedMemberName(powerEnumType);

            var field = powerEnumType.GetField(memberName, BindingFlags.NonPublic | BindingFlags.Static);

            var value = field?.GetValue(null) as IPowerEnumInternal;

            return value?.Items as IReadOnlyList<object>
                ?? throw new InvalidOperationException("Failed to retrieve items.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to access the PowerEnum internal data for the provided type {powerEnumType}. Please ensure this type is a valid PowerEnum class (i.e. a class that has the [PowerEnum] attribute) and check the source generator is not reporting errors.", ex);
        }
    }

    internal static IReadOnlyList<object> GetItemsUncached(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)]
        Type powerEnumType)
    {
        var memberName = PrivateMemberReflectionUtil.GetPrivateSharedMemberName(powerEnumType);
        var field = powerEnumType.GetField(memberName, BindingFlags.NonPublic | BindingFlags.Static);
        var value = field?.GetValue(null) as IPowerEnumInternal;

        return value?.Items as IReadOnlyList<object>
            ?? throw new InvalidOperationException(
                $"Failed to retrieve Items from PowerEnum of type {powerEnumType}.");
    }


    private static ConcurrentDictionary<Type, string>? _privateSharedMemberNameCache;

    private static string GetPrivateSharedMemberName(Type type)
    {
        var cache = LazyInitializer.EnsureInitialized(ref _privateSharedMemberNameCache)!;

        if (!cache.TryGetValue(type, out var name))
        {
            name = PrivateMemberReflectionUtil.GetPrivateSharedMemberName(type);

            cache.TryAdd(type, name);
        }

        return name;
    }
}
