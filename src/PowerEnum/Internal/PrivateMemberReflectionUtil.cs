using PowerEnumShared;
using System.Reflection;
using static PowerEnumShared.PrivateMemberUtil;

namespace PowerEnum.Internal
{
    internal class PrivateMemberReflectionUtil
    {
        internal static string GetPrivateSharedMemberName(Type type)
        {
            if (type.FullName is null)
            {
                throw new ArgumentException("Type must be a PowerEnum class.", nameof(type));
            }

            // Start the hash with the unique identifier of this instance of the generated code.
            if (type.GetCustomAttribute<PowerEnumGenerated>() is not PowerEnumGenerated attr)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} is not valid. Please ensure the provided type is a class having the [PowerEnum] attribute and ensure the PowerEnum nuget package is installed in the same project as the class that has the [PowerEnum] attribute.");
            }

            return $"{Constants.PowerEnumInternalLowerPrefix}_shared_{Hash(attr.Identifier, MemberType.SharedMember)}";
        }
    }
}
