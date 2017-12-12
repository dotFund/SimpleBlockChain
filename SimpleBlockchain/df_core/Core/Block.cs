using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SimpleBlockchain.Core
{
    public class Block : BlockBase, IInventory, IEquatable<Block>
    {
    }
}
