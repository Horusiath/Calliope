namespace Calliope
{
    /// <summary>
    /// Interface used to expose <see cref="Merge"/> operation,
    /// method used for different replica conflict resolution.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConvergent<T> where T : IConvergent<T>
    {
        /// <summary>
        /// Merges current instance with another one, automatically and deterministically resolving all conflicts.
        /// Merge operation should be associative, commutative and idempotent.
        /// </summary>
        /// <param name="other">Other instance of the same type.</param>
        /// <returns></returns>
        T Merge(T other);
    }
}