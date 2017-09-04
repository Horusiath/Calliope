using Calliope.Persistence;

namespace Calliope.Replication
{
    public interface ICrdtTrait<TCrdt, out TValue, TOp>
    {
        /// <summary>
        /// Default instance of a CRDT.
        /// </summary>
        TCrdt Zero { get; }

        /// <summary>
        /// Returns an actual value stored by CRDT object.
        /// </summary>
        /// <param name="crdt"></param>
        /// <returns></returns>
        TValue GetValue(TCrdt crdt);

        /// <summary>
        /// First phase, update. Also known as "at-source".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="crdt"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        object Prepare(TCrdt crdt, TOp operation);

        /// <summary>
        /// Second phase, apply. Also known as "downstream".
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="crdt"></param>
        /// <param name="operation"></param>
        /// <returns></returns>
        TCrdt Effect(TCrdt crdt, TOp operation);
    }

    public interface ICrdtService<TTrait, TCrdt, TValue, TOp> 
        where TTrait : struct, ICrdtTrait<TCrdt, TValue, TOp>
    {
        
    }
}