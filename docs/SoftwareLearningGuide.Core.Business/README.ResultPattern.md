# Patrón Result<T> - Manejo de Errores Funcional

Este documento explica el patrón **Result<T>** implementado en `SoftwareLearningGuide.Core.Business` y cómo se utiliza para manejar errores de forma explícita y predecible.

![Result Pattern](https://img.shields.io/badge/Pattern-Result%3CT%3E-green)

### 📚 Tabla de Contenidos

1. [¿Qué es Result<T>?](#qué-es-result)
2. [Ventajas sobre Excepciones](#ventajas-sobre-excepciones)
3. [Implementación](#implementación)
4. [Uso Básico](#uso-básico)
5. [Composición de Resultados](#composición-de-resultados)
6. [Ejemplos del Proyecto](#ejemplos-del-proyecto)
7. [Mejores Prácticas](#mejores-prácticas)

---

## ¿Qué es Result<T>?

**Result<T>** es un patrón que encapsula el resultado de una operación, que puede ser:
- ✅ **Exitosa** - Contiene un valor de tipo `T`
- ❌ **Fallida** - Contiene un mensaje de error

En lugar de lanzar excepciones, el método devuelve un `Result<T>` que indica de forma explícita si la operación fue exitosa o no.

### Definición

```csharp
public class Result<T>
{
	public bool IsSuccess { get; }
	public T? Value { get; }
	public string? Error { get; }

	private Result(T? value, bool isSuccess, string? error)
	{
		Value = value;
		IsSuccess = isSuccess;
		Error = error;
	}

	// Crear un resultado exitoso
	public static Result<T> Success(T value) => new(value, true, null);

	// Crear un resultado fallido
	public static Result<T> Failure(string error) => new(default, false, error);

	// Métodos útiles para composición
	public Result<TNew> Map<TNew>(Func<T, Result<TNew>> mapper) { ... }
	public Result<T> Tap(Action<T> action) { ... }
	public T GetValueOrThrow() { ... }
}

// Para operaciones sin valor de retorno
public class Result
{
	public bool IsSuccess { get; }
	public string? Error { get; }

	public static Result Success() => new(true, null);
	public static Result Failure(string error) => new(false, error);
}
```

---

## Ventajas sobre Excepciones

### ❌ Problemas con Excepciones

```csharp
public Money CalculateDiscount(Product product, decimal discountPercent)
{
	// ¿Qué excepciones puede lanzar?
	// ¿Debería capturarse aquí o delegar?
	// ¿El consumidor sabrá qué excepciones esperar?

	if (discountPercent < 0 || discountPercent > 100)
		throw new DomainException("Descuento inválido"); // ¿Qué tipo de excepción?

	var discountAmount = product.Price.Multiply(discountPercent / 100);
	return product.Price.Subtract(discountAmount);
}

// Uso problemático
try
{
	var discountedPrice = CalculateDiscount(product, 150);
	// El compilador no me obliga a manejar el error
}
catch (DomainException ex)
{
	// ¿Qué hago aquí?
	Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
	// Captura genérica de excepciones inesperadas
}
```

### ✅ Solución con Result<T>

```csharp
public Result<Money> CalculateDiscount(Product product, decimal discountPercent)
{
	// El compilador me obliga a validar al llamador

	if (discountPercent < 0 || discountPercent > 100)
		return Result<Money>.Failure("Descuento debe estar entre 0 y 100%");

	var discountAmount = product.Price.Multiply(discountPercent / 100);
	if (!discountAmount.IsSuccess)
		return discountAmount;

	return product.Price.Subtract(discountAmount.Value!);
}

// Uso explícito
var result = CalculateDiscount(product, 20);
if (!result.IsSuccess)
{
	Console.WriteLine($"Error: {result.Error}");
	return;
}

var discountedPrice = result.Value; // Garantizado que existe
```

### Comparativa

| Aspecto | Excepciones | Result<T> |
|---------|-------------|-----------|
| **Visibilidad** | Implícita (comentarios) | Explícita (tipo) |
| **Rendimiento** | Lenta (stack unwinding) | Rápida |
| **Composición** | Difícil (try-catch) | Fácil (Map, Tap) |
| **Tipo-seguridad** | Sin verificación | El compilador verifica |
| **Legibilidad** | Acción vs Flujo control | Flujo claro |

> **Filosofía del patrón:** Result\<T> no es solo un patrón técnico; es una filosofía de diseño. En lugar de preguntar "¿qué puede fallar?" y capturar excepciones, preguntas "¿qué puede salir bien?" y verificas explícitamente. Esto hace que el código sea más predecible y fácil de testear.

---

## Implementación

### Implementación Completa en el Proyecto

> **Archivo productivo:** [`SoftwareLearningGuide.Core.Business/Exceptions/Result.cs`](SoftwareLearningGuide.Core.Business/Exceptions/Result.cs)

```csharp
namespace SoftwareLearningGuide.Core.Business.Exceptions;

/// <summary>
/// Result genérico que encapsula el resultado de una operación.
/// Puede ser exitosa (con valor) o fallida (con mensaje de error).
/// </summary>
public class Result<T>
{
	public bool IsSuccess { get; }
	public T? Value { get; }
	public string? Error { get; }

	private Result(T? value, bool isSuccess, string? error)
	{
		Value = value;
		IsSuccess = isSuccess;
		Error = error;
	}

	/// <summary>
	/// Crea un resultado exitoso.
	/// </summary>
	public static Result<T> Success(T value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		return new Result<T>(value, true, null);
	}

	/// <summary>
	/// Crea un resultado fallido.
	/// </summary>
	public static Result<T> Failure(string error)
	{
		if (string.IsNullOrWhiteSpace(error))
			throw new ArgumentException("El mensaje de error no puede estar vacío.", nameof(error));

		return new Result<T>(default, false, error);
	}

	/// <summary>
	/// Aplica una función al valor si es exitoso, manteniendo el error si es fallido.
	/// Permite composición de operaciones.
	/// </summary>
	public Result<TNew> Map<TNew>(Func<T, Result<TNew>> mapper)
	{
		if (!IsSuccess)
			return Result<TNew>.Failure(Error!);

		return mapper(Value!);
	}

	/// <summary>
	/// Aplica una acción si es exitoso sin cambiar el tipo (efecto secundario).
	/// </summary>
	public Result<T> Tap(Action<T> action)
	{
		if (IsSuccess)
			action(Value!);

		return this;
	}

	/// <summary>
	/// Obtiene el valor o lanza una excepción si falló.
	/// Útil en contextos donde el error es fatal.
	/// </summary>
	public T GetValueOrThrow()
	{
		if (!IsSuccess)
			throw new InvalidOperationException($"Result falló: {Error}");

		return Value!;
	}

	/// <summary>
	/// Obtiene el valor o devuelve un valor por defecto si falló.
	/// </summary>
	public T GetValueOrDefault(T defaultValue) => IsSuccess ? Value! : defaultValue;

	public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
}

/// <summary>
/// Result no genérico para operaciones que no devuelven valor (void).
/// </summary>
public class Result
{
	public bool IsSuccess { get; }
	public string? Error { get; }

	private Result(bool isSuccess, string? error)
	{
		IsSuccess = isSuccess;
		Error = error;
	}

	/// <summary>
	/// Crea un resultado exitoso.
	/// </summary>
	public static Result Success() => new(true, null);

	/// <summary>
	/// Crea un resultado fallido.
	/// </summary>
	public static Result Failure(string error)
	{
		if (string.IsNullOrWhiteSpace(error))
			throw new ArgumentException("El mensaje de error no puede estar vacío.", nameof(error));

		return new(false, error);
	}

	/// <summary>
	/// Aplica una acción si es exitoso.
	/// </summary>
	public Result Tap(Action action)
	{
		if (IsSuccess)
			action();

		return this;
	}

	/// <summary>
	/// Convierte Result a Result<T> con un valor específico.
	/// </summary>
	public Result<T> ToResult<T>(T value)
	{
		if (!IsSuccess)
			return Result<T>.Failure(Error!);

		return Result<T>.Success(value);
	}

	/// <summary>
	/// Obtiene el resultado o lanza una excepción si falló.
	/// </summary>
	public void GetValueOrThrow()
	{
		if (!IsSuccess)
			throw new InvalidOperationException($"Result falló: {Error}");
	}

	public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";
}
```

> **¿Por qué hay dos clases Result y Result\<T>?** `Result` es para operaciones que no devuelven valor (como actualizar un nombre), y `Result<T>` es para operaciones que sí devuelven algo (como calcular un precio). Ambas siguen la misma filosofía: Success o Failure. La distinción existe porque en C# un método que no retorna nada (`void`) no puede devolver un `Result<Money>`, así que necesitamos una versión sin tipo genérico para esos casos.

---

## Uso Básico

### 1. Crear Resultados Exitosos

```csharp
// Result<T> - Operación que devuelve un valor
var money = new Money(100, "USD");
return Result<Money>.Success(money);

// Result - Operación sin valor de retorno
public Result UpdateName(string newName)
{
	if (string.IsNullOrWhiteSpace(newName))
		return Result.Failure("El nombre no puede estar vacío.");

	Name = newName;
	return Result.Success();
}
```

### 2. Crear Resultados Fallidos

```csharp
// Result<T> - Con valor genérico
if (amount < 0)
	return Result<Money>.Failure("El monto no puede ser negativo");

// Result - Sin tipo específico
if (stockQuantity < 0)
	return Result.Failure("El stock no puede ser negativo");
```

### 3. Verificar Éxito/Fallo

```csharp
var result = order.AddProduct(product, 5);

if (result.IsSuccess)
{
	Console.WriteLine("Producto agregado exitosamente");
}
else
{
	Console.WriteLine($"Error: {result.Error}");
}

// O más concisamente
if (!result.IsSuccess)
{
	HandleError(result.Error);
	return;
}

// Proceder con éxito
ProcessOrder();
```

### 4. Obtener Valores

```csharp
var result = order.GetTotalAmount();

// Opción 1: Verificar primero
if (result.IsSuccess)
{
	var total = result.Value; // Garantizado que no es null
}

// Opción 2: Valor por defecto
var total = result.GetValueOrDefault(Money.Zero());

// Opción 3: Lanzar si falla (para casos críticos)
try
{
	var total = result.GetValueOrThrow();
}
catch (InvalidOperationException ex)
{
	// Manejar caso crítico
}
```

---

## Composición de Resultados

### Map - Transformar el Valor

```csharp
// Result<Money>.Map() - Transformar dinero a otra moneda
var priceInUsd = new Money(100, "USD");
var moneyResult = Result<Money>.Success(priceInUsd);

// Aplicar descuento
var discountedResult = moneyResult.Map(money => 
	money.Multiply(0.9) // 10% descuento
);

if (discountedResult.IsSuccess)
{
	Console.WriteLine($"Precio con descuento: {discountedResult.Value}");
}
else
{
	Console.WriteLine($"Error: {discountedResult.Error}");
}

// Encadenar múltiples transformaciones
var finalPrice = Result<Money>.Success(new Money(100, "USD"))
	.Map(money => money.Multiply(0.9))  // 10% descuento
	.Map(discounted => discounted.Add(new Money(5, "USD"))) // +$5 impuesto
	.Map(withTax => withTax.Divide(2)); // Dividir entre 2 personas
```

> **Map es como una cadena de producción:** cada paso transforma el resultado del anterior. Si en algún paso algo falla, el error se propaga automáticamente sin necesidad de escribir `if/else` en cada línea. Nota cómo el tipo va cambiando: `Money` → `Money` → `Money` → `Money`. El compilador verifica que cada transformación sea compatible.

### Tap - Efecto Secundario

```csharp
// Tap permite ejecutar código sin cambiar el resultado
var result = product.UpdatePrice(new Money(99.99, "USD"))
	.Tap(() => logger.LogInformation("Precio actualizado"))
	.Tap(() => NotifyPriceChange());

if (!result.IsSuccess)
{
	Console.WriteLine($"Error: {result.Error}");
}
```

> **Tap es como un sensor en la cadena de producción:** no modifica el resultado, solo "observa" y ejecuta un efecto secundario (logging, notificaciones, métricas). Es útil para agregar comportamientos transversales sin ensuciar la lógica principal. Si el `Result` falla, el `Tap` simplemente no se ejecuta.

### Composición Manual

```csharp
public Result<Money> CalculateFinalPrice(Product product, decimal discountPercent)
{
	// Calcular descuento
	var discountResult = product.Price.Multiply(discountPercent / 100);
	if (!discountResult.IsSuccess)
		return Result<Money>.Failure($"Error calculando descuento: {discountResult.Error}");

	// Restar descuento
	var finalPriceResult = product.Price.Subtract(discountResult.Value!);
	if (!finalPriceResult.IsSuccess)
		return Result<Money>.Failure($"Error calculando precio final: {finalPriceResult.Error}");

	return finalPriceResult;
}

// Uso
var result = CalculateFinalPrice(product, 20);
if (result.IsSuccess)
{
	Console.WriteLine($"Precio final: {result.Value}");
}
```

---

## Ejemplos del Proyecto

### 1. Value Object - Money.cs

> **¿Por qué `Money` devuelve `Result<T>` en vez de lanzar excepciones?** Porque `Money` es un `record` inmutable y cada operación aritmética puede fallar por razones verificables (moneda incompatible, divisor cero, resultado negativo). Al devolver `Result<T>`, el consumidor siempre sabe que debe verificar el resultado antes de usar el valor. El constructor privado de `Money` lanza excepciones para invariants irrompibles (como monto negativo), pero las operaciones entre dos objetos `Money` devuelven `Result<T>` porque son errores de contexto de negocio, no de invariante del objeto.

> **Referencia de producción:** [`ValueObjects/Money.cs`](../../ValueObjects/Money.cs)

```csharp
public Result<Money> Add(Money other) {
    if (other == null)
        return Result<Money>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

    if (Currency != other.Currency)
        return Result<Money>.Failure(
            DomainErrors.MoneyErrors.CannotAddDifferentCurrencies(Currency, other.Currency));

    try {
        return Result<Money>.Success(new Money(Amount + other.Amount, Currency));
    }
    catch (ArgumentException ex) {
        return Result<Money>.Failure(ex.Message);
    }
}

public Result<Money> Subtract(Money other) {
    if (other == null)
        return Result<Money>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

    if (Currency != other.Currency)
        return Result<Money>.Failure(
            DomainErrors.MoneyErrors.CannotSubtractDifferentCurrencies(Currency, other.Currency));

    var result = Amount - other.Amount;
    if (result < MinAmount)
        return Result<Money>.Failure(DomainErrors.MoneyErrors.SubtractionResultCannotBeNegative(result));

    try {
        return Result<Money>.Success(new Money(result, Currency));
    }
    catch (ArgumentException ex) {
        return Result<Money>.Failure(ex.Message);
    }
}

public Result<Money> Multiply(decimal factor) {
    if (factor < 0)
        return Result<Money>.Failure(DomainErrors.MoneyErrors.MultiplyFactorCannotBeNegative(factor));

    try {
        return Result<Money>.Success(new Money(Amount * factor, Currency));
    }
    catch (ArgumentException ex) {
        return Result<Money>.Failure(ex.Message);
    }
}

public Result<Money> Divide(decimal divisor) {
    if (divisor == 0)
        return Result<Money>.Failure(DomainErrors.MoneyErrors.CannotDivideByZero());

    if (divisor < 0)
        return Result<Money>.Failure(DomainErrors.MoneyErrors.DivisorCannotBeNegative(divisor));

    try {
        return Result<Money>.Success(new Money(Amount / divisor, Currency));
    }
    catch (ArgumentException ex) {
        return Result<Money>.Failure(ex.Message);
    }
}

public Result<bool> IsGreaterThan(Money other) {
    if (other == null)
        return Result<bool>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

    if (Currency != other.Currency)
        return Result<bool>.Failure(
            DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies(Currency, other.Currency));

    return Result<bool>.Success(Amount > other.Amount);
}

public Result<bool> IsLessThan(Money other) {
    if (other == null)
        return Result<bool>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

    if (Currency != other.Currency)
        return Result<bool>.Failure(
            DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies(Currency, other.Currency));

    return Result<bool>.Success(Amount < other.Amount);
}

public Result<bool> IsGreaterThanOrEqual(Money other) {
    if (other == null)
        return Result<bool>.Failure(DomainErrors.MoneyErrors.OtherMoneyCannotBeNull());

    if (Currency != other.Currency)
        return Result<bool>.Failure(
            DomainErrors.MoneyErrors.CannotCompareDifferentCurrencies(Currency, other.Currency));

    return Result<bool>.Success(Amount >= other.Amount);
}```

> **¿Notas el patrón?** Cada método que puede fallar devuelve `Result` en lugar de lanzar excepciones. Esto obliga al llamador a verificar el resultado, eliminando sorpresas en runtime. En `Money`, incluso las comparaciones simples como `IsGreaterThan` devuelven `Result<bool>` porque podría haber un error de moneda incompatible. Los mensajes de error provienen del catálogo centralizado en [`DomainErrors.cs`](../Errors/DomainErrors.cs).

### 2. Entidad - Product.cs

```csharp
public Result UpdateName(string newName)
{
	if (string.IsNullOrWhiteSpace(newName))
		return Result.Failure("El nombre del producto no puede estar vacío.");

	Name = newName.Trim();
	UpdatedAt = DateTime.UtcNow;
	return Result.Success();
}

public Result<Money> CalculateSubtotal(int quantity)
{
	if (quantity <= 0)
		return Result<Money>.Failure("La cantidad debe ser mayor a cero.");

	return Price.Multiply(quantity);
}

public Result RemoveStock(int quantity)
{
	if (quantity <= 0)
		return Result.Failure("La cantidad a remover debe ser mayor a cero.");

	if (StockQuantity < quantity)
		return Result.Failure(
			$"Stock insuficiente. Stock actual: {StockQuantity}, solicitado: {quantity}");

	StockQuantity -= quantity;
	UpdatedAt = DateTime.UtcNow;
	return Result.Success();
}
```

> **¿Notas el patrón?** En `Product`, cada método que modifica el estado (`UpdateName`, `RemoveStock`) devuelve `Result` en lugar de `void`. Esto significa que el compilador **obliga** a quien llame a estos métodos a preguntarse: "¿y si falla?". No puedes usar `RemoveStock` sin antes verificar `IsSuccess`. Es como si cada方法 tuviera un "semáforo" que debes revisar antes de continuar.

### 3. Agregado - Order.cs

```csharp
public Result AddProduct(Product product, int quantity)
{
	if (product == null)
		return Result.Failure("El producto no puede ser nulo.");

	if (quantity <= 0)
		return Result.Failure("La cantidad debe ser mayor a cero.");

	if (quantity > MaxProductsPerLine)
		return Result.Failure(
			$"No se pueden agregar más de {MaxProductsPerLine} unidades del mismo producto.");

	if (Status != OrderStatus.Pending)
		return Result.Failure(
			$"No se pueden agregar productos a un pedido en estado {Status}.");

	if (!product.HasSufficientStock(quantity))
		return Result.Failure(
			$"Stock insuficiente para el producto '{product.Name}'.");

	var existingLine = _lines.FirstOrDefault(l => l.ProductId.Equals(product.Id));
	if (existingLine != null)
	{
		var newQuantity = existingLine.Quantity + quantity;
		if (newQuantity > MaxProductsPerLine)
			return Result.Failure(
				$"No se pueden superar {MaxProductsPerLine} unidades del mismo producto.");

		return existingLine.UpdateQuantity(newQuantity);
	}
	else
	{
		var line = new OrderLine(
			OrderLineId.Create(),
			product.Id,
			product.Name,
			product.Price,
			quantity);

		_lines.Add(line);
		return Result.Success();
	}
}

public Result<Money> GetTotalAmount()
{
	if (_lines.Count == 0)
		return Result<Money>.Success(Money.Zero());

	var firstLineResult = _lines[0].GetSubtotal();
	if (!firstLineResult.IsSuccess)
		return firstLineResult;

	Money totalAmount = firstLineResult.Value!;
	for (int i = 1; i < _lines.Count; i++)
	{
		var subtotalResult = _lines[i].GetSubtotal();
		if (!subtotalResult.IsSuccess)
			return Result<Money>.Failure($"Error al calcular subtotal en línea {i}");

		var addResult = totalAmount.Add(subtotalResult.Value!);
		if (!addResult.IsSuccess)
			return addResult;

		totalAmount = addResult.Value!;
	}

	return Result<Money>.Success(totalAmount);
}

public Result Confirm()
{
	if (_lines.Count == 0)
		return Result.Failure("No se puede confirmar un pedido vacío.");

	if (Status != OrderStatus.Pending)
		return Result.Failure(
			$"No se puede confirmar un pedido en estado {Status}.");

	Status = OrderStatus.Confirmed;
	ConfirmedAt = DateTime.UtcNow;
	return Result.Success();
}
```

> **¿Notas el patrón?** En `Order`, la validación es una cascada de `if/return` que verifica cada regla de negocio antes de ejecutar la acción. Cada `return Result.Failure(...)` es un "cortocircuito" que detiene la ejecución inmediatamente. No necesitas excepciones porque el flujo de control ya está diseñado para manejar errores explícitamente.

### 4. Uso en Capa de Aplicación

```csharp
public class OrderService
{
	public Result<OrderDto> CreateOrder(CreateOrderRequest request)
	{
		// Validar cliente
		if (!TryGetCustomer(request.CustomerId, out var customer))
			return Result<OrderDto>.Failure("Cliente no encontrado");

		// Crear dirección
		var addressResult = Address.Create(
			request.Street, request.City, request.State,
			request.PostalCode, request.Country);

		if (!addressResult.IsSuccess)
			return Result<OrderDto>.Failure($"Dirección inválida: {addressResult.Error}");

		// Crear pedido
		var order = new Order(OrderId.Create(), customer.Id, addressResult.Value!);

		// Agregar productos
		foreach (var lineRequest in request.Lines)
		{
			var product = GetProduct(lineRequest.ProductId);
			var addResult = order.AddProduct(product, lineRequest.Quantity);

			if (!addResult.IsSuccess)
				return Result<OrderDto>.Failure($"Error al agregar producto: {addResult.Error}");
		}

		// Confirmar
		var confirmResult = order.Confirm();
		if (!confirmResult.IsSuccess)
			return Result<OrderDto>.Failure($"Error al confirmar: {confirmResult.Error}");

		// Guardar
		_repository.Save(order);

		// Retornar
		return Result<OrderDto>.Success(MapToDto(order));
	}
}
```

---

## Mejores Prácticas

### 1. Siempre Verificar IsSuccess

```csharp
// ✅ Correcto
var result = operation.DoSomething();
if (!result.IsSuccess)
{
	logger.LogError(result.Error);
	return;
}

// ❌ Incorrecto - Puede acceder a Value cuando es null
var result = operation.DoSomething();
var value = result.Value; // Peligro si result.IsSuccess es false
```

> **¿Por qué es tan importante esto?** Porque `Value` puede ser `null` cuando la operación falló. Si accedes a `result.Value` sin verificar `IsSuccess` primero, estás accediendo a `default(T)`, que para tipos de referencia es `null` y para tipos de valor es `0` o `false`. Esto puede causar `NullReferenceException` en tiempo de ejecución, exactamente el tipo de error que intentamos evitar con el patrón Result.

### 2. Usar Nombres Descriptivos para Errores

```csharp
// ✅ Descriptivo
return Result.Failure("Stock insuficiente. Disponible: 5, Solicitado: 10");

// ❌ Genérico
return Result.Failure("Error");
```

> **¿Por qué un error genérico es peligroso?** Porque cuando ves "Error" en un log a las 3 AM, no tienes ni idea de qué falló. Un mensaje descriptivo como "Stock insuficiente. Disponible: 5, Solicitado: 10" te dice exactamente qué pasó, cuál era el estado del sistema, y cómo resolverlo. Es la diferencia entre resolver un problema en 5 minutos o en 2 horas.

### 3. Propagar Errores Correctamente

```csharp
// ✅ Propagar el error
public Result<Money> CalculateFinalPrice(Product product, decimal discount)
{
	var discountedResult = product.Price.Multiply(1 - discount);
	if (!discountedResult.IsSuccess)
		return Result<Money>.Failure(discountedResult.Error!);

	return discountedResult;
}

// ❌ Perder información del error
public Result<Money> CalculateFinalPrice(Product product, decimal discount)
{
	var discountedResult = product.Price.Multiply(1 - discount);
	if (!discountedResult.IsSuccess)
		return Result<Money>.Failure("Error calculando");

	return discountedResult;
}
```

> **¿Por qué propagar el error original?** Porque el error original contiene el contexto completo: qué operación falló, por qué falló, y qué valores tenía el sistema. Si creas un error nuevo como "Error calculando", pierdes toda esa información. Es como borrar el stack trace de una excepción: la siguiente persona que vea el error no tendrá pistas sobre la causa raíz.

### 4. Usar Map para Transformaciones Encadenadas

```csharp
// ✅ Limpio con Map
Result<Money> result = Result<Money>.Success(new Money(100, "USD"))
	.Map(money => money.Multiply(0.9))
	.Map(discounted => discounted.Add(new Money(5, "USD")));

// ❌ Verboso sin Map
var step1 = Result<Money>.Success(new Money(100, "USD"));
if (!step1.IsSuccess) return step1;

var step2WithDiscount = step1.Value!.Multiply(0.9);
if (!step2WithDiscount.IsSuccess) return step2WithDiscount;

var step3WithTax = step2WithDiscount.Value!.Add(new Money(5, "USD"));
if (!step3WithTax.IsSuccess) return step3WithTax;

Result<Money> result = step3WithTax;
```

> **¿Por qué Map es mejor?** Porque elimina la "bóveda de if/else" que se acumula cuando encadenas operaciones. Con Map, cada línea es una transformación pura. Sin Map, cada línea necesita su propio `if (!result.IsSuccess) return result;`. Con 5 transformaciones, eso son 10 líneas de boilerplate que Map elimina completamente.

### 5. Documentar Fallos Posibles

```csharp
/// <summary>
/// Obtiene el total del pedido.
/// </summary>
/// <returns>
/// Result<Money>.Success con el monto total si el cálculo es exitoso.
/// Result<Money>.Failure si hay errores en los subtotales de las líneas.
/// </returns>
public Result<Money> GetTotalAmount()
{
	// ...
}
```

> **¿Por qué es importante documentar fallos?** Porque cuando otro desarrollador usa tu método, necesita saber qué puede fallar sin leer tu código fuente. Los comentarios XML son la documentación viva que aparece en IntelliSense. Si no documentas los posibles fallos, el consumidor no sabe si debe envolver la llamada en un `try/catch` o si puede usar el valor directamente.

---

## Transición de Excepciones a Result<T>

Si tienes código con excepciones:

```csharp
// ❌ Excepciones
public Money CalculateTotal(int quantity)
{
	if (quantity <= 0)
		throw new DomainException("Cantidad debe ser mayor a cero");

	return Price.Multiply(quantity);
}
```

Conviértelo a Result<T>:

```csharp
// ✅ Result<T>
public Result<Money> CalculateTotal(int quantity)
{
	if (quantity <= 0)
		return Result<Money>.Failure("Cantidad debe ser mayor a cero");

	return Price.Multiply(quantity);
}
```

---

## Ventajas de Result<T> en Tests

```csharp
[Test]
public void CalculateSubtotal_WithInvalidQuantity_ReturnFailure()
{
	var product = new Product(ProductId.Create(), "Laptop", "High-end", 
							 new Money(1000, "USD"), 5);

	var result = product.CalculateSubtotal(-5);

	Assert.IsFalse(result.IsSuccess);
	Assert.AreEqual("La cantidad debe ser mayor a cero.", result.Error);
}

[Test]
public void CalculateSubtotal_WithValidQuantity_ReturnSuccess()
{
	var product = new Product(ProductId.Create(), "Laptop", "High-end",
							 new Money(1000, "USD"), 5);

	var result = product.CalculateSubtotal(3);

	Assert.IsTrue(result.IsSuccess);
	Assert.AreEqual(new Money(3000, "USD"), result.Value);
}
```

---

## Recursos Recomendados

- 📖 [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- 📖 [Result Pattern in C#](https://martinfowler.com/articles/replaceThrowWithNotification.html)
- 🎥 [Error Handling in Functional Programming](https://pragprog.com/titles/cdc-fsharp/functional-programming-in-csharp/)

---

**Última actualización:** 2024  
**Proyecto:** SoftwareLearningGuide.Core.Business
