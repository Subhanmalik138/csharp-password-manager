using System.Security.Cryptography;
using VaultKey.Core;

namespace VaultKey.Tests;

public class CryptoTests
{
    private readonly ICryptoService _crypto = new AesGcmCryptoService();

    [Fact]
    public void Roundtrip_RestoresPlaintext()
    {
        var encrypted = _crypto.Encrypt("hello vault", "correct horse");
        Assert.Equal("hello vault", _crypto.Decrypt(encrypted, "correct horse"));
    }

    [Fact]
    public void WrongPassword_ThrowsCryptographicException()
    {
        var encrypted = _crypto.Encrypt("secret", "right-password");
        Assert.Throws<CryptographicException>(() => _crypto.Decrypt(encrypted, "wrong-password"));
    }

    [Fact]
    public void SamePlaintext_ProducesDifferentCiphertext()
    {
        var a = _crypto.Encrypt("same", "password");
        var b = _crypto.Encrypt("same", "password");
        Assert.NotEqual(a.Ciphertext, b.Ciphertext);
        Assert.NotEqual(a.Salt, b.Salt);
        Assert.NotEqual(a.Nonce, b.Nonce);
    }

    [Fact]
    public void TamperedCiphertext_FailsAuthentication()
    {
        var encrypted = _crypto.Encrypt("secret", "password");
        var bytes = Convert.FromBase64String(encrypted.Ciphertext);
        bytes[0] ^= 0xFF;
        var tampered = encrypted with { Ciphertext = Convert.ToBase64String(bytes) };
        Assert.Throws<CryptographicException>(() => _crypto.Decrypt(tampered, "password"));
    }

    [Fact]
    public void EmptyMasterPassword_Throws()
    {
        Assert.Throws<ArgumentException>(() => _crypto.Encrypt("x", ""));
    }
}
