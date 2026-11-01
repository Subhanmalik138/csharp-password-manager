namespace VaultKey.Core;

/// <summary>
/// Rough strength estimate: Shannon-style entropy from the character pool
/// actually used, minus penalties for the patterns attackers try first
/// (short length, repeated characters, common sequences and passwords).
/// A score, not a verdict — it exists to nudge, not to certify.
/// </summary>
public static class PasswordStrength
{
    public enum Level { VeryWeak, Weak, Fair, Strong, VeryStrong }

    private static readonly string[] CommonPasswords =
        ["password", "123456", "12345678", "qwerty", "letmein", "admin",
         "welcome", "monkey", "dragon", "football", "123456789", "123123"];

    public static (double EntropyBits, Level Strength) Evaluate(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (0, Level.VeryWeak);

        var pool = 0;
        if (password.Any(char.IsLower)) pool += 26;
        if (password.Any(char.IsUpper)) pool += 26;
        if (password.Any(char.IsDigit)) pool += 10;
        if (password.Any(c => !char.IsLetterOrDigit(c))) pool += 32;

        var entropy = password.Length * Math.Log2(Math.Max(pool, 2));

        // Penalties for patterns that make a password far weaker than its
        // length suggests.
        if (password.Length < 8) entropy -= 20;
        if (password.Distinct().Count() < password.Length / 2) entropy -= 15;
        if (CommonPasswords.Contains(password.ToLowerInvariant())) entropy = 0;
        if (HasSequence(password)) entropy -= 15;

        var level = entropy switch
        {
            < 28 => Level.VeryWeak,
            < 40 => Level.Weak,
            < 60 => Level.Fair,
            < 80 => Level.Strong,
            _ => Level.VeryStrong,
        };
        return (Math.Max(entropy, 0), level);
    }

    private static bool HasSequence(string password)
    {
        var lower = password.ToLowerInvariant();
        for (var i = 0; i + 3 <= lower.Length; i++)
        {
            var slice = lower[i..(i + 3)];
            if (slice is "abc" or "bcd" or "cde" or "123" or "234" or "345"
                or "qwe" or "asd" or "zxc")
                return true;
        }
        return false;
    }
}
