using System.IO;
using SimpleBlockchain.IO;
using SimpleBlockchain.VM;

namespace SimpleBlockchain.Core
{
    public interface IVerifiable : ISerializable, IScriptContainer
    {
        /// <summary>
        /// script list to verify the account
        /// </summary>
        Witness[] Scripts { get; set; }

        /// <summary>
        /// deserialized unsigned
        /// </summary>
        /// <param name="reader">数据来源</param>
        void DeserializeUnsigned(BinaryReader reader);

        /// <summary>
        /// get scripthash for verify.
        /// </summary>
        /// <returns>UInt160 Hash</returns>
        UInt160[] GetScriptHashesForVerifying();

        /// <summary>
        /// serializeUnsigned
        /// </summary>
        /// <param name="writer">unsigned data</param>
        void SerializeUnsigned(BinaryWriter writer);
    }
}
