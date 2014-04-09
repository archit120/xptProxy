using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XptProxy
{
    public class Transaction
    {
        public uint Version;
        public VarInt TxInCount;
        public TxIn[] TxInList;
        public VarInt TxOutCount;
        public TxOut[] TxOutList;
        public uint LockTime;

        public Transaction() {}

        public Transaction(uint version, TxIn[] txInList, TxOut[] txOutList)
        {
            Version = version;
            TxInCount = txInList.Length;
            TxInList = txInList;
            TxOutCount = txOutList.Length;
            TxOutList = txOutList;
            LockTime = 0;
        }

        //Special Coinbase constructor - Just what we need :D
        public Transaction(long value, byte[] sigScript, byte[] pubKey)
        {
            if(pubKey.Length != 33)
                throw new Exception("Wrong length of pubKey!");
            TxIn cbTxIn = new TxIn(sigScript);
            cbTxIn.ScriptLength += 4;
            byte[] scriptPubKey = new byte[35];
            scriptPubKey[0] = 33;
            scriptPubKey[34] = 172;
            pubKey.CopyTo(scriptPubKey, 1);
            TxOut cbTxOut = new TxOut(value, scriptPubKey);

            //We can't call it directly :'(

            Version = 1;
            TxInCount = 1;
            TxInList = new TxIn[] { cbTxIn };
            TxOutCount = 1;
            TxOutList = new TxOut[] { cbTxOut };
            LockTime = 0;
        }

        public byte[] getBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(TxInCount.getBytes());
            foreach (TxIn txIn in TxInList)
            {
                bytes.AddRange(txIn.getBytes());
            }
            bytes.AddRange(TxOutCount.getBytes());
            foreach (TxOut txOut in TxOutList)
            {
                bytes.AddRange(txOut.getBytes());
            }
            bytes.AddRange(BitConverter.GetBytes(LockTime));
            return bytes.ToArray();
        }
    }
}
