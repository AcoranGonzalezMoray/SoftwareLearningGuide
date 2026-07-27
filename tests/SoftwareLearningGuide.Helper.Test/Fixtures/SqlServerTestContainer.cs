using Testcontainers.MsSql;

namespace SoftwareLearningGuide.Helper.Test.Fixtures;

public class SqlServerTestContainer {
    private static MsSqlContainer? _container;

    public static string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    public static async Task StartContainer() {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithReuse(false)
            .Build();
        await _container.StartAsync();
    }

    public static async Task StopContainer() {
        if (_container != null) {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}