using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class PRCrypto 
{
    public static int KeyLength = 128;
    private const string SaltKey = "ShMG8hLyZ7k~Ge5@";
    private const string VIKey = "~6YUi0Sv5@|{aOZO";
    public const string Password = "~dASDF32523tsdfgsdgfsedgsd"; // TODO: Generate random VI each encryption and store it with encrypted value

    public static string Encrypt(byte[] value, string password)
    {
        var keyBytes = new Rfc2898DeriveBytes(Password, Encoding.UTF8.GetBytes(SaltKey)).GetBytes(KeyLength / 8);
        var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.Zeros };
        var encryptor = symmetricKey.CreateEncryptor(keyBytes, Encoding.UTF8.GetBytes(VIKey));

        using (var memoryStream = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(value, 0, value.Length);
                cryptoStream.FlushFinalBlock();
                cryptoStream.Close();
                memoryStream.Close();

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
    }

    public static string EncryptString(string value, string password)
    {
        return Encrypt(Encoding.UTF8.GetBytes(value), password);
    }

    public static string DecryptString(string value, string password)
    {
        var cipherTextBytes = Convert.FromBase64String(value);
        var keyBytes = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(SaltKey)).GetBytes(KeyLength / 8);
        var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC, Padding = PaddingMode.None };
        var decryptor = symmetricKey.CreateDecryptor(keyBytes, Encoding.UTF8.GetBytes(VIKey));

        using (var memoryStream = new MemoryStream(cipherTextBytes))
        {
            using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
            {
                var plainTextBytes = new byte[cipherTextBytes.Length];
                var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);

                memoryStream.Close();
                cryptoStream.Close();

                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount).TrimEnd("\0".ToCharArray());
            }
        }
    }
}
