using System.Security.Cryptography;
using System.Text;

namespace VaultKey.Core;

/// <summary>
/// AES-256-GCM encryption with a PBKDF2-SHA256 derived key. Every vault
/// gets a fresh random salt and nonce, so identical vaults encrypted under
/// the same password still produce different ciphertexts.
/// </summary>
public sealed class AesGcmCryptoService : ICryptoService
{
    private const int KeyBytes = 32;   // AES-256
    private const int SaltBytes = 16;
    private const int NonceBytes = 12; // GCM recommended size
    private const int TagBytes = 16;
    private const int Iterations = 100_000;

    public EncryptedVault Encrypt(string plaintext, string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var key = DeriveKey(masterPassword, salt);

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagBytes];

        using (var aes = new AesGcm(key, TagBytes))
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        CryptographicOperations.ZeroMemory(key);
        return new EncryptedVault(
            Convert.ToBase64String(salt),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(cipherBytes),
            Convert.ToBase64String(tag));
    }

    public string Decrypt(EncryptedVault vault, string masterPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(masterPassword);

        var salt = Convert.FromBase64String(vault.Salt);
        var nonce = Convert.FromBase64String(vault.Nonce);
        var cipherBytes = Convert.FromBase64String(vault.Ciphertext);
        var tag = Convert.FromBase64String(vault.Tag);
        var key = DeriveKey(masterPassword, salt);

        var plainBytes = new byte[cipherBytes.Length];
        try
        {
            using (var aes = new AesGcm(key, TagBytes))
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] DeriveKey(string masterPassword, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(masterPassword),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeyBytes);
}
