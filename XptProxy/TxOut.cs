using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XptProxy
{
    public class TxOut
    {
        public long Value;
        public VarInt ScriptLength;   //  CScript stub
        public byte[] ScriptPubKey;   //  CScript stub   

        public TxOut(){}

        public TxOut(long value, byte[] scriptPubKey)
        {
            Value = value;
            ScriptLength = scriptPubKey.Length;
            ScriptPubKey = scriptPubKey;
        }

        public byte[] getBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Value));
            bytes.AddRange(ScriptLength.getBytes());
            bytes.AddRange(ScriptPubKey);
            return bytes.ToArray();
        }
    }
}
