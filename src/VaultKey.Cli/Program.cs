using VaultKey.Core;

var store = new VaultStore();
var generator = new PasswordGenerator();

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    PrintUsage();
    return args.Length == 0 ? 1 : 0;
}

try
{
    return args[0] switch
    {
        "init" => Init(Options(args[1..])),
        "add" => Add(Options(args[1..])),
        "get" => Get(Options(args[1..])),
        "list" => List(Options(args[1..])),
        "search" => Search(Options(args[1..])),
        "remove" => Remove(Options(args[1..])),
        "generate" => Generate(Options(args[1..])),
        "change-password" => ChangePassword(Options(args[1..])),
        _ => Unknown(args[0]),
    };
}
catch (System.Security.Cryptography.CryptographicException)
{
    Console.Error.WriteLine("Error: wrong master password, or the vault file is corrupted.");
    return 1;
}
catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException
    or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    PrintUsage();
    return 1;
}

void PrintUsage()
{
    Console.WriteLine("""
        VaultKey — encrypted CLI password manager

        Usage: vaultkey <command> [options]
          init [--file vault.json] [--force]
          add --site SITE --username USER [--password PASS | --generate]
              [--length N] [--no-symbols] [--category CAT] [--notes TEXT]
              [--file vault.json]
          get --site SITE [--file vault.json]
          list [--category CAT] [--file vault.json]
          search QUERY [--file vault.json]
          remove --site SITE [--file vault.json]
          generate [--length N] [--no-symbols] [--exclude-ambiguous]
          change-password [--file vault.json]

        The master password is prompted for (hidden input) unless --password
        is passed. --file defaults to vault.json in the current directory.
        """);
}

/// <summary>Minimal `--key value` / `--flag` parser; no external dependency.</summary>
Dictionary<string, string> Options(string[] rest)
{
    var opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < rest.Length; i++)
    {
        if (!rest[i].StartsWith("--", StringComparison.Ordinal))
        {
            opts["__positional"] = rest[i];
            continue;
        }
        var key = rest[i][2..];
        opts[key] = i + 1 < rest.Length && !rest[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? rest[++i]
            : "true";
    }
    return opts;
}

string VaultPath(Dictionary<string, string> opts) =>
    opts.TryGetValue("file", out var file) ? file : "vault.json";

string Required(Dictionary<string, string> opts, string key) =>
    opts.TryGetValue(key, out var value)
        ? value
        : throw new ArgumentException($"Missing required option --{key}.");

string MasterPassword(Dictionary<string, string> opts, string key = "password")
{
    if (opts.TryGetValue(key, out var password))
        return password;
    Console.Write("Master password: ");
    return ReadHidden();
}

string ReadHidden()
{
    var chars = new List<char>();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return new string(chars.ToArray());
        }
        if (key.Key == ConsoleKey.Backspace && chars.Count > 0)
            chars.RemoveAt(chars.Count - 1);
        else if (!char.IsControl(key.KeyChar))
            chars.Add(key.KeyChar);
    }
}

Vault Load(Dictionary<string, string> opts, out string master)
{
    master = MasterPassword(opts);
    return store.Load(VaultPath(opts), master);
}

int Init(Dictionary<string, string> opts)
{
    var path = VaultPath(opts);
    if (store.Exists(path) && !opts.ContainsKey("force"))
        throw new InvalidOperationException($"Vault already exists at {path} (pass --force to overwrite).");

    var master = MasterPassword(opts);
    if (master.Length < 4)
        throw new ArgumentException("Master password must be at least 4 characters.");
    store.Save(path, new Vault(), master);
    Console.WriteLine($"Vault created at {path}.");
    return 0;
}

int Add(Dictionary<string, string> opts)
{
    var vault = Load(opts, out var master);
    var site = Required(opts, "site");
    var username = Required(opts, "username");

    string entryPassword;
    if (opts.ContainsKey("generate"))
    {
        var length = opts.TryGetValue("length", out var l) ? int.Parse(l) : 20;
        entryPassword = generator.Generate(length, !opts.ContainsKey("no-symbols"));
        Console.WriteLine($"Generated password: {entryPassword}");
    }
    else
    {
        entryPassword = opts.TryGetValue("entry-password", out var ep) ? ep : ReadEntryPassword();
    }

    var entry = new VaultEntry
    {
        Site = site,
        Username = username,
        Password = entryPassword,
        Category = opts.TryGetValue("category", out var cat) ? cat : "General",
        Notes = opts.TryGetValue("notes", out var notes) ? notes : string.Empty,
    };
    vault.Add(entry);
    store.Save(VaultPath(opts), vault, master);
    Console.WriteLine($"Added entry for {site}.");
    return 0;
}

string ReadEntryPassword()
{
    Console.Write("Password for entry (empty = generate one): ");
    var entered = ReadHidden();
    return entered.Length > 0 ? entered : generator.Generate();
}

int Get(Dictionary<string, string> opts)
{
    var vault = Load(opts, out _);
    var entry = vault.Find(Required(opts, "site"))
        ?? throw new InvalidOperationException("No entry found for that site.");
    var (_, strength) = PasswordStrength.Evaluate(entry.Password);
    Console.WriteLine($"Site:     {entry.Site}");
    Console.WriteLine($"Username: {entry.Username}");
    Console.WriteLine($"Password: {entry.Password}");
    Console.WriteLine($"Category: {entry.Category}");
    Console.WriteLine($"Strength: {strength}");
    if (entry.Notes.Length > 0)
        Console.WriteLine($"Notes:    {entry.Notes}");
    return 0;
}

int List(Dictionary<string, string> opts)
{
    var vault = Load(opts, out _);
    var entries = opts.TryGetValue("category", out var cat)
        ? vault.ByCategory(cat)
        : vault.Entries;
    foreach (var entry in entries)
        Console.WriteLine($"{entry.Site,-30} {entry.Username,-25} [{entry.Category}]");
    return 0;
}

int Search(Dictionary<string, string> opts)
{
    var vault = Load(opts, out _);
    var query = opts.TryGetValue("__positional", out var q) ? q : Required(opts, "query");
    foreach (var entry in vault.Search(query))
        Console.WriteLine($"{entry.Site,-30} {entry.Username,-25} [{entry.Category}]");
    return 0;
}

int Remove(Dictionary<string, string> opts)
{
    var vault = Load(opts, out var master);
    var site = Required(opts, "site");
    if (!vault.Remove(site))
        throw new InvalidOperationException("No entry found for that site.");
    store.Save(VaultPath(opts), vault, master);
    Console.WriteLine($"Removed entry for {site}.");
    return 0;
}

int Generate(Dictionary<string, string> opts)
{
    var length = opts.TryGetValue("length", out var l) ? int.Parse(l) : 20;
    var password = generator.Generate(
        length,
        !opts.ContainsKey("no-symbols"),
        opts.ContainsKey("exclude-ambiguous"));
    var (entropy, strength) = PasswordStrength.Evaluate(password);
    Console.WriteLine(password);
    Console.WriteLine($"Strength: {strength} (~{entropy:F0} bits entropy)");
    return 0;
}

int ChangePassword(Dictionary<string, string> opts)
{
    var vault = Load(opts, out _);
    Console.Write("New master password: ");
    var newMaster = ReadHidden();
    Console.Write("Confirm new master password: ");
    if (ReadHidden() != newMaster)
        throw new ArgumentException("New master passwords do not match.");
    store.Save(VaultPath(opts), vault, newMaster);
    Console.WriteLine("Master password changed.");
    return 0;
}
