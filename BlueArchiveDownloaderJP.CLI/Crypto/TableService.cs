using Crypto.XXHash;
using System.Text;
using System;

namespace Crypto
{
    /// <summary>
    /// General password gen by file name, encode to base64 for zips password
    /// </summary>
    public static class TableService
    {
        /// <summary>
        /// 以給定 key 產生密碼，預設長度 20 bytes（實際回傳長度會依 base64 編碼比例略微調整）
        /// </summary>
        /// <param name="key">用於隨機種子的字串</param>
        /// <param name="length">最終想要的密碼長度</param>
        /// <returns>產生好的 byte[] 密碼</returns>
        public static byte[] CreatePassword(string key, int length = 20)
        {
            byte[] password = GC.AllocateUninitializedArray<byte>((int)Math.Round((decimal)(length / 4 * 3)));

            using var xxhash = XXHash32.Create();
            xxhash.ComputeHash(Encoding.UTF8.GetBytes(key));

            var mt = new MersenneTwister((int)xxhash.HashUInt32);

            int i = 0;
            while (i < password.Length)
            {
                Array.Copy(BitConverter.GetBytes(mt.Next()), 0, password, i, Math.Min(4, password.Length - i));
                i += 4;
            }

            return password;
        }
    }
}
