using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Respawn;
using SoftwareLearningGuide.Helper.Test.Fixtures;

namespace SoftwareLearningGuide.Helper.Tests.Helper;

public abstract class EFDatabase<T> where T : DbContext {
    protected T Context { get; private set; } = null!;

    protected T GetContext() => Context;

    [SetUp]
    public async Task SetUp() {
        var connectionString = SqlServerTestContainer.ConnectionString;
        var optionsBuilder = new DbContextOptionsBuilder<T>();
        optionsBuilder.UseSqlServer(connectionString);
        Context = (T)Activator.CreateInstance(typeof(T), optionsBuilder.Options)!;
        Context.Database.Migrate();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions {
            TablesToIgnore = ["__EFMigrationsHistory"],
            SchemasToExclude = [],
            DbAdapter = DbAdapter.SqlServer
        });

        await respawner.ResetAsync(connection);
        await connection.CloseAsync();
    }

    [TearDown]
    public async Task TearDown() {
        await Context.DisposeAsync()!;
    }
}