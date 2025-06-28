using System;

namespace PowerEnumShared;

internal static class PrivateMemberUtil
{
    internal enum MemberType
    {
        SharedMember,
        StructMember,
        SharedType,
        StructType
    }

    internal const string Warning = "Please do not rely on these generated members in your application.";

    internal static ulong InitialHashValue = 5381UL;

    internal static ulong Hash(ulong hash, string value)
    {
        foreach (var c in value)
        {
            hash = (hash << 5) + hash + c;
        }

        return hash;
    }

    internal static string Hash(ulong identifier, MemberType memberType)
    {
        var hash = memberType switch
        {
            MemberType.SharedMember => Hash(identifier, "SharedMember"),
            MemberType.StructMember => Hash(identifier, "StructMember"),
            MemberType.SharedType => Hash(identifier, "SharedType"),
            MemberType.StructType => Hash(identifier, "StructType"),
            _ => throw new ArgumentOutOfRangeException(nameof(memberType)),
        };

        // Hash our warning in, to ensure anyone doing this outside of
        // the library is warned not to.
        hash = Hash(hash, Warning);

        return hash.ToString("x16");
    }
}
