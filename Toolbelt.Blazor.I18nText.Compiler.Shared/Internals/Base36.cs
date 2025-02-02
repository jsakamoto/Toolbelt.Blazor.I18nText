using System;
using System.Numerics;

namespace Toolbelt.Blazor.I18nText.Compiler.Shared.Internals
{
    /// <summary>
    /// Provides methods to convert byte array to Base36 string.
    /// </summary>
    internal static class Base36
    {
        /// <summary>
        /// Converts byte array to Base36 string.
        /// </summary>
        public static string Encode(byte[] bytes)
        {
            const string base36Chars = "0123456789abcdefghijklmnopqrstuvwxyz";

            var result = new char[10];
            var buff = new byte[9];
            Array.Copy(bytes, buff, 9);
            var dividend = BigInteger.Abs(new BigInteger(buff));
            for (var i = 0; i < 10; i++)
            {
                dividend = BigInteger.DivRem(dividend, 36, out var remainder);
                result[i] = base36Chars[(int)remainder];
            }

            return new string(result);
        }
    }
}
