using VaultKey.Core;

namespace VaultKey.Tests;

public class StoreTests
{
    [Fact]
    public void SaveLoad_RoundtripsEntries()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new VaultStore();
            var vault = new Vault([
                new VaultEntry { Site = "a.com", Username = "u1", Password = "pw1" },
                new VaultEntry { Site = "b.com", Username = "u2", Password = "pw2", Category = "Work" },
            ]);
            store.Save(path, vault, "master");
            var loaded = store.Load(path, "master");
            Assert.Equal(2, loaded.Entries.Count);
            Assert.Equal("u2", loaded.Find("b.com")!.Username);
            Assert.Equal("Work", loaded.Find("b.com")!.Category);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Save_LeavesNoTempFileBehind()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        try
        {
            new VaultStore().Save(Path.Combine(dir, "v.json"), new Vault(), "master");
            Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() =>
            new VaultStore().Load("/tmp/definitely-not-here-vault.json", "master"));
    }
}

public class GeneratorTests
{
    private readonly PasswordGenerator _generator = new();

    [Fact]
    public void Generate_HasRequestedLength()
    {
        Assert.Equal(24, _generator.Generate(24).Length);
    }

    [Fact]
    public void Generate_ContainsAllEnabledClasses()
    {
        var password = _generator.Generate(32);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, c => !char.IsLetterOrDigit(c));
    }

    [Fact]
    public void Generate_WithoutSymbols_HasNoSymbols()
    {
        var password = _generator.Generate(32, includeSymbols: false);
        Assert.All(password, c => Assert.True(char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void Generate_ExcludeAmbiguous_HasNone()
    {
        var password = _generator.Generate(64, excludeAmbiguous: true);
        Assert.DoesNotContain('l', password);
        Assert.DoesNotContain('1', password);
        Assert.DoesNotContain('I', password);
        Assert.DoesNotContain('O', password);
        Assert.DoesNotContain('0', password);
    }

    [Fact]
    public void Generate_ProducesUniquePasswords()
    {
        var passwords = Enumerable.Range(0, 100).Select(_ => _generator.Generate()).ToHashSet();
        Assert.Equal(100, passwords.Count);
    }

    [Fact]
    public void Generate_TooShort_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate(3));
    }
}

public class StrengthTests
{
    [Fact]
    public void CommonPassword_IsVeryWeak()
    {
        var (_, level) = PasswordStrength.Evaluate("password");
        Assert.Equal(PasswordStrength.Level.VeryWeak, level);
    }

    [Fact]
    public void LongMixedPassword_IsStrong()
    {
        var (_, level) = PasswordStrength.Evaluate("Tr0ub4dor&Xk9!zQw2");
        Assert.True(level is PasswordStrength.Level.Strong or PasswordStrength.Level.VeryStrong);
    }

    [Fact]
    public void EmptyPassword_IsVeryWeak()
    {
        var (entropy, level) = PasswordStrength.Evaluate("");
        Assert.Equal(0, entropy);
        Assert.Equal(PasswordStrength.Level.VeryWeak, level);
    }
}
