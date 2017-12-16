using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleBlockchain.Cryptography.ECC;
using SimpleBlockchain.IO;
using SimpleBlockchain.IO.Json;
using SimpleBlockchain.Wallets;

namespace SimpleBlockchain.Core
{
    [Obsolete]
    public class EnrollmentTransaction : Transaction
    {
        public ECPoint PublicKey;

        private UInt160 _script_hash = null;
        private UInt160 ScriptHash
        {
            get
            {
                if (_script_hash == null)
                {
                    _script_hash = VerificationContract.CreateSignatureContract(PublicKey).ScriptHash;
                }
                return _script_hash;
            }
        }

        public override int Size => base.Size + PublicKey.Size;

        public EnrollmentTransaction()
            : base(TransactionType.EnrollmentTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version != 0) throw new FormatException();
            PublicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            return base.GetScriptHashesForVerifying().Union(new UInt160[] { ScriptHash }).OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(PublicKey);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["pubkey"] = PublicKey.ToString();
            return json;
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            return false;
        }
    }
}
