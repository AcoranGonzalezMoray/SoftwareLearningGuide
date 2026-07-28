# TDD (Test-Driven Development)

![Testing](https://img.shields.io/badge/Testing-TDD-4ECDC4)
![Metodología](https://img.shields.io/badge/Metodología-Red--Green--Refactor-FF6B6B)

**Test-Driven Development** es una metodología de desarrollo donde los tests se escriben **antes** del código de producción, guiando el diseño mediante un ciclo de tres fases: Red, Green, Refactor.

---

## ¿Qué es TDD?

> TDD es una disciplina de programación que invierte el orden tradicional: primero escribes un test que falla (Red), luego escribes el código mínimo para que pase (Green), y finalmente refactorizas el código mejorando su diseño (Refactor).

No es una estrategia de testing — es una **técnica de diseño** que produce código más limpio, testeable y con alta cobertura.

---

## Ciclo Red-Green-Refactor

```
┌─────────────────────────────────────────────────────┐
│                     RED                             │
│  1. Escribe un test que falle                       │
│  2. Define el comportamiento deseado                │
│  3. El test ni siquiera compila (aún no existe el   │
│     código de producción)                           │
└─────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────┐
│                    GREEN                            │
│  1. Escribe el código mínimo para que el test pase  │
│  2. Sin importar que "no sea bonito"                │
│  3. El objetivo es que el test compile y pase       │
└─────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────┐
│                  REFACTOR                           │
│  1. Mejora el código sin cambiar comportamiento     │
│  2. Elimina duplicación, mejora nombres, refine     │
│  3. Los tests deben seguir pasando                  │
└─────────────────────────────────────────────────────┘
         │
         └──→ Vuelve a RED para el siguiente requisito
```

---

## Ejemplo en el proyecto

### Fase RED — Escribir el test primero

```csharp
[Test]
public async Task AddProduct_WithNegativePrice_ShouldReturnError()
{
    var result = Product.Create(
        ProductId.Create(),
        "Test",
        "Desc",
        Money.Create(-10m, "USD").Value!, // Precio negativo
        5
    );

    Assert.That(result.IsFailure, Is.True);
    Assert.That(result.Error.Code, Is.EqualTo("Product.InvalidPrice"));
}
```

**Nota:** `Product.Create` ni siquiera existe aún.

### Fase GREEN — Implementación mínima

```csharp
public static Result<Product> Create(ProductId id, string name,
    string description, Money price, int stockQuantity)
{
    // Solo lo necesario para que el test pase
    if (price.Amount <= 0)
        return Result<Product>.Failure(Errors.Product.InvalidPrice);

    return Result<Product>.Success(new Product(id, name, description, price, stockQuantity));
}
```

### Fase REFACTOR — Mejorar el diseño

```csharp
public static Result<Product> Create(ProductId id, string name,
    string description, Money price, int stockQuantity)
{
    var errors = new List<Error>();

    if (string.IsNullOrWhiteSpace(name))
        errors.Add(Errors.Product.InvalidName);
    if (price.Amount <= 0)
        errors.Add(Errors.Product.InvalidPrice);
    if (stockQuantity < 0)
        errors.Add(Errors.Product.InvalidStock);

    return errors.Count > 0
        ? Result<Product>.Failure(errors.ToArray())
        : Result<Product>.Success(new Product(id, name, description, price, stockQuantity));
}
```

---

## Tipos de tests en el proyecto

| Tipo | Proyecto | Framework | Propósito |
|------|----------|-----------|-----------|
| **Unitarios** | `Core.Business.Test` | NUnit | Value Objects, Entidades, Agregados |
| **Unitarios** | `Application.Command.Test` | NUnit | Command Handlers (con mocks) |
| **Unitarios** | `Application.Query.Test` | NUnit | Query Handlers (con Dapper) |
| **Integración** | `Infraestructure.Test` | NUnit | Repositorios, DbContext, transacciones |
| **E2E / API** | `Api.Test` | NUnit | HTTP requests reales contra la API |

---

## Buenas prácticas de TDD aplicadas

| Práctica | Cómo se aplica |
|----------|----------------|
| **Nombres descriptivos** | `AddProduct_WithNegativePrice_ShouldReturnError` |
| **Arrange-Act-Assert** | Estructura clara en cada test (Given-When-Then) |
| **Un solo concepto por test** | Cada test verifica un único escenario |
| **Tests independientes** | Respawn + TestContainers aislan el estado |
| **Fixtures compartidos** | JSON de respuesta para asserts predecibles |
| **Builders para datos** | `OrderBuilder`, `ProductBuilder` reducen boilerplate |
| **Código mínimo en Green** | Solo lo necesario para que el test pase |

---

## TDD vs Escribir tests después

| Aspecto | TDD (Test First) | Tests después |
|---------|------------------|---------------|
| **Diseño** | Guiado por tests, emerge naturalmente | Puede producir código no testeable |
| **Cobertura** | Alta por construcción | Depende de la disciplina |
| **Confianza** | Inmediata (test antes = requisito claro) | El test solo valida lo ya escrito |
| **Velocidad inicial** | Más lento al principio | Más rápido al principio |
| **Deuda técnica** | Menor (refactor continuo) | Mayor (se postponen refactors) |
| **Documentación** | Los tests documentan el comportamiento | Los tests documentan el código existente |

---

## Referencias

- [Test-Driven Development by Example — Kent Beck](https://www.pearson.com/en-us/subject-catalog/p/test-driven-development-by-example/P200000009465)
- [Martin Fowler — TDD](https://martinfowler.com/bliki/TestDrivenDevelopment.html)
- [The Three Laws of TDD (Uncle Bob)](https://www.oreilly.com/library/view/clean-code-a/9780136083238/)

**Ver también:**
- [`README.Respawn.md`](README.Respawn.md) — Respawn para limpiar datos entre tests
- [`README.TestContainer.md`](README.TestContainer.md) — TestContainers para entornos de integración
- [`README.ResponseFixture.md`](README.ResponseFixture.md) — Fixtures JSON para asserts en tests
