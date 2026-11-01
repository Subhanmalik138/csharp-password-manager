namespace VaultKey.Core;

/// <summary>
/// A single credential stored in the vault. Passwords are held as plain
/// strings only in memory; on disk the whole vault is AES-GCM encrypted.
/// </summary>
public sealed record VaultEntry
{
    public required string Site { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string Category { get; init; } = "General";
    public string Notes { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Site))
            throw new ArgumentException("Site must not be empty.", nameof(Site));
        if (string.IsNullOrWhiteSpace(Username))
            throw new ArgumentException("Username must not be empty.", nameof(Username));
        if (string.IsNullOrWhiteSpace(Password))
            throw new ArgumentException("Password must not be empty.", nameof(Password));
    }
}
