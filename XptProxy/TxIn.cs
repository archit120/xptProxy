using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XptProxy
{
    public class OutPoint
    {
        public byte[] hash;
        public uint index;

        public OutPoint()
        {
            hash = new byte[32];
            index = uint.MaxValue;
        }

        public byte[] getBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(hash);
            bytes.AddRange(BitConverter.GetBytes(index));
            return bytes.ToArray();
        }
    }
    public class TxIn
    {
        public OutPoint previous_output;
        public VarInt ScriptLength;   //  CScript stub
        public byte[] SigScript;      //  CScript stub
        public uint Sequence;

        public TxIn()
        {   
            previous_output = new OutPoint();
            Sequence = uint.MaxValue;
        }

        public TxIn(byte[] sigScript) : this()
        {
            SigScript = sigScript;
            ScriptLength = sigScript.Length;
        }

        public byte[] getBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(previous_output.getBytes());
            bytes.AddRange(ScriptLength.getBytes());
            bytes.AddRange(SigScript);
            bytes.AddRange(BitConverter.GetBytes(Sequence));
            return bytes.ToArray();
        }
    }
}
