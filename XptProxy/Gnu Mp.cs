using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace XptProxy
{
    class GnuMp
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct mpz_t
        {
            private int _mp_alloc;
            private int _mp_size;
            private IntPtr ptr;
        }
        [DllImport("gmp", EntryPoint = "__gmpz_init")]//, CallingConvention=CallingConvention.Cdecl)]
        public static extern void mpz_init(ref mpz_t value);

        [DllImport("gmp", EntryPoint = "__gmpz_init_set_ui")]//, CallingConvention=CallingConvention.Cdecl)]
        public static extern void mpz_init_set_ui(ref mpz_t value, ulong v);

        [DllImport("gmp", EntryPoint = "__gmpz_probab_prime_p")]//, CallingConvention=CallingConvention.Cdecl)]
        public static extern int mpz_probab_prime_p(ref mpz_t value, int reps);

        [DllImport("gmp", EntryPoint = "__gmpz_get_ui")]//, CallingConvention=CallingConvention.Cdecl)]
        public static extern ulong mpz_get_ui(ref mpz_t value);

        [DllImport("gmp", EntryPoint = "__gmpz_import")]
        public static extern void mpz_import(ref mpz_t rop, ulong count, int order,
                                              ulong size, int endian,
                                              ulong nails, byte[] op);
                                        
        [DllImport("gmp", EntryPoint = "__gmpz_add_ui")]
        public static extern void mpz_add_ui(ref mpz_t rop, ref mpz_t op1, ulong op2);
    }
}
