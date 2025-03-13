using MFAWPF.Helper;
using Sodium;
using System;
using System.Text;
using System.Security.Cryptography;
namespace MFAAvalonia.Helper;

public static class SimpleEncryptionHelper
{
    private static readonly byte[] _key = SecretBox.GenerateKey();

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        
        try
        {
            var nonce = SecretBox.GenerateNonce();
            var cipherText = SecretBox.Create(plainText, nonce, _key);
            return Convert.ToBase64String(nonce) + ":" + Convert.ToBase64String(cipherText);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;

        try
        {
            var parts = encryptedText.Split(':');
            var nonce = Convert.FromBase64String(parts[0]);
            var cipherText = Convert.FromBase64String(parts[1]);
            byte[] decryptedBytes = SecretBox.Open(cipherText, nonce, _key); 
            return Encoding.UTF8.GetString(decryptedBytes); 
        }
        catch
        {
            return string.Empty; 
        }
    }
}
