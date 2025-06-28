using System.ComponentModel;

namespace PowerEnum.Internal
{
    /// <summary>
    /// This is part of the internal implementation detail of PowerEnum.
    /// <para>
    /// Users of PowerEnum should not use this interface. It is unstable and subject to change.
    /// </para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IPowerEnumInternal
    {
        /// <summary>
        /// This is part of the internal implementation detail of PowerEnum.
        /// <para>
        /// Users of PowerEnum should not use this interface. It is unstable and subject to change.
        /// </para>
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        object Items { get; }
    }
}
