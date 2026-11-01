using System.Security.Cryptography;

namespace VaultKey.Core;

/// <summary>
/// Encrypts and decrypts the serialized vault. Implementations must be
/// authenticated encryption: a wrong master password (or a tampered file)
/// must fail decryption rather than return garbage.
/// </summary>
public interface ICryptoService
{
    /// <summary>Encrypts <paramref name="plaintext"/> under <paramref name="masterPassword"/>.</summary>
    EncryptedVault Encrypt(string plaintext, string masterPassword);

    /// <summary>Decrypts a vault produced by <see cref="Encrypt"/>.</summary>
    /// <exception cref="CryptographicException">Wrong password or tampered data.</exception>
    string Decrypt(EncryptedVault vault, string masterPassword);
}

/// <summary>Wire format of one encrypted vault file (all fields Base64).</summary>
public sealed record EncryptedVault(
    string Salt,
    string Nonce,
    string Ciphertext,
    string Tag);
