using System;
using System.Collections.Generic;
using SimpleBlockchain.Cryptography;

namespace SimpleBlockchain.Wallets
{
    public abstract class Wallet : IDisposable
    {
        public event EventHandler BalanceChanged;

        public static readonly byte AddressVersion = Settings.Default.AddressVersion;

        private readonly string path;
        private readonly byte[] iv;         //what is this?
        private readonly byte[] masterKey;  //what is this?
        private readonly Dictionary<UInt160, KeyPair> keys;
        public void Dispose()
        {

        }

        public static string ToAddress(UInt160 scriptHash)
        {
            byte[] data = new byte[21];
            data[0] = AddressVersion;
            Buffer.BlockCopy(scriptHash.ToArray(), 0, data, 1, 20);
            return data.Base58CheckEncode();
        }
    }
}
