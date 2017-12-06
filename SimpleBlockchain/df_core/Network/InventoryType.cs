namespace SimpleBlockchain.Network
{
    public enum InventoryType : byte
    {
        /// <summary>
        /// transaction
        /// </summary>
        TX = 0x01,
        /// <summary>
        /// block
        /// </summary>
        Block = 0x02,
        /// <summary>
        /// consensus data
        /// </summary>
        Consensus = 0xe0
    }
}
