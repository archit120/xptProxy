using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XptProxy
{
    class Hash
    {
        public static byte[] DSha256_Hash(byte[] T)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                Byte[] result = hash.ComputeHash(T);
                return result;
            }
        }
    }
}
