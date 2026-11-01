using System.Text.Json;

namespace VaultKey.Core;

/// <summary>
/// Persists a <see cref="Vault"/> to one encrypted JSON file. Saves are
/// atomic: the new vault is written to a temp file in the same directory
/// and then moved over the old file, so a crash can never leave a
/// half-written vault behind.
/// </summary>
public sealed class VaultStore
{
    private readonly ICryptoService _crypto;

    public VaultStore(ICryptoService? crypto = null) =>
        _crypto = crypto ?? new AesGcmCryptoService();

    public void Save(string path, Vault vault, string masterPassword)
    {
        var json = JsonSerializer.Serialize(vault.Entries);
        var encrypted = _crypto.Encrypt(json, masterPassword);
        var fileJson = JsonSerializer.Serialize(encrypted);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        var tempPath = Path.Combine(directory, "." + Path.GetFileName(path) + ".tmp");
        File.WriteAllText(tempPath, fileJson);
        File.Move(tempPath, path, overwrite: true);
    }

    public Vault Load(string path, string masterPassword)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Vault file not found: {path}", path);

        var fileJson = File.ReadAllText(path);
        var encrypted = JsonSerializer.Deserialize<EncryptedVault>(fileJson)
            ?? throw new InvalidDataException("Vault file is not valid JSON.");
        var json = _crypto.Decrypt(encrypted, masterPassword);
        var entries = JsonSerializer.Deserialize<List<VaultEntry>>(json) ?? [];
        return new Vault(entries);
    }

    public bool Exists(string path) => File.Exists(path);
}
