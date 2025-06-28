namespace PowerEnum.Internal
{
    /// <summary>
    /// This attribute is added by the PowerEnum source generator.
    /// Users of PowerEnum should not use this attribute in their own code.
    /// </summary>
    /// <param name="identifier"></param>
    [AttributeUsage(AttributeTargets.Class)]
    public class PowerEnumGenerated(ulong identifier) : Attribute
    {
        /// <summary>
        /// This identifier is a hash of the source document that defined this PowerEnum.
        /// It is used internally by PowerEnum.
        /// </summary>
        internal ulong Identifier => identifier;
    }
}
