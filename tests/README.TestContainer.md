# TestContainers

![Testing](https://img.shields.io/badge/Testing-TestContainers-blue)
![Integration](https://img.shields.io/badge/Integración-Contenedores-45B7D1)

**TestContainers** es una librería que permite levantar contenedores Docker desechables para tests de integración, proporcionando entornos reales sin necesidad de infraestructura permanente.

---

## ¿Qué es TestContainers?

> "TestContainers for .NET es una librería para crear y gestionar contenedores Docker desde tests. Proporciona instancias limpias y desechables de bases de datos, message brokers, etc."

En el proyecto se usa para levantar un **SQL Server 2022** en un contenedor Docker que se comparte entre todos los tests.

---

## ¿Por qué TestContainers?

| Opción | Problema |
|--------|----------|
| Base de datos compartida | Datos residuales entre tests, conflictos |
| SQL Server LocalDB | No disponible en CI/CD, diferencias con producción |
| Base de datos real | Costosa, requiere conexión de red, estado compartido |
| **TestContainers** | **Aislado, portátil, idéntico a producción, efímero** |

---

## Uso en el proyecto

### Fixture de ciclo de vida global

```csharp
// SoftwareLearningGuide.Helper.Test/Fixtures/SqlServerTestContainer.cs
public class SqlServerTestContainer
{
    private static MsSqlContainer? _container;

    public static string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    public static async Task StartContainer()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithReuse(false)
            .Build();
        await _container.StartAsync();
    }

    public static async Task StopContainer()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
```

### Fixture NUnit que lo orquesta

```csharp
// SoftwareLearningGuide.Helper.Test/Fixtures/TestSqlServerTestContainerFixture.cs
[SetUpFixture]
public class TestSqlServerTestContainerFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp() => await SqlServerTestContainer.StartContainer();

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await SqlServerTestContainer.StopContainer();
}
```

### Consumo desde tests

```csharp
public abstract class E2ETestBase
{
    protected E2ETestBase()
    {
        ConnectionString = SqlServerTestContainer.ConnectionString;
        // Usa la conexion del contenedor para el WebApplicationFactory
    }
}
```

---

## Anatomía

```
┌─────────────────────────────────────────────────────────┐
│  TestSqlServerTestContainerFixture (NUnit SetUpFixture) │
│  [OneTimeSetUp]  →  SqlServerTestContainer.Start()      │
│  [OneTimeTearDown] → SqlServerTestContainer.Stop()      │
└─────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│  SqlServerTestContainer                                 │
│  - MsSqlBuilder: mcr.microsoft.com/mssql/server:2022    │
│  - ConnectionString compartida estáticamente            │
│  - Singleton de contenedor para toda la sesión de tests │
└─────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────┐
│  Consumidores:                                          │
│  - E2ETestBase (Api.Test) → HttpClient + TestContainers │
│  - EFDatabase<T> (Helper.Test) → DbContext + Respawn    │
└─────────────────────────────────────────────────────────┘
```

---

## Ventajas en CI/CD

| Aspecto | Beneficio |
|---------|-----------|
| **No requiere SQL Server instalado** | Solo Docker |
| **Misma versión que producción** | `mcr.microsoft.com/mssql/server:2022-latest` |
| **Aislamiento completo** | Cada ejecución de tests arranca su propio contenedor |
| **Sin estado compartido** | Contenedor fresco en cada sesión |
| **Efímero** | Se destruye al terminar, sin limpieza manual |

---

## Configuración disponible

```csharp
new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
    .WithPassword("Custom_Passw0rd!")     // Contraseña del SA
    .WithEnvironment("ACCEPT_EULA", "Y")  // Aceptar licencia
    .WithPortBinding(1433, true)          // Puerto fijo vs aleatorio
    .WithReuse(true)                      // Reutilizar contenedor entre ejecuciones
    .Build();
```

---

## Referencias

- [TestContainers for .NET](https://dotnet.testcontainers.org/)
- [GitHub: TestContainers](https://github.com/testcontainers/testcontainers-dotnet)

**Ver también:**
- [`README.Respawn.md`](README.Respawn.md) — Respawn para limpiar datos entre tests
- [`README.TDD.md`](README.TDD.md) — Ciclo TDD y buenas prácticas de testing
- [`README.ResponseFixture.md`](README.ResponseFixture.md) — Fixtures JSON para asserts en tests
