using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Bitnet;
using Bitnet.Client;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.InteropServices;

namespace XptProxy
{
    public struct WorkData
    {
        public uint Version;
        public uint Hieght;
        public uint nBits;

        public uint TargetCompact;
        public uint TargetShareCompact;

        public uint nTime;

        public byte[] prevBlockHash;
        public byte[] merkleRoot;

        public ushort CoinBase1Size;
        public byte[] CoinBase1;
        public ushort CoinBase2Size;
        public byte[] CoinBase2;

        public ushort TxCount;
        public byte[] TxHashes;

        public byte[] GetBytes()
        {
            if (prevBlockHash == null || merkleRoot == null || CoinBase1 == null || CoinBase2 == null ||
                TxHashes == null)
            {
                throw new Exception("Improper initialization!");
            }
            if (prevBlockHash.Length != 32 || merkleRoot.Length != 32 || CoinBase1.Length != CoinBase1Size ||
                CoinBase2.Length != CoinBase2Size || TxHashes.Length != TxCount*32)
            {
                throw new Exception("Improper length of arrays!");
            }
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Version));
            bytes.AddRange(BitConverter.GetBytes(Hieght));
            bytes.AddRange(BitConverter.GetBytes(nBits));

            bytes.AddRange(BitConverter.GetBytes(TargetCompact));
            bytes.AddRange(BitConverter.GetBytes(TargetShareCompact));

            bytes.AddRange(BitConverter.GetBytes(nTime));

            bytes.AddRange(prevBlockHash);
            bytes.AddRange(merkleRoot);

            bytes.AddRange(BitConverter.GetBytes(CoinBase1Size));
            bytes.AddRange(CoinBase1);
            bytes.AddRange(BitConverter.GetBytes(CoinBase2Size));
            bytes.AddRange(CoinBase2);

            bytes.AddRange(BitConverter.GetBytes(TxCount));
            bytes.AddRange(TxHashes);

            return bytes.ToArray();
        }
    }


    public struct ShareData
    {
        public byte[] MerkleRoot;
        public byte[] PrevBlockHash;

        public uint Version;
        public uint NTime;
        public uint NOnce;
        public uint NBits;

        public byte[] RiecoinNOffset;
        public byte[] MerkleRootOriginal;

        public byte UserExtraNonceLength;
        public byte[] UserExtraNonce;

        public uint ShareID;

        public ShareData(byte[] data, int index = 0)
        {
            int parserIndex = index;

            MerkleRoot = new byte[32];
            Array.Copy(data, parserIndex, MerkleRoot, 0, 32);
            parserIndex += 32;

            PrevBlockHash = new byte[32];
            Array.Copy(data, parserIndex, PrevBlockHash, 0, 32);
            parserIndex += 32;

            Version = BitConverter.ToUInt32(data, parserIndex);
            parserIndex += 4;

            NTime = BitConverter.ToUInt32(data, parserIndex);
            parserIndex += 4;

            NOnce = BitConverter.ToUInt32(data, parserIndex);
            parserIndex += 4;

            NBits = BitConverter.ToUInt32(data, parserIndex);
            parserIndex += 4;

            RiecoinNOffset = new byte[32];
            Array.Copy(data, parserIndex, RiecoinNOffset, 0, 32);
            parserIndex += 32;

            MerkleRootOriginal = new byte[32];
            Array.Copy(data, parserIndex, MerkleRootOriginal, 0, 32);
            parserIndex += 32;

            if (MerkleRootOriginal[0] != 0 || MerkleRootOriginal[12] != 0 || MerkleRootOriginal[31] != 0)
                throw new Exception("Invalid parsing or block data!");

            UserExtraNonceLength = data[parserIndex];
            parserIndex++;

            UserExtraNonce = new byte[UserExtraNonceLength];
            Array.Copy(data, parserIndex, UserExtraNonce, 0, UserExtraNonceLength);
            parserIndex += UserExtraNonceLength;

            ShareID = BitConverter.ToUInt32(data, parserIndex);
            parserIndex += 4;
        }

        public byte[] GetHashForPow()
        {
            List<byte> bytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(Version));

            bytes.AddRange(PrevBlockHash);
            bytes.AddRange(MerkleRoot);

            bytes.AddRange(BitConverter.GetBytes(NBits));
            bytes.AddRange(BitConverter.GetBytes(((ulong) NTime)));

            return Hash.DSha256_Hash(bytes.ToArray());
        }

    }

    class Program
    {
        [DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);


        public static Dictionary<TcpClient, XptWorker> Workers = new Dictionary<TcpClient, XptWorker>();

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x%2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static int BN_num_bytes(BigInteger number)
        {
            if (number == 0)
            {
                return 0;
            }
            return 1 + (int) Math.Floor(BigInteger.Log(BigInteger.Abs(number), 2))/8;
        }

        public static ulong GetCompact(uint Target)
        {
            uint nSize = (uint) BN_num_bytes(Target);
            ulong nCompact = 0;
            if (nSize <= 3)
                nCompact = Target << (int) (8*(3 - nSize));
            else
            {
                BigInteger big;
                big = Target >> (int) (8*(nSize - 3));
                nCompact = (ulong) big;
            }
            // The 0x00800000 bit denotes the sign.
            // Thus, if it is already set, divide the mantissa by 256 and increase the exponent.
            if ((nCompact & 0x00800000) == 1)
            {
                nCompact >>= 8;
                nSize++;
            }
            nCompact |= nSize << 24;
            nCompact |= (ulong) ((Target & 0x00800000) != 0 ? 0x00800000 : 0);
            return nCompact;
        }

        private static byte COIN_MODE = 0x07;
        private static string pubKeyRie = "021c9705b676e2193ddecc9bb77f6a0dfffcd3ee61612db925f1f6d56a46ca9c6b";
        private static BitnetClient bc = new BitnetClient("http://localhost:3889");
        private static GnuMp.mpz_t numb1;

        private static void Main(string[] args)
        {
            Console.WriteLine("testing");

         /*   ulong ab = uint.MaxValue;
            ab++;
            GnuMp.mpz_init(ref numb1);
            byte[] tester = BitConverter.GetBytes(ab);
            //int test = GnuMp.mpz_probab_prime_p(ref numb1, 25);
            GnuMp.mpz_import(ref numb1, (ulong) tester.Length, -1, 1, 0, 0, tester);

            ulong num2 = GnuMp.mpz_get_ui(ref numb1);         */

      


            Console.WriteLine("beginning listen");

            StartListening();

            while (true)
            {

            }
        }

        private static async void StartListening()
        {
            TcpListener listener = new TcpListener(7707);
            listener.Start();
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                AcceptClient(client);
            }

        }

        private static bool checkXptVersion(byte version)
        {
            if (version == 06)
                return true;
            Console.WriteLine("Unknown XPT verison");
            return false;
        }

        private static async void AcceptClient(TcpClient client)
        {

            XptWorker worker = new XptWorker();
            while (true)
            {
                byte[] buffer = new byte[1024];
                await client.GetStream().ReadAsync(buffer, 0, 1024);
                switch (BitConverter.ToUInt32(buffer, 0) & 0xFF)
                {
                    case 01:
                        Console.WriteLine("XPT_OPC_C_AUTH_REQ request");
                        if (checkXptVersion(buffer[4]))
                        {
                            string username = ASCIIEncoding.ASCII.GetString(buffer, 9, buffer[8]);
                            string password = ASCIIEncoding.ASCII.GetString(buffer, 10 + buffer[8],
                                                                            buffer[9 + buffer[8]]);
                            Console.WriteLine("Username:{0} Password:{1} Accepted!", username, password);

                            byte[] login = new byte[7];

                            login[6] = 0x07;

                            login = BlockBuilder.getBlock(02, login);

                            worker.WorkerName = username;
                            worker.WorkerPass = password;

                            Workers.Add(client, worker);

                            await client.GetStream().WriteAsync(login, 0, login.Length);
                            Console.WriteLine("Sent XPT_OPC_S_AUTH_ACK");

                            Console.WriteLine("Sending work data");


                            JToken bt = bc.InvokeMethod("getblocktemplate")["result"];
                            System.IO.File.WriteAllText("tets.txt", bt.ToString());
                            WorkData workData;
                            workData.Version = (uint) bt["version"];
                            workData.Hieght = (uint) bt["height"];
                            //We get nbits of pime value using .getCompact()
                            string prime = (string) bt["diff"];
                            workData.nBits = (uint) GetCompact(uint.Parse(prime));

                            workData.TargetCompact = uint.Parse(prime);
                            workData.TargetShareCompact = 5;
                            //Random gibber-gabber? Is that even the right spelling?

                            workData.nTime = (uint) bt["curtime"];
                            //You absolutely sure about this Archit? No I am not, Archit

                            byte[] coinbase = new Transaction((long) bt["coinbasevalue"],
                                                              StringToByteArray((string) bt["coinbaseaux"]["flags"]),
                                                              StringToByteArray(pubKeyRie)).getBytes();

                            workData.CoinBase1 = new byte[42];
                            workData.CoinBase1Size = 42;

                            Array.Copy(coinbase, 0, workData.CoinBase1, 0, 42);

                            workData.CoinBase2 = new byte[coinbase.Length - 42];
                            workData.CoinBase2Size = (ushort) workData.CoinBase2.Length;

                            Array.Copy(coinbase, 42, workData.CoinBase2, 0, workData.CoinBase2Size);

                            Console.WriteLine("Sending coinbase1 : {0} and coinbase2 : {1}",
                                              BitConverter.ToString(workData.CoinBase1).Replace("-", string.Empty),
                                              BitConverter.ToString(workData.CoinBase2).Replace("-", string.Empty));

                            workData.prevBlockHash = StringToByteArray((string) bt["previousblockhash"]);
                            Array.Reverse(workData.prevBlockHash);
                            workData.merkleRoot = new byte[32];

                            JArray transactions = (JArray) bt["transactions"];

                            workData.TxCount = (ushort) transactions.Count;
                            workData.TxHashes = new byte[workData.TxCount*32];

                            for (int i = 0; i < workData.TxCount; i++)
                            {
                                byte[] txHash = StringToByteArray((string) transactions[i]["hash"]);
                                Array.Reverse(txHash);
                                Array.Copy(txHash, 0, workData.TxHashes, i*32, 32);
                            }


                            byte[] block = workData.GetBytes();
                            block = BlockBuilder.getBlock(03, block);

                            await client.GetStream().WriteAsync(block, 0, block.Length);
                            Console.WriteLine("Sent XPT_OPC_S_WORKDATA1");
                        }
                        break;
                    case 04:
                        Console.WriteLine("XPT_OPC_C_SUBMIT_SHARE request");

                        //Prepare to be doomed
                        ShareData share = new ShareData(buffer, 4);
                        //Array.Reverse(share.RiecoinNOffset);

                        GnuMp.mpz_t n = new GnuMp.mpz_t();

                        GnuMp.mpz_init(ref n);

                        GnuMp.mpz_import(ref n, (ulong) share.RiecoinNOffset.Length, -1, 1, 0, 0, share.RiecoinNOffset);

                        BigInteger bn = new BigInteger(share.RiecoinNOffset);

                        if ((bn - 97)%210 != 0)
                        {
                            Console.WriteLine("Full of crap you are! {0}", (bn - 97)%210);
                        }



                        if (GnuMp.mpz_probab_prime_p(ref n, 25) == 0)
                            break;

                        GnuMp.mpz_add_ui(ref n, ref n, 4);

                        if (GnuMp.mpz_probab_prime_p(ref n, 25) == 0)
                            break;

                        GnuMp.mpz_add_ui(ref n, ref n, 2);

                        if (GnuMp.mpz_probab_prime_p(ref n, 25) == 0)
                            break;

                        GnuMp.mpz_add_ui(ref n, ref n, 4);

                        if (GnuMp.mpz_probab_prime_p(ref n, 25) == 0)
                            break;

                        Console.WriteLine("A valid share you are!");

                        //n+12 , 16
                        /*
                         * 
                        if (GnuMp.mpz_probab_prime_p(ref mn5, 25) == 0)
                        {
                            Console.WriteLine("A valid share you are!");
                            break;
                        }
                        if (GnuMp.mpz_probab_prime_p(ref mn5, 25) == 0)
                            Console.WriteLine("A valid block you are!");    */
                        break;
                    case 08:
                        Console.WriteLine("XPT_OPC_C_PING request");

                        await client.GetStream().WriteAsync(buffer, 0, buffer.Length);

                        Console.WriteLine("Sent XPT_OPC_S_PING");



                        break;
                    default:
                        Console.WriteLine("Unknown method from client, {0}", buffer[0]);
                        break;
                }
            }
        }
    }
}

