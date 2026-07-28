# Entity Framework Core - Command Side

![EF Core](https://img.shields.io/badge/ORM-Entity_Framework_Core-blue)
![CQRS](https://img.shields.io/badge/Pattern-Command_Side-green)

**Entity Framework Core** se usa en el lado de **Command (escritura)** para persistir agregados y entidades del dominio. Las queries de lectura usan Dapper en su lugar.

En este proyecto, EF Core se usa exclusivamente para el lado de command (escritura). Esto es porque EF Core brilla en operaciones de escritura: tracking automático de cambios, relaciones con Include/ThenInclude, y migraciones de esquema. Para el lado de lectura, Dapper es más rápido y nos da control total del SQL.

---

## Tabla de Contenidos

1. [¿Por qué EF Core solo para Commands?](#por-qué-ef-core-solo-para-commands)
2. [DbContext - ApplicationDbContext](#dbcontext---applicationdbcontext)
3. [Configuración de Entidades](#configuración-de-entidades)
4. [Mapeo de Value Objects](#mapeo-de-value-objects)
5. [Backed Fields - Colecciones Privadas](#backed-fields---colecciones-privadas)
6. [Value Conversions - IDs](#value-conversions---ids)
7. [Owned Types](#owned-types)
8. [Registro de Dependencias](#registro-de-dependencias)
9. [Comandos de Migraciones](#comandos-de-migraciones)
10. [Referencia de Archivos](#referencia-de-archivos)

---

## ¿Por qué EF Core solo para Commands?

En CQRS, el lado de **Command** necesita:

- **Tracking** de cambios para persistir agregados
- **Transacciones** para operaciones de escritura múltiples
- **Relaciones** entre entidades (Order → OrderLine, Order → Customer)

EF Core es ideal para esto. Para **lecturas** se usa Dapper (sin tracking, más rápido).

```
Escrituras (Commands) → EF Core (tracking, relaciones, transacciones)
Lecturas (Queries)    → Dapper    (sin tracking, SQL directo, más rápido)
```

---

## DbContext - ApplicationDbContext

EF Core centraliza toda la escritura en un único `DbContext`. Este contexto gestiona las entidades del dominio, los Value Objects y el outbox de eventos de dominio. Para que todas las configuraciones se descubran automáticamente, usamos `ApplyConfigurationsFromAssembly`, que escanea el ensamblado y registra cada implementación de `IEntityTypeConfiguration<T>`.

Antes de agregar una nueva entidad, basta con crear su clase de configuración en `Configurations/` — no es necesario modificar `ApplicationDbContext`.

```csharp
// SoftwareLearningGuide.Infraestructure.Data/Context/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

**Puntos clave:**

- `ApplyConfigurationsFromAssembly` carga automáticamente todas las configuraciones `IEntityTypeConfiguration<T>` del ensamblado.
- `Set<T>()` es una forma más limpia de exponer DbSets.
- `OutboxMessageEntity` se gestiona automáticamente mediante `OutboxMessageConfiguration` (se agrega como `DbSet` aquí para que EF Core lo rastree).

---

## Configuración de Entidades

Cada entidad tiene su propia clase de configuración que implementa `IEntityTypeConfiguration<T>`. EF Core descubre estas configuraciones automáticamente gracias a `ApplyConfigurationsFromAssembly` en `ApplicationDbContext`.

### OrderConfiguration - El Agregado

El agregado `Order` es la entidad raíz. Configura la tabla principal (`Orders`), las conversiones de valores para los IDs fuertemente tipados, el mapeo de owned types (`ShippingAddress`), y la colección privada de order lines respaldada por un campo (`_lines`). También persiste los timestamps de estado del workflow del order.

```csharp
// SoftwareLearningGuide.Infraestructure.Data/Configurations/OrderConfiguration.cs
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        // Value Conversion: OrderId → Guid
        builder.Property(o => o.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderId(value))
            .ValueGeneratedNever();

        // Value Conversion: CustomerId → Guid
        builder.Property(o => o.CustomerId)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value));

        // Enum → string
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Owned Type: Address se mapea como columnas planas
        builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200);
            addressBuilder.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
            addressBuilder.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(100);
            addressBuilder.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
            addressBuilder.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
        });

        // Backed Field: colección privada _lines
        builder.Ignore(o => o.Lines);

        builder.HasMany<OrderLine>("_lines")
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Timestamps del workflow del Order
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.ConfirmedAt).IsRequired(false);
        builder.Property(o => o.ShippedAt).IsRequired(false);
        builder.Property(o => o.DeliveredAt).IsRequired(false);
        builder.Property(o => o.CancelledAt).IsRequired(false);
    }
}
```

¿Por qué 'Owned Types' para Address? Porque Address no es una entidad independiente: no tiene su propia tabla, no se referencia por ID, y vive dentro de Order. EF Core lo maneja como columnas prefijadas (ShippingStreet, ShippingCity, etc.) en la tabla de Orders. Esto mantiene el dominio rico en objetos pero la base de datos simple.

¿Por qué los timestamps son opcionales? Porque un Order pasa por un state machine: CreatedAt siempre existe, pero ConfirmedAt, ShippedAt, DeliveredAt y CancelledAt solo se establecen cuando el order avanza a cada estado. Son `IsRequired(false)` para reflejar que son nullable en la base de datos.

### OrderLineConfiguration - Money como Owned Type

`OrderLine` es una entidad separada con su propio ID, persistida en su propia tabla (`OrderLines`). Se configura como Owned Types los valores complejos como `Money` (que se mapea a dos columnas: `UnitPrice` y `Currency`). También persiste el timestamp `CreatedAt`.

```csharp
// SoftwareLearningGuide.Infraestructure.Data/Configurations/OrderLineConfiguration.cs
public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(ol => ol.Id);

        builder.Property(ol => ol.Id)
            .HasConversion(
                id => id.Value,
                value => new OrderLineId(value))
            .ValueGeneratedNever();

        builder.Property(ol => ol.ProductId)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value));

        builder.Property(ol => ol.ProductName).HasMaxLength(200).IsRequired();

        // Owned Type: Money se mapea como columnas planas
        builder.OwnsOne(ol => ol.UnitPrice, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Property(ol => ol.Quantity).IsRequired();
        builder.Property(ol => ol.CreatedAt).IsRequired();
    }
}
```

¿Por qué configurar OrderLine como entidad separada si es parte del agregado Order? Porque OrderLine tiene su propio ID (OrderLineId) y se persiste en su propia tabla (OrderLines). Pero desde el punto de vista del dominio, solo se accede a través de Order. EF Core permite esta dualidad: el agregado controla el acceso, pero la persistencia es independiente.

---

### ProductConfiguration - Owned Type para Money

`Product` es una entidad independiente con su propio aggregate root. Su Value Object `Money` se mapea como Owned Type, con la particularidad de que la propiedad `Currency` se nombra `PriceCurrency` en la base de datos para diferenciarlo del propio valor `Price`.

```csharp
// SoftwareLearningGuide.Infraestructure.Data/Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => new ProductId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();

        builder.OwnsOne(p => p.Price, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)");
            moneyBuilder.Property(m => m.Currency).HasColumnName("PriceCurrency").HasMaxLength(3).IsRequired();
        });

        builder.Property(p => p.StockQuantity).IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired(false);
    }
}
```

¿Por qué `PriceCurrency` en lugar de solo `Currency`? Porque Product ya tiene `Price` como columna de tipo decimal; sin el prefijo `Price`, una columna `Currency` sería ambigua. Prefijar el nombre de la propiedad del Value Object evita colisiones de nombres.

---

### CustomerConfiguration - Dos Owned Types

`Customer` tiene dos Value Objects mapeados como Owned Types: `Email` (un solo valor) y `DefaultShippingAddress` (un Value Object compuesto como Address, pero para dirección de envío por defecto). Se configuran con columnas prefijadas (`DefaultStreet`, `DefaultCity`, etc.) para diferenciarse de las columnas de `ShippingAddress` en Order.

```csharp
// SoftwareLearningGuide.Infraestructure.Data/Configurations/CustomerConfiguration.cs
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();

        builder.OwnsOne(c => c.Email, emailBuilder =>
        {
            emailBuilder.Property(e => e.Value).HasColumnName("Email").HasMaxLength(254);
        });

        builder.OwnsOne(c => c.DefaultShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street).HasColumnName("DefaultStreet").HasMaxLength(200);
            addressBuilder.Property(a => a.City).HasColumnName("DefaultCity").HasMaxLength(100);
            addressBuilder.Property(a => a.State).HasColumnName("DefaultState").HasMaxLength(100);
            addressBuilder.Property(a => a.PostalCode).HasColumnName("DefaultPostalCode").HasMaxLength(20);
            addressBuilder.Property(a => a.Country).HasColumnName("DefaultCountry").HasMaxLength(100);
        });

        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired(false);
    }
}
```

¿Por qué dos Owned Types para Customer? `Email` es un Value Object simple (una sola propiedad `Value`), mientras que `DefaultShippingAddress` es un Value Object compuesto como `Address`. Ambos se mapean como columnas planas en la tabla de Customers. El prefijo `Default` en las columnas de dirección evita ambigüedad con las columnas `Shipping*` de Order.

---

### OutboxMessageConfiguration - Outbox Pattern

El [Outbox Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/outbox-pattern) garantiza que los eventos de dominio se persistan de forma atómica junto con los cambios del modelo de datos. `DomainOutboxMessages` se configura como una tabla normal en el mismo `DbContext` y se gestiona por `OutboxMessageConfiguration`.

```csharp
// SoftwareLearningGuide.Infraestructure.Data/Configurations/OutboxMessageConfiguration.cs
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("DomainOutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedOnUtc)
            .IsRequired(false);

        builder.Property(x => x.Error)
            .IsRequired(false)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.ProcessedOnUtc, x.CreatedOnUtc });
    }
}
```

¿Por qué un índice compuesto en `(ProcessedOnUtc, CreatedOnUtc)`? El OutboxProcessor consulta filas no procesadas (`ProcessedOnUtc IS NULL`) para publicar eventos. El índice compuesto optimiza esa consulta y cubre la ordenación por tiempo de creación.

---

## Mapeo de Value Objects

EF Core maneja Value Objects de dos formas:

### 1. Owned Types (Value Objects complejos)

Para Value Objects con múltiples propiedades (Money, Address, Email):

```csharp
// Money → columnas: UnitPrice (decimal), Currency (string)
builder.OwnsOne(ol => ol.UnitPrice, moneyBuilder =>
{
    moneyBuilder.Property(m => m.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)");
    moneyBuilder.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
});

// Address → columnas: ShippingStreet, ShippingCity, ShippingState, ShippingPostalCode, ShippingCountry
builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
{
    addressBuilder.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200);
    // ...
});
```

Value Conversions son la puente entre el mundo rico de objetos del dominio y las columnas simples de la base de datos. Money se convierte a un decimal + string, un Guid se convierte a un StronglyTypedId, etc. Esto permite que el dominio use objetos expresivos mientras la BD mantiene esquemas simples.

### 2. Value Conversions (Value Objects simples)

Para Value Objects que envuelven un solo valor (IDs):

```csharp
// OrderId → Guid
builder.Property(o => o.Id)
    .HasConversion(
        id => id.Value,              // OrderId → Guid (para DB)
        value => new OrderId(value)) // Guid → OrderId (para C#)
    .ValueGeneratedNever();
```

Value Conversions son la puente entre el mundo rico de objetos del dominio y las columnas simples de la base de datos. Money se convierte a un decimal + string, un Guid se convierte a un StronglyTypedId, etc. Esto permite que el dominio use objetos expresivos mientras la BD mantiene esquemas simples.

---

## Backed Fields - Colecciones Privadas

El agregado `Order` expone una colección privada `_lines`:

```csharp
// Core.Business/Aggregates/Order.cs
private readonly List<OrderLine> _lines = new();
public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
```

EF Core se configura para mapear esta colección a través del campo privado:

```csharp
builder.Ignore(o => o.Lines); // Ignora la propiedad pública

builder.HasMany<OrderLine>("_lines") // Mapea el campo privado
    .WithOne()
    .HasForeignKey("OrderId")
    .OnDelete(DeleteBehavior.Cascade)
    .Metadata.PrincipalToDependent
    .SetPropertyAccessMode(PropertyAccessMode.Field); // Acceso por campo, no propiedad
```

**Resultado en la DB:**

```
Orders                          OrderLines
┌─────────────────────┐        ┌─────────────────────────────┐
│ Id (PK)             │        │ Id (PK)                     │
│ CustomerId          │◄───────│ OrderId (FK)                │
│ Status              │        │ ProductId                   │
│ ShippingStreet      │        │ ProductName                 │
│ ShippingCity        │        │ UnitPrice (decimal)         │
│ ShippingState       │        │ Currency                    │
│ ShippingPostalCode  │        │ Quantity                    │
│ ShippingCountry     │        │ CreatedAt                   │
│ CreatedAt           │        └─────────────────────────────┘
│ ConfirmedAt         │
│ ShippedAt           │
│ DeliveredAt         │
│ CancelledAt         │
└─────────────────────┘
```

---

## Value Conversions - IDs

Todos los IDs usan Value Conversions para mapear Value Objects a `Guid`:

```csharp
builder.Property(o => o.Id)
    .HasConversion(
        id => id.Value,              // OrderId → Guid
        value => new OrderId(value)) // Guid → OrderId
    .ValueGeneratedNever();

builder.Property(o => o.CustomerId)
    .HasConversion(
        id => id.Value,
        value => new CustomerId(value));
```

`ValueGeneratedNever()` indica que EF Core **no genera** el valor automáticamente (nosotros lo generamos en el dominio).

---

## Owned Types

Los Owned Types son Value Objects que EF Core mapea como columnas en la tabla del owner:

| Owner | Owned Type | Columnas en DB |
|-------|------------|----------------|
| `Order` | `ShippingAddress` | `ShippingStreet`, `ShippingCity`, `ShippingState`, `ShippingPostalCode`, `ShippingCountry` |
| `OrderLine` | `UnitPrice` (Money) | `UnitPrice` (decimal), `Currency` (string) |
| `Customer` | `Email` | `Email` (string) |
| `Customer` | `DefaultShippingAddress` | `DefaultStreet`, `DefaultCity`, `DefaultState`, `DefaultPostalCode`, `DefaultCountry` |
| `Product` | `Price` (Money) | `Price` (decimal), `PriceCurrency` (string) |

---

## Registro de Dependencias

El método de extensión `AddInfrastructure` registra el DbContext de EF Core y el `OutboxWriter` personalizado. El `OutboxWriter` garantiza que los eventos de dominio se escriban en la tabla `DomainOutboxMessages` dentro de la misma transacción que `SaveChangesAsync`, cumpliendo con el [Transactional Outbox Pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/outbox-pattern).

```csharp
// SoftwareLearningGuide.Infraestructure/Dependencies.cs
public static class Dependencies
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Registrar Outbox Writer personalizado
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        return services;
    }
}
```

Se registra en `Program.cs` con:

```csharp
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection")!);
```

¿Por qué AddInfrastructure es un método de extensión? Porque encapsula toda la configuración de infraestructura (DbContext, conexiones, outbox) en un solo lugar. Los callers no necesitan saber qué ORM se usa ni cómo se configura. Si mañana cambias de EF Core a Dapper para commands, solo cambias AddInfrastructure.

---

## Comandos de Migraciones

EF Core usa migraciones para gestionar los cambios en el esquema de la base de datos.

### Requisitos

```bash
# Instalar la herramienta CLI de EF Core (si no esta instalada)
dotnet tool install --global dotnet-ef

# Verificar la version
dotnet ef --version
```

### Crear una Migracion

```bash
# Navegar a la carpeta del proyecto que contiene el DbContext
cd SoftwareLearningGuide.Infraestructure.Data

# Crear una migracion
dotnet ef migrations add <NombreMigracion> --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api

# Ejemplo: crear migracion inicial
dotnet ef migrations add InitialCreate --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api
```

### Aplicar Migraciones

```bash
# Aplicar migraciones a la base de datos
dotnet ef database update --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api

# Aplicar hasta una migracion especifica
dotnet ef database update <NombreMigracion> --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api
```

### Listar Migraciones

```bash
# Ver todas las migraciones disponibles
dotnet ef migrations list --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api
```

### Eliminar la Ultima Migracion

```bash
# Eliminar la ultima migracion (solo si no se ha aplicado)
dotnet ef migrations remove --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api
```

### Generar Script SQL

```bash
# Generar script SQL de todas las migraciones
dotnet ef migrations script --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api --output ..\docker\init.sql

# Generar script idempotente (solo aplica cambios pendientes)
dotnet ef migrations script --idempotent --project ..\SoftwareLearningGuide.Infraestructure.Data --startup-project ..\SoftwareLearningGuide.Api --output ..\docker\init-idempotent.sql
```

### Resumen de Comandos

| Comando | Descripcion |
|---------|-------------|
| `dotnet ef migrations add <Nombre>` | Crea una nueva migracion basada en cambios del modelo |
| `dotnet ef database update` | Aplica todas las migraciones pendientes a la DB |
| `dotnet ef database update <Nombre>` | Aplica hasta una migracion especifica |
| `dotnet ef migrations list` | Lista todas las migraciones |
| `dotnet ef migrations remove` | Elimina la ultima migracion (si no se aplico) |
| `dotnet ef migrations script` | Genera script SQL desde las migraciones |
| `dotnet ef migrations script --idempotent` | Genera script idempotente (seguro para ejecutar multiples veces) |

### Migraciones en Docker (Opcion Alternativa)

Si se usa `EnsureCreated()` (como en este proyecto), EF Core crea la base de datos y el esquema automaticamente al iniciar la aplicacion. Esto es practico para desarrollo pero **no recomendado para produccion**:

```csharp
// Program.cs - Create con Feature Toggle
if (featureManager.IsEnabledAsync(FeatureToggleNames.FT_APPLY_MIGRATIONS_ON_START).GetAwaiter().GetResult())
{
    dbContext.Database.EnsureCreated(); // Crea DB + esquema si no existen
}
```

**En produccion, se recomienda usar migraciones** para tener control sobre los cambios de esquema:

```bash
# Generar script SQL para produccion
dotnet ef migrations script --idempotent --output docker/init.sql

# El script se ejecuta en el container de SQL Server al iniciar
```

---

## Referencia de Archivos

| Archivo | Descripción |
|---------|-------------|
| `SoftwareLearningGuide.Infraestructure.Data/Context/ApplicationDbContext.cs` | DbContext principal |
| `SoftwareLearningGuide.Infraestructure.Data/Configurations/OrderConfiguration.cs` | Configuración del agregado Order |
| `SoftwareLearningGuide.Infraestructure.Data/Configurations/OrderLineConfiguration.cs` | Configuración de OrderLine con Money |
| `SoftwareLearningGuide.Infraestructure.Data/Configurations/ProductConfiguration.cs` | Configuración de Product con OwnsOne Price |
| `SoftwareLearningGuide.Infraestructure.Data/Configurations/CustomerConfiguration.cs` | Configuración de Customer con Email y DefaultShippingAddress |
| `SoftwareLearningGuide.Infraestructure.Data/Configurations/OutboxMessageConfiguration.cs` | Configuración de la tabla DomainOutboxMessages |
| `SoftwareLearningGuide.Infraestructure/Repositories/OrderWriteRepository.cs` | Implementación del repository de Order |
| `SoftwareLearningGuide.Infraestructure/Repositories/BaseRepository.cs` | Repositorio base genérico |
| `SoftwareLearningGuide.Application/Ports/IOrderWriteRepository.cs` | Puerto de escritura de Order |
| `SoftwareLearningGuide.Application/Ports/IBaseRepository.cs` | Puerto base de repositorio |
| `SoftwareLearningGuide.Infraestructure/Dependencies.cs` | Registro de DI |
| `SoftwareLearningGuide.Infraestructure/Repositories/UnitOfWork.cs` | Implementación de IUnitOfWork |

---

## Paquetes NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| `Microsoft.EntityFrameworkCore` | 10.0.0 | ORM principal |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.0 | Provider de SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.0 | Migraciones y herramientas |