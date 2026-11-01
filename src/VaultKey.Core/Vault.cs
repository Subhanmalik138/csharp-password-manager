namespace VaultKey.Core;

/// <summary>
/// An in-memory collection of entries. Site names are unique within a
/// vault (case-insensitive) so a lookup can never be ambiguous.
/// </summary>
public sealed class Vault
{
    private readonly List<VaultEntry> _entries = new();

    public Vault(IEnumerable<VaultEntry>? entries = null)
    {
        if (entries is not null)
            foreach (var entry in entries)
                Add(entry);
    }

    public IReadOnlyList<VaultEntry> Entries => _entries.AsReadOnly();

    public void Add(VaultEntry entry)
    {
        entry.Validate();
        if (Find(entry.Site) is not null)
            throw new InvalidOperationException($"An entry for '{entry.Site}' already exists.");
        _entries.Add(entry);
    }

    public VaultEntry? Find(string site) =>
        _entries.FirstOrDefault(e =>
            string.Equals(e.Site, site, StringComparison.OrdinalIgnoreCase));

    public bool Remove(string site)
    {
        var entry = Find(site);
        return entry is not null && _entries.Remove(entry);
    }

    public IEnumerable<VaultEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _entries;
        return _entries.Where(e =>
            e.Site.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            e.Username.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            e.Category.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<VaultEntry> ByCategory(string category) =>
        _entries.Where(e =>
            string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
}
