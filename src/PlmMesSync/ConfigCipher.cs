using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PlmMesSync;

public static class ConfigCipher
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("PLM_MES_SAP_Ponto_2024");

    public static string DecryptConnectionString(string connectionString)
    {
        return Regex.Replace(connectionString, @"ENC\(([^)]+)\)", match =>
        {
            var encrypted = Convert.FromBase64String(match.Groups[1].Value);
            var decrypted = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(decrypted);
        });
    }

    public static string Encrypt(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.LocalMachine);
        return Convert.ToBase64String(encrypted);
    }
}
