using System.IO;

namespace SimpleBlockchain.IO
{
    /// <summary>
    /// Serializable Interface
    /// </summary>
    public interface ISerializable
    {
        int Size { get; }

        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="writer">Serialize data</param>
        void Serialize(BinaryWriter writer);

        /// <summary>
        /// Deserialize
        /// </summary>
        /// <param name="reader">Deserialize data</param>
        void Deserialize(BinaryReader reader);
    }
}
