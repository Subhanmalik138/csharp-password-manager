using VaultKey.Core;

namespace VaultKey.Tests;

public class VaultTests
{
    private static VaultEntry Entry(string site, string user = "u", string category = "General") =>
        new() { Site = site, Username = user, Password = "p@ssw0rd!", Category = category };

    [Fact]
    public void Add_ThenFind_ReturnsEntry()
    {
        var vault = new Vault();
        vault.Add(Entry("github.com"));
        Assert.Equal("github.com", vault.Find("github.com")!.Site);
    }

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        var vault = new Vault([Entry("GitHub.com")]);
        Assert.NotNull(vault.Find("github.COM"));
    }

    [Fact]
    public void DuplicateSite_Throws()
    {
        var vault = new Vault([Entry("github.com")]);
        Assert.Throws<InvalidOperationException>(() => vault.Add(Entry("GITHUB.com")));
    }

    [Fact]
    public void Remove_DeletesAndReturnsTrue_ThenFalse()
    {
        var vault = new Vault([Entry("a.com")]);
        Assert.True(vault.Remove("a.com"));
        Assert.False(vault.Remove("a.com"));
    }

    [Fact]
    public void Search_MatchesSiteUsernameAndCategory()
    {
        var vault = new Vault([Entry("github.com", "alice", "Work"), Entry("gmail.com", "bob")]);
        Assert.Single(vault.Search("git"));
        Assert.Single(vault.Search("alice"));
        Assert.Single(vault.Search("work"));
        Assert.Equal(2, vault.Search("").Count());
    }

    [Fact]
    public void EntryValidation_RejectsEmptyFields()
    {
        var vault = new Vault();
        Assert.Throws<ArgumentException>(() => vault.Add(Entry("")));
        Assert.Throws<ArgumentException>(() => vault.Add(Entry("x.com", "")));
    }
}
