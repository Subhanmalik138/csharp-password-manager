using System.Security.Cryptography;

namespace VaultKey.Core;

/// <summary>
/// Generates passwords with <see cref="RandomNumberGenerator"/> (never
/// <see cref="Random"/>). Guarantees at least one character from every
/// enabled class so the result is never, say, all lowercase by chance.
/// </summary>
public sealed class PasswordGenerator
{
    public const string Lower = "abcdefghijklmnopqrstuvwxyz";
    public const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string Digits = "0123456789";
    public const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?";
    public const string Ambiguous = "l1IO0";

    public string Generate(
        int length = 20,
        bool includeSymbols = true,
        bool excludeAmbiguous = false)
    {
        if (length < 4)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be at least 4.");

        var classes = new List<string> { Lower, Upper, Digits };
        if (includeSymbols)
            classes.Add(Symbols);

        var pool = string.Concat(classes);
        if (excludeAmbiguous)
        {
            pool = new string(pool.Where(c => !Ambiguous.Contains(c)).ToArray());
            classes = classes
                .Select(cls => new string(cls.Where(c => !Ambiguous.Contains(c)).ToArray()))
                .Where(cls => cls.Length > 0)
                .ToList();
        }

        // One guaranteed character from each class, then fill the rest.
        var chars = classes.Select(cls => cls[RandomNumberGenerator.GetInt32(cls.Length)]).ToList();
        while (chars.Count < length)
            chars.Add(pool[RandomNumberGenerator.GetInt32(pool.Length)]);

        // Shuffle so the guaranteed characters are not all at the front.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
    }
}
