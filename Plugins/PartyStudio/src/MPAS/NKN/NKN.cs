using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.IO;
using System.Security.Cryptography;

namespace PartyStudioPlugin
{
    public class NKN
    {
        private static readonly string AES_KEY = "au3x5kBAnBbxqsqB";
        private static readonly string AES_IV = "L8bdU63qcwpNYvR7";

        public static string Decrypt(Stream stream)
        {
            using (var reader = new BinaryReader(stream)) {
                byte[] encodedContents = reader.ReadBytes((int)reader.BaseStream.Length);

                AesEncryption.SetKey(AES_KEY);
                AesEncryption.SetIV(AES_IV);
               return AesEncryption.AesDecrypt(encodedContents);
            }
        }

        public static byte[] Encrypt(string contents)
        {
            var mem = new MemoryStream();
            using (var writer = new BinaryWriter(mem)) {
                AesEncryption.SetKey(AES_KEY);
                AesEncryption.SetIV(AES_IV);
                writer.Write(AesEncryption.AesEncrypt(IntoBytes(contents)));
            }
            return mem.ToArray();
        }

        static byte[] IntoBytes(string contents)
        {
            var mem = new MemoryStream();
            using (var writer = new FileWriter(mem))
            {
                writer.Write(Encoding.UTF8.GetBytes(contents));
                writer.AlignBytes(64);
            }
            return mem.ToArray();
        }
    }
}
