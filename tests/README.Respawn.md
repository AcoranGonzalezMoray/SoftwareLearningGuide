# Respawn

![Testing](https://img.shields.io/badge/Testing-Respawn-brightgreen)
![Database](https://img.shields.io/badge/Database-Reset-FF6B6B)

**Respawn** es una librería ligera para .NET que restaura la base de datos a un estado limpio entre pruebas, eliminando datos sin perder el esquema ni las migraciones.

---

## ¿Qué es Respawn?

> Respawn elimina datos de todas las tablas respetando restricciones de clave foránea, dejando la base de datos vacía pero con su estructura intacta.

Es la alternativa moderna a "recrear la base de datos desde cero antes de cada test" — mucho más rápida porque solo ejecuta `DELETE` en orden topológico.

---

## ¿Por qué Respawn?

| Opción | Problema |
|--------|----------|
| `DropDatabase` + Migrate | Lento, especialmente con migraciones complejas |
| `EnsureDeleted` | Destructivo, borra la base de datos entera |
| Delete manual por tabla | Propenso a errores de FK, olvidos, código repetitivo |
| **Respawn** | **Rápido, automático, respeta el esquema** |

---

## Uso en el proyecto

```csharp
using Respawn;

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
{
    TablesToIgnore = ["__EFMigrationsHistory"], // No tocar la tabla de migraciones
    SchemasToExclude = [],
    DbAdapter = DbAdapter.SqlServer
});

await respawner.ResetAsync(connection);
```

Se usa en:
- [`E2ETestBase.cs`](SoftwareLearningGuide.Api.Test/E2E/E2ETestBase.cs) — limpieza entre tests E2E
- [`EFDatabase.cs`](SoftwareLearningGuide.Helper.Test/Fixtures/EFDatabase.cs) — limpieza entre tests de infraestructura

---

## ¿Cómo funciona?

1. **Analiza el esquema** — descubre todas las tablas, columnas, FK y dependencias
2. **Orden topológico** — elimina datos de tablas hijas antes que padres (respeta FK)
3. **Resetea identidades** — opcionalmente reinicia semillas de columnas `IDENTITY`
4. **Ignora tablas** — `__EFMigrationsHistory` se excluye para no perder el historial de migraciones

---

## Configuración

```csharp
var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
{
    TablesToIgnore = ["__EFMigrationsHistory"],
    SchemasToExclude = [],
    DbAdapter = DbAdapter.SqlServer,
    // WithReseed = true         // Reinicia Identity columns
    // SchemasToInclude = ["dbo"] // Solo ciertos schemas
});
```

---

## Respawn vs otras alternativas

| Alternativa | Velocidad | Mantenimiento | Complejidad |
|-------------|-----------|---------------|-------------|
| **Respawn** | Alta | Bajo | Baja |
| `DELETE FROM tablas` manual | Media | Alto | Media |
| Recrear BD desde script | Baja | Alto | Alta |
| Transacciones con rollback | Alta | Medio | Alta |

---

## Referencias

- [Respawn GitHub](https://github.com/jbogard/Respawn)
- [Documentación oficial](https://github.com/jbogard/Respawn#respawn)

**Ver también:**
- [`README.TestContainer.md`](README.TestContainer.md) — TestContainers para levantar SQL Server en contenedores
- [`README.TDD.md`](README.TDD.md) — Ciclo TDD y buenas prácticas de testing
- [`README.ResponseFixture.md`](README.ResponseFixture.md) — Fixtures JSON para asserts en tests
