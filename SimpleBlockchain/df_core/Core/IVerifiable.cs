using System.IO;
using SimpleBlockchain.IO;
using SimpleBlockchain.VM;

namespace SimpleBlockchain.Core
{
    public interface IVerifiable : ISerializable, IScriptContainer
    {
        Witness[] Scripts { get; set; }

        void DeserializeUnsigned(BinaryReader reader);

        UInt160[] GetScriptHashesForVerifying();

        void SerializeUnsigned(BinaryWriter writer);
    }
}
