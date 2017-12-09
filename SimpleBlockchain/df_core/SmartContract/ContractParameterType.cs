namespace SimpleBlockchain.SmartContract
{
    public enum ContractParameterType : byte
    {
        /// <summary>
        /// sign
        /// </summary>
        Signature = 0x00,
        Boolean = 0x01,
        /// <summary>
        /// integer
        /// </summary>
        Integer = 0x02,
        /// <summary>
        /// 160 Hash
        /// </summary>
        Hash160 = 0x03,
        /// <summary>
        /// 256 Hash
        /// </summary>
        Hash256 = 0x04,
        /// <summary>
        /// byte array
        /// </summary>
        ByteArray = 0x05,
        PublicKey = 0x06,
        String = 0x07,

        Array = 0x10,

        InteropInterface = 0xf0,

        Void = 0xff
    }
}
