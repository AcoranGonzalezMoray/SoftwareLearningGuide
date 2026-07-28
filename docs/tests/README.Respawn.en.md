# Respawn

![Testing](https://img.shields.io/badge/Testing-Respawn-brightgreen)
![Database](https://img.shields.io/badge/Database-Reset-FF6B6B)

**Respawn** is a lightweight .NET library that restores the database to a clean state between tests, deleting data without losing the schema or migrations.

---

## What is Respawn?

> Respawn deletes data from all tables respecting foreign key constraints, leaving the database empty but with its structure intact.

It's the modern alternative to "recreate the database from scratch before each test" — much faster because it only executes `DELETE` in topological order.

---

## Why Respawn?

| Option | Problem |
|--------|----------|
| `DropDatabase` + Migrate | Slow, especially with complex migrations |
| `EnsureDeleted` | Destructive, deletes the entire database |
| Manual delete per table | Prone to FK errors, omissions, repetitive code |
| **Respawn** | **Fast, automatic, respects the schema** |

---

## Usage in the project

```csharp
using Respawn;

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
{
    TablesToIgnore = ["__EFMigrationsHistory"], // Don't touch the migrations table
    SchemasToExclude = [],
    DbAdapter = DbAdapter.SqlServer
});

await respawner.ResetAsync(connection);
```

It's used in:
- [`E2ETestBase.cs`](SoftwareLearningGuide.Api.Test/E2E/E2ETestBase.cs) — cleanup between E2E tests
- [`EFDatabase.cs`](SoftwareLearningGuide.Helper.Test/Fixtures/EFDatabase.cs) — cleanup between infrastructure tests

---

## How does it work?

1. **Analyzes the schema** — discovers all tables, columns, FKs and dependencies
2. **Topological order** — deletes data from child tables before parents (respects FK)
3. **Resets identities** — optionally restarts `IDENTITY` column seeds
4. **Ignores tables** — `__EFMigrationsHistory` is excluded to preserve migration history

---

## Configuration

```csharp
var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
{
    TablesToIgnore = ["__EFMigrationsHistory"],
    SchemasToExclude = [],
    DbAdapter = DbAdapter.SqlServer,
    // WithReseed = true         // Restart Identity columns
    // SchemasToInclude = ["dbo"] // Only certain schemas
});
```

---

## Respawn vs other alternatives

| Alternative | Speed | Maintenance | Complexity |
|-------------|-----------|---------------|-------------|
| **Respawn** | High | Low | Low |
| Manual `DELETE FROM tables` | Medium | High | Medium |
| Recreate DB from script | Low | High | High |
| Transactions with rollback | High | Medium | High |

---

## References

- [Respawn GitHub](https://github.com/jbogard/Respawn)
- [Official documentation](https://github.com/jbogard/Respawn#respawn)

**See also:**
- [`README.TestContainer.md`](README.TestContainer.md) — TestContainers for spinning up SQL Server in containers
- [`README.TDD.md`](README.TDD.md) — TDD cycle and testing best practices
- [`README.ResponseFixture.md`](README.ResponseFixture.md) — JSON fixtures for test asserts
