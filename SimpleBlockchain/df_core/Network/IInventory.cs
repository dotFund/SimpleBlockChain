using SimpleBlockchain.Core;

namespace SimpleBlockchain.Network
{
    public interface IInventory : IVerifiable
    {
        UInt256 Hash { get; }

        InventoryType InventoryType { get; }

        bool Verify();
    }
}
