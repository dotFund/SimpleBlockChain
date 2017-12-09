using System;
using System.Threading;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using SimpleBlockchain.IO.Caching;
using SimpleBlockchain.Cryptography;
using SimpleBlockchain.Core;

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
        private readonly Dictionary<UInt160, VerificationContract> contracts;
        private readonly HashSet<UInt160> watchOnly;
        private readonly TrackableCollection<CoinReference, Coin> coins;
        private uint current_height;

        private static readonly Random rand = new Random();
        private readonly Thread thread;
        private bool isrunning = true;

        protected string DbPath => path;
        protected object SyncRoot { get; } = new object();
        public uint WalletHeight => current_height;
        protected abstract Version Version { get; }

        private Wallet(string path, byte[] passwordKey, bool create)
        {
            this.path = path;
            if (create)
            {
                this.iv = new byte[16];
                this.masterKey = new byte[32];
                this.keys = new Dictionary<UInt160, KeyPair>();
                this.contracts = new Dictionary<UInt160, VerificationContract>();
                this.watchOnly = new HashSet<UInt160>();
                this.coins = new TrackableCollection<CoinReference, Coin>();
                this.current_height = Blockchain.Default?.HeaderHeight + 1 ?? 0;
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                    rng.GetBytes(masterKey);
                }
                BuildDatabase();
                SaveStoredData("PasswordHash", passwordKey.Sha256());
                SaveStoredData("IV", iv);
                SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
                SaveStoredData("Version", new[] { Version.Major, Version.Minor, Version.Build, Version.Revision }.Select(p => BitConverter.GetBytes(p)).SelectMany(p => p).ToArray());
                SaveStoredData("Height", BitConverter.GetBytes(current_height));
#if NET461
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
#endif
            }
            else
            {
                byte[] passwordHash = LoadStoredData("PasswordHash");
                if (passwordHash != null && !passwordHash.SequenceEqual(passwordKey.Sha256()))
                    throw new CryptographicException();
                this.iv = LoadStoredData("IV");
                this.masterKey = LoadStoredData("MasterKey").AesDecrypt(passwordKey, iv);
#if NET461
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
#endif
                this.keys = LoadKeyPairs().ToDictionary(p => p.PublicKeyHash);
                this.contracts = LoadContracts().ToDictionary(p => p.ScriptHash);
                this.watchOnly = new HashSet<UInt160>(LoadWatchOnly());
                this.coins = new TrackableCollection<CoinReference, Coin>(LoadCoins());
                this.current_height = LoadStoredData("Height").ToUInt32(0);
            }
            Array.Clear(passwordKey, 0, passwordKey.Length);
            this.thread = new Thread(ProcessBlocks);
            this.thread.IsBackground = true;
            this.thread.Name = "Wallet.ProcessBlocks";
            this.thread.Start();
        }

        protected abstract void SaveStoredData(string name, byte[] value);
        protected abstract byte[] LoadStoredData(string name);
        protected abstract IEnumerable<KeyPair> LoadKeyPairs();
        protected abstract IEnumerable<VerificationContract> LoadContracts();
        protected abstract IEnumerable<Coin> LoadCoins();

        protected virtual IEnumerable<UInt160> LoadWatchOnly()
        {
            return Enumerable.Empty<UInt160>();
        }

        protected virtual void BuildDatabase()
        {
        }

        private void ProcessBlocks()
        {
            while (isrunning)
            {
                while (current_height <= Blockchain.Default?.Height && isrunning)
                {
                    lock (SyncRoot)
                    {
                        Block block = Blockchain.Default.GetBlock(current_height);
                        if (block != null) ProcessNewBlock(block);
                    }
                }
                for (int i = 0; i < 20 && isrunning; i++)
                {
                    Thread.Sleep(100);
                }
            }
        }

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
