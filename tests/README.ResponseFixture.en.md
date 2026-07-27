# Response Fixture

![Testing](https://img.shields.io/badge/Testing-Fixture-8B5CF6)
![JSON](https://img.shields.io/badge/JSON-Asserts-45B7D1)

**Response Fixtures** are JSON files containing expected API responses, used in E2E tests to verify that endpoints return exactly the agreed contract.

---

## What is a Response Fixture?

> A response fixture is a JSON file representing the expected response from an endpoint. The test loads this file and compares the actual response against it, ensuring the API contract is maintained.

---

## Why Response Fixtures?

| Option | Problem |
|--------|----------|
| Inline asserts with literals | Verbose code, hard to maintain, mixes data with logic |
| Anonymous objects | Not reusable, fragmented across tests |
| Static constant classes | Scalable but rigid, inflexible |
| **Response Fixture** | **External files, readable, versionable, reusable** |

---

## Usage in the project

### JSON Fixture

```json
// E2E/Product/ResponseFixtures/Create_success.json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "name": "Gaming Laptop",
  "description": "Laptop with RTX 4070",
  "price": 1299.99,
  "currency": "USD",
  "stockQuantity": 10,
  "createdAt": "2026-01-15T10:30:00Z"
}
```

### Loading the fixture in the test

```csharp
// In E2ETestBase.cs
protected async Task<JsonNode> LoadFixture(string relativePath)
{
    var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    var fixturePath = Path.Combine(baseDir, relativePath);
    var json = await File.ReadAllTextAsync(fixturePath);
    return JsonNode.Parse(json)
        ?? throw new InvalidOperationException($"Empty fixture file: {fixturePath}");
}
```

### Usage in an E2E test

```csharp
[Test]
public async Task CreateProduct_WithValidData_ShouldReturnExpectedResponse()
{
    // Arrange
    var request = new { name = "Gaming Laptop", description = "Laptop with RTX 4070",
                        price = 1299.99, currency = "USD", stock = 10 };

    // Act
    var response = await Client.PostAsJsonAsync("/api/v2.0/product", request);

    // Assert
    var actual = await GetResponseJson(response);
    var expected = await LoadFixture("E2E/Product/ResponseFixtures/Create_success.json");

    Assert.That(JsonNode.DeepEquals(actual, expected), Is.True);
}
```

---

## Fixture Structure

```
tests/SoftwareLearningGuide.Api.Test/
└── E2E/
    ├── Product/
    │   └── ResponseFixtures/
    │       ├── Create_success.json
    │       ├── GetAll_with_products.json
    │       ├── GetAll_empty.json
    │       └── GetById_existing.json
    ├── Customer/
    │   └── ResponseFixtures/
    │       ├── Create_success.json
    │       ├── GetAll_with_customers.json
    │       ├── GetAll_empty.json
    │       ├── GetById_existing.json
    │       └── GetById_with_address.json
    └── Order/
        └── ResponseFixtures/
            ├── GetAll_with_orders.json
            ├── GetAll_empty.json
            └── GetById_with_lines.json
```

---

## Best practices

| Practice | Description |
|----------|-------------|
| **One fixture per scenario** | `GetAll_empty.json`, `GetAll_with_orders.json` |
| **Descriptive names** | `GetById_existing.json`, `Create_success.json` |
| **No dynamic data** | Use fixed GUIDs, static dates, predictable values |
| **Versioned in Git** | Fixtures reflect the API contract at each commit |
| **Copied to output** | Configure `.csproj` to copy JSON to `bin/Debug` |

---

### .csproj configuration

```xml
<ItemGroup>
  <Content Include="E2E/**/ResponseFixtures/*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

---

## Key benefits

- **Living contract** — the fixture documents the exact response structure
- **Early detection** — unintentional changes in the response break the test
- **Collaboration** — frontend can know the expected response without running the API
- **Maintainable** — changing a field in a fixture is safer than changing 10 inline asserts

---

## References

- [JSON Fixtures — xUnit Patterns](https://xunitpatterns.com/Shared%20Fixture.html)
- [System.Text.Json — JsonNode.DeepEquals](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.nodes.jsonnode.deepequals)

**See also:**
- [`README.Respawn.md`](README.Respawn.md) — Respawn for cleaning data between tests
- [`README.TestContainer.md`](README.TestContainer.md) — TestContainers for integration environments
- [`README.TDD.md`](README.TDD.md) — TDD cycle and testing best practices
