using System.Security.Cryptography;
using System.Text;

namespace QuasarApi.Helpers
{
    public class CryptoHelper
    {
        // Definição da quantidade de iterações
        //private const int Iterations = 10000;

        //// Criptografar AES - usando Iterations
        //public static string StringToCrypto(string pwd)
        //{
        //    string EncryptionKey = "MAKV2SPBNI99212";
        //    byte[] clearBytes = Encoding.Unicode.GetBytes(pwd);
        //    using (Aes encryptor = Aes.Create())
        //    {
        //        var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, Iterations, HashAlgorithmName.SHA256);
        //        encryptor.Key = pdb.GetBytes(32);
        //        encryptor.IV = pdb.GetBytes(16);
        //        using (var ms = new MemoryStream())
        //        {
        //            using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
        //            {
        //                cs.Write(clearBytes, 0, clearBytes.Length);
        //                cs.Close();
        //            }
        //            return Convert.ToBase64String(ms.ToArray());
        //        }
        //    }
        //}

        //// Descriptografar AES - usando Iterations
        //public static string CryptoToString(string pwd)
        //{
        //    string EncryptionKey = "MAKV2SPBNI99212";
        //    byte[] cipherBytes = Convert.FromBase64String(pwd);
        //    using (Aes encryptor = Aes.Create())
        //    {
        //        var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }, Iterations, HashAlgorithmName.SHA256);
        //        encryptor.Key = pdb.GetBytes(32);
        //        encryptor.IV = pdb.GetBytes(16);
        //        using (var ms = new MemoryStream())
        //        {
        //            using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
        //            {
        //                cs.Write(cipherBytes, 0, cipherBytes.Length);
        //                cs.Close();
        //            }
        //            return Encoding.Unicode.GetString(ms.ToArray());
        //        }
        //    }
        //}

        //// Criptografar AES
        //public static string StringToCryptoOld(string pwd)
        //{
        //    string EncryptionKey = "MAKV2SPBNI99212";
        //    byte[] clearBytes = Encoding.Unicode.GetBytes(pwd);
        //    using (Aes encryptor = Aes.Create())
        //    {
        //        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
        //        encryptor.Key = pdb.GetBytes(32);
        //        encryptor.IV = pdb.GetBytes(16);
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
        //            {
        //                cs.Write(clearBytes, 0, clearBytes.Length);
        //                cs.Close();
        //            }
        //            return Convert.ToBase64String(ms.ToArray());
        //        }
        //    }
        //}

        //// Descriptografar AES
        //public static string CryptoToStringOld(string pwd)
        //{
        //    string EncryptionKey = "MAKV2SPBNI99212";
        //    byte[] cipherBytes = Convert.FromBase64String(pwd);
        //    using (Aes encryptor = Aes.Create())
        //    {
        //        Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
        //        encryptor.Key = pdb.GetBytes(32);
        //        encryptor.IV = pdb.GetBytes(16);
        //        using (MemoryStream ms = new MemoryStream())
        //        {
        //            using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
        //            {
        //                cs.Write(cipherBytes, 0, cipherBytes.Length);
        //                cs.Close();
        //            }
        //            return Encoding.Unicode.GetString(ms.ToArray());
        //        }
        //    }
        //}

        // Validar senha
        public static bool ValidatePassword(string password, string correctHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, correctHash);
        }

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, GetRandomSalt());
        }

        // Gerar hash
        private static string GetRandomSalt()
        {
            return BCrypt.Net.BCrypt.GenerateSalt(12);
        }

    }
}

