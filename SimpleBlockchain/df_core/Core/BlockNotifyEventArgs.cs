using System;
using SimpleBlockchain.SmartContract;

namespace SimpleBlockchain.Core
{
    public class BlockNotifyEventArgs : EventArgs
    {
        public Block Block { get; }
        public NotifyEventArgs[] Notifications { get; }

        public BlockNotifyEventArgs(Block block, NotifyEventArgs[] notifications)
        {
            this.Block = block;
            this.Notifications = notifications;
        }
    }
}
