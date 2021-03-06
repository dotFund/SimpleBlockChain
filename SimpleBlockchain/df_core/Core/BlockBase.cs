﻿using System;
using System.IO;
using SimpleBlockchain.Cryptography;
using SimpleBlockchain.IO;
using SimpleBlockchain.IO.Json;
using SimpleBlockchain.VM;
using SimpleBlockchain.Wallets;

namespace SimpleBlockchain.Core
{
    public abstract class BlockBase : IVerifiable
    {
        public uint Version;
        public UInt256 PrevHash;
        public UInt256 MerkleRoot;
        public uint Timestamp;
        public uint Index;
        public ulong ConsensusData;
        public UInt160 NextConsensus;
        public Witness Script;

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        Witness[] IVerifiable.Scripts
        {
            get
            {
                return new[] { Script };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Script = value[0];
            }
        }

        public virtual int Size => sizeof(uint) + PrevHash.Size + MerkleRoot.Size + sizeof(uint) + sizeof(uint) + sizeof(ulong) + NextConsensus.Size + 1 + Script.Size;

        public virtual void Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Script = reader.ReadSerializable<Witness>();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt32();
            Index = reader.ReadUInt32();
            ConsensusData = reader.ReadUInt64();
            NextConsensus = reader.ReadSerializable<UInt160>();
        }

        byte[] IScriptContainer.GetMessage()
        {
            return this.GetHashData();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying()
        {
            if (PrevHash == UInt256.Zero)
                return new[] { Script.VerificationScript.ToScriptHash() };
            Header prev_header = Blockchain.Default.GetHeader(PrevHash);
            if (prev_header == null) throw new InvalidOperationException();
            return new UInt160[] { prev_header.NextConsensus };
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Script);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Index);
            writer.Write(ConsensusData);
            writer.Write(NextConsensus);
        }

        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevHash.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["index"] = Index;
            json["nonce"] = ConsensusData.ToString("x16");
            json["nextconsensus"] = Wallet.ToAddress(NextConsensus);
            json["script"] = Script.ToJson();
            return json;
        }

        public bool Verify()
        {
            if (Hash == Blockchain.GenesisBlock.Hash) return true;
            if (Blockchain.Default.ContainsBlock(Hash)) return true;
            Header prev_header = Blockchain.Default.GetHeader(PrevHash);
            if (prev_header == null) return false;
            if (prev_header.Index + 1 != Index) return false;
            if (prev_header.Timestamp >= Timestamp) return false;
            if (!this.VerifyScripts()) return false;
            return true;
        }
    }
}
