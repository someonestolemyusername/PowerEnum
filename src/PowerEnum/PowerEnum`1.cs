using System.Diagnostics.CodeAnalysis;

namespace PowerEnum
{
    /// <summary>
    /// Provides static access to PowerEnum functionality.
    /// </summary>
    /// <typeparam name="TPowerEnum">
    /// The type of the PowerEnum you are using.
    /// This must be a PowerEnum class (i.e. a <c>class</c> that
    /// has the <c>[PowerEnum]</c> attribute).
    /// </typeparam>
    public static class PowerEnum
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicFields)] TPowerEnum>
    {
        /// <summary>
        /// Gets a list of items in this PowerEnum.
        /// </summary>
        /// <value>
        /// A list of items in this PowerEnum.
        /// </value>
        public static IReadOnlyList<TPowerEnum> Items { get; } = GetItemsForPowerEnum();

        private static IReadOnlyList<TPowerEnum> GetItemsForPowerEnum()
        {
            try
            {
                return (IReadOnlyList<TPowerEnum>)PowerEnum.GetItemsUncached(typeof(TPowerEnum));
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(
                    $"Failed to retrieve Items from PowerEnum of type {typeof(TPowerEnum).FullName}.", e);
            }
        }
    }
}
