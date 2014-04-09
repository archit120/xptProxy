using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XptProxy
{
    public static class BlockBuilder
    {
        public static byte[] getBlock(byte opcode, byte[] data)
        {
            byte[] block = new byte[data.Length + 4];
            data.CopyTo(block, 4);
            int header = (opcode) & 0x000000FF | (int) ((data.Length << 8) & 0xFFFFFF00);
            BitConverter.GetBytes(header).CopyTo(block, 0);
            return block;
        }
    }
}
