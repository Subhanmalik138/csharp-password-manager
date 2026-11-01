# VaultKey — Encrypted CLI Password Manager

A small, dependency-free password manager for the terminal. Your credentials
live in a single file encrypted with AES-GCM under a key derived from your
master password (PBKDF2-SHA256), so nothing sensitive is ever stored in
plaintext. No accounts, no cloud, no sync — just one vault file you control.

## Features

- AES-GCM authenticated encryption: a wrong master password or a tampered
  vault file fails loudly instead of returning garbage
- PBKDF2-SHA256 key derivation (100,000 iterations, random 16-byte salt)
- Atomic vault saves (write to a temp file, then move) so a crash mid-save
  cannot corrupt your vault
- Add, get, list, search, remove, and rotate the master password
- Cryptographically secure password generator (letters, digits, symbols,
  optional exclusion of ambiguous characters)
- Password strength estimator (entropy + common-pattern penalties)

## Quick start

```bash
dotnet run --project src/VaultKey.Cli -- init --file myvault.json
dotnet run --project src/VaultKey.Cli -- add --file myvault.json \
    --site github.com --username subhan --generate
dotnet run --project src/VaultKey.Cli -- get --file myvault.json --site github.com
dotnet run --project src/VaultKey.Cli -- list --file myvault.json
dotnet run --project src/VaultKey.Cli -- generate --length 20
```

You will be prompted for the master password (input is hidden). For scripts,
pass `--password` instead — only do that in a trusted environment, since the
password then appears in your shell history.

## Design decisions

1. **Whole-vault encryption, not per-entry encryption.** Encrypting the
   serialized entry list as one blob means metadata (site names, usernames,
   categories) is protected too, and there is exactly one authentication tag
   to verify. The cost — rewriting the whole file on every change — is
   negligible for a personal vault.
2. **AES-GCM + PBKDF2, both from `System.Security.Cryptography`.** No
   third-party crypto dependency means a smaller attack surface and nothing
   to keep patched. PBKDF2 at 100k iterations is deliberately slow enough
   to blunt offline guessing while staying instant for a human.
3. **Atomic saves via temp file + move.** A vault is only ever replaced,
   never edited in place, so an interrupted write leaves either the old
   vault or the new one — never a half-written file.
4. **Site names unique (case-insensitive).** `get` and `remove` can never be
   ambiguous, and a duplicate `add` fails instead of silently shadowing an
   entry you might lose.

## Security notes

- The master password is never stored. Lose it and the vault is
  unrecoverable — by design.
- VaultKey is a learning project, not an audited security product. It is
  suitable for personal use; do not rely on it for high-value secrets
  without review.

## Project layout

```
src/VaultKey.Core/   VaultEntry, Vault, AES-GCM crypto, vault store,
                     password generator, strength estimator
src/VaultKey.Cli/    Command-line interface (init/add/get/list/search/
                     remove/generate/change-password)
tests/VaultKey.Tests/ xUnit tests for crypto, store, vault, and generator
```

## Tests

```bash
dotnet test
```

## Tech stack

- C# / .NET 8, zero third-party runtime dependencies
- xUnit for tests
