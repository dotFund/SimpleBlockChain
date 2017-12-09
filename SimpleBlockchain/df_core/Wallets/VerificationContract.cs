using System;
using System.Linq;
using System.IO;
using SimpleBlockchain.SmartContract;
using SimpleBlockchain.IO;
using SimpleBlockchain.Cryptography.ECC;
using SimpleBlockchain.Core;

namespace SimpleBlockchain.Wallets
{
    public class VerificationContract : Contract, IEquatable<VerificationContract>, ISerializable
    {
        public UInt160 PublicKeyHash;

        private string _address;

        /// <summary>
        /// get Address
        /// </summary>
        public string Address
        {
            get
            {
                if (_address == null)
                {
                    _address = Wallet.ToAddress(ScriptHash);
                }
                return _address;
            }
        }

        public int Size => PublicKeyHash.Size + ParameterList.GetVarSize() + Script.GetVarSize();

        public static VerificationContract Create(UInt160 publicKeyHash, ContractParameterType[] parameterList, byte[] redeemScript)
        {
            return new VerificationContract
            {
                Script = redeemScript,
                ParameterList = parameterList,
                PublicKeyHash = publicKeyHash
            };
        }

        public static VerificationContract CreateMultiSigContract(UInt160 publicKeyHash, int m, params ECPoint[] publicKeys)
        {
            return new VerificationContract
            {
                Script = CreateMultiSigRedeemScript(m, publicKeys),
                ParameterList = Enumerable.Repeat(ContractParameterType.Signature, m).ToArray(),
                PublicKeyHash = publicKeyHash
            };
        }

        public static VerificationContract CreateSignatureContract(ECPoint publicKey)
        {
            return new VerificationContract
            {
                Script = CreateSignatureRedeemScript(publicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                PublicKeyHash = publicKey.EncodePoint(true).ToScriptHash(),
            };
        }

        /// <summary>
        /// Deserialize
        /// </summary>
        /// <param name="reader">reader stream</param>
        public void Deserialize(BinaryReader reader)
        {
            PublicKeyHash = reader.ReadSerializable<UInt160>();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            Script = reader.ReadVarBytes();
        }

        /// <summary>
        /// Equals function that come from IEquatable
        /// </summary>
        /// <param name="other">others this type</param>
        /// <returns>boolean</returns>
        public bool Equals(VerificationContract other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

        /// <summary>
        /// Equals function that come from IEquatable
        /// </summary>
        /// <param name="obj">others this type</param>
        /// <returns>boolean</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as VerificationContract);
        }

        /// <summary>
        /// Get HashCode.
        /// 
        /// </summary>
        /// <returns>返回HashCode</returns>
        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

        /// <summary>
        /// serialize
        /// </summary>
        /// <param name="writer">serializing write stream</param>
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(PublicKeyHash);
            writer.WriteVarBytes(ParameterList.Cast<byte>().ToArray());
            writer.WriteVarBytes(Script);
        }
    }
}
