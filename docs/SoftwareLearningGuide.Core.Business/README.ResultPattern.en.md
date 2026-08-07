# Result<T> Pattern - Functional Error Handling

This document explains the **Result<T>** pattern implemented in `SoftwareLearningGuide.Core.Business` and how it's used to handle errors explicitly and predictably.

![Result Pattern](https://img.shields.io/badge/Pattern-Result%3CT%3E-green)

#### Table of Contents

1. [What is Result<T>?](#what-is-resultt)
2. [Advantages over Exceptions](#advantages-over-exceptions)
3. [Implementation](#implementation)
4. [Basic Usage](#basic-usage)
5. [Result Composition](#result-composition)
6. [Project Examples](#project-examples)
7. [Best Practices](#best-practices)

---

## What is Result<T>?

**Result<T>** is a pattern that encapsulates the result of an operation, which can be:
- ✅ **Successful** - Contains a value of type `T`
- ❌ **Failed** - Contains an error message

Instead of throwing exceptions, the method returns a `Result<T>` that explicitly indicates whether the operation succeeded or not.

### Definition

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

	// Create a successful result
	public static Result<T> Success(T value) => new(value, true, null);

	// Create a failed result
	public static Result<T> Failure(string error) => new(default, false, error);

	// Useful composition methods
	public Result<TNew> Map<TNew>(Func<T, Result<TNew>> mapper) { ... }
	public Result<T> Tap(Action<T> action) { ... }
	public T GetValueOrThrow() { ... }
}

// For operations without return value
public class Result
{
	public bool IsSuccess { get; }
	public string? Error { get; }

	public static Result Success() => new(true, null);
	public static Result Failure(string error) => new(false, error);
}
```

---

## Advantages over Exceptions

### ❌ Problems with Exceptions

```csharp
public Money CalculateDiscount(Product product, decimal discountPercent)
{
	// What exceptions can it throw?
	// Should it be caught here or delegated?
	// Will the consumer know what exceptions to expect?

	if (discountPercent < 0 || discountPercent > 100)
		throw new DomainException("Invalid discount"); // What exception type?

	var discountAmount = product.Price.Multiply(discountPercent / 100);
	return product.Price.Subtract(discountAmount);
}

// Problematic usage
try
{
	var discountedPrice = CalculateDiscount(product, 150);
	// The compiler doesn't force me to handle the error
}
catch (DomainException ex)
{
	// What do I do here?
	Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
	// Generic catch for unexpected exceptions
}
```

### ✅ Solution with Result<T>

```csharp
public Result<Money> CalculateDiscount(Product product, decimal discountPercent)
{
	// The compiler forces the caller to validate

	if (discountPercent < 0 || discountPercent > 100)
		return Result<Money>.Failure("Discount must be between 0 and 100%");

	var discountAmount = product.Price.Multiply(discountPercent / 100);
	if (!discountAmount.IsSuccess)
		return discountAmount;

	return product.Price.Subtract(discountAmount.Value!);
}

// Explicit usage
var result = CalculateDiscount(product, 20);
if (!result.IsSuccess)
{
	Console.WriteLine($"Error: {result.Error}");
	return;
}

var discountedPrice = result.Value; // Guaranteed to exist
```

### Comparison

| Aspect | Exceptions | Result<T> |
|--------|------------|-----------|
| **Visibility** | Implicit (comments) | Explicit (type) |
| **Performance** | Slow (stack unwinding) | Fast |
| **Composition** | Difficult (try-catch) | Easy (Map, Tap) |
| **Type-safety** | No verification | Compiler verifies |
| **Readability** | Action vs Controlled flow | Clear flow |

> **Pattern philosophy:** Result\<T> is not just a technical pattern; it's a design philosophy. Instead of asking "what can fail?" and catching exceptions, you ask "what can go right?" and explicitly verify. This makes code more predictable and easier to test.

---

## Implementation

### Complete Implementation in the Project

> **Productive file:** [`SoftwareLearningGuide.Core.Business/Exceptions/Result.cs`](SoftwareLearningGuide.Core.Business/Exceptions/Result.cs)

```csharp
namespace SoftwareLearningGuide.Core.Business.Exceptions;

/// <summary>
/// Generic Result that encapsulates the result of an operation.
/// Can be successful (with value) or failed (with error message).
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
	/// Creates a successful result.
	/// </summary>
	public static Result<T> Success(T value)
	{
		if (value == null)
			throw new ArgumentNullException(nameof(value));

		return new Result<T>(value, true, null);
	}

	/// <summary>
	/// Creates a failed result.
	/// </summary>
	public static Result<T> Failure(string error)
	{
		if (string.IsNullOrWhiteSpace(error))
			throw new ArgumentException("Error message cannot be empty.", nameof(error));

		return new Result<T>(default, false, error);
	}

	/// <summary>
	/// Applies a function to the value if successful, maintaining the error if failed.
	/// Enables operation composition.
	/// </summary>
	public Result<TNew> Map<TNew>(Func<T, Result<TNew>> mapper)
	{
		if (!IsSuccess)
			return Result<TNew>.Failure(Error!);

		return mapper(Value!);
	}

	/// <summary>
	/// Applies an action if successful without changing the type (side effect).
	/// </summary>
	public Result<T> Tap(Action<T> action)
	{
		if (IsSuccess)
			action(Value!);

		return this;
	}

	/// <summary>
	/// Gets the value or throws an exception if failed.
	/// Useful in contexts where the error is fatal.
	/// </summary>
	public T GetValueOrThrow()
	{
		if (!IsSuccess)
			throw new InvalidOperationException($"Result failed: {Error}");

		return Value!;
	}

	/// <summary>
	/// Gets the value or returns a default value if failed.
	/// </summary>
	public T GetValueOrDefault(T defaultValue) => IsSuccess ? Value! : defaultValue;

	public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {Error}";
}

/// <summary>
/// Non-generic Result for operations that don't return a value (void).
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
	/// Creates a successful result.
	/// </summary>
	public static Result Success() => new(true, null);

	/// <summary>
	/// Creates a failed result.
	/// </summary>
	public static Result Failure(string error)
	{
		if (string.IsNullOrWhiteSpace(error))
			throw new ArgumentException("Error message cannot be empty.", nameof(error));

		return new(false, error);
	}

	/// <summary>
	/// Applies an action if successful.
	/// </summary>
	public Result Tap(Action action)
	{
		if (IsSuccess)
			action();

		return this;
	}

	/// <summary>
	/// Converts Result to Result<T> with a specific value.
	/// </summary>
	public Result<T> ToResult<T>(T value)
	{
		if (!IsSuccess)
			return Result<T>.Failure(Error!);

		return Result<T>.Success(value);
	}

	/// <summary>
	/// Gets the result or throws an exception if failed.
	/// </summary>
	public void GetValueOrThrow()
	{
		if (!IsSuccess)
			throw new InvalidOperationException($"Result failed: {Error}");
	}

	public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";
}
```

> **Why are there two classes Result and Result\<T>?** `Result` is for operations that don't return a value (like updating a name), and `Result<T>` is for operations that do return something (like calculating a price). Both follow the same philosophy: Success or Failure. The distinction exists because in C# a method that returns nothing (`void`) can't return a `Result<Money>`, so we need a non-generic version for those cases.

---

## Basic Usage

### 1. Creating Successful Results

```csharp
// Result<T> - Operation that returns a value
var money = Money.Create(100, "USD").Value!;
return Result<Money>.Success(money);

// Result - Operation without return value
public Result UpdateName(string newName)
{
	if (string.IsNullOrWhiteSpace(newName))
		return Result.Failure("Name cannot be empty.");

	Name = newName;
	return Result.Success();
}
```

### 2. Creating Failed Results

```csharp
// Result<T> - With generic value
if (amount < 0)
	return Result<Money>.Failure("Amount cannot be negative");

// Result - Without specific type
if (stockQuantity < 0)
	return Result.Failure("Stock cannot be negative");
```

### 3. Verifying Success/Failure

```csharp
var result = order.AddProduct(product, 5);

if (result.IsSuccess)
{
	Console.WriteLine("Product added successfully");
}
else
{
	Console.WriteLine($"Error: {result.Error}");
}

// Or more concisely
if (!result.IsSuccess)
{
	HandleError(result.Error);
	return;
}

// Proceed with success
ProcessOrder();
```

### 4. Getting Values

```csharp
var result = order.GetTotalAmount();

// Option 1: Check first
if (result.IsSuccess)
{
	var total = result.Value; // Guaranteed not null
}

// Option 2: Default value
var total = result.GetValueOrDefault(Money.Zero());

// Option 3: Throw if failed (for critical cases)
try
{
	var total = result.GetValueOrThrow();
}
catch (InvalidOperationException ex)
{
	// Handle critical case
}
```

---

## Result Composition

### Map - Transform the Value

```csharp
// Result<Money>.Map() - Transform money to another currency
var priceInUsd = Money.Create(100, "USD").Value!;
var moneyResult = Result<Money>.Success(priceInUsd);

// Apply discount
var discountedResult = moneyResult.Map(money => 
	money.Multiply(0.9) // 10% discount
);

if (discountedResult.IsSuccess)
{
	Console.WriteLine($"Discounted price: {discountedResult.Value}");
}
else
{
	Console.WriteLine($"Error: {discountedResult.Error}");
}

// Chain multiple transformations
var finalPrice = Result<Money>.Success(Money.Create(100, "USD").Value!)
	.Map(money => money.Multiply(0.9))  // 10% discount
	.Map(discounted => discounted.Add(Money.Create(5, "USD").Value!)) // +$5 tax
	.Map(withTax => withTax.Divide(2)); // Split between 2 people
```

> **Map is like an assembly line:** each step transforms the result from the previous one. If something fails at any step, the error propagates automatically without needing to write `if/else` on each line. Notice how the type keeps changing: `Money` → `Money` → `Money` → `Money`. The compiler verifies that each transformation is compatible.

### Tap - Side Effect

```csharp
// Tap allows executing code without changing the result
var result = product.UpdatePrice(Money.Create(99.99m, "USD").Value!)
	.Tap(() => logger.LogInformation("Price updated"))
	.Tap(() => NotifyPriceChange());

if (!result.IsSuccess)
{
	Console.WriteLine($"Error: {result.Error}");
}
```

> **Tap is like a sensor on the assembly line:** it doesn't modify the result, it just "observes" and executes a side effect (logging, notifications, metrics). It's useful for adding cross-cutting behaviors without cluttering the main logic. If the `Result` fails, the `Tap` simply doesn't execute.

### Manual Composition

```csharp
public Result<Money> CalculateFinalPrice(Product product, decimal discountPercent)
{
	// Calculate discount
	var discountResult = product.Price.Multiply(discountPercent / 100);
	if (!discountResult.IsSuccess)
		return Result<Money>.Failure($"Error calculating discount: {discountResult.Error}");

	// Subtract discount
	var finalPriceResult = product.Price.Subtract(discountResult.Value!);
	if (!finalPriceResult.IsSuccess)
		return Result<Money>.Failure($"Error calculating final price: {finalPriceResult.Error}");

	return finalPriceResult;
}

// Usage
var result = CalculateFinalPrice(product, 20);
if (result.IsSuccess)
{
	Console.WriteLine($"Final price: {result.Value}");
}
```

---

## Project Examples

### 1. Value Object - Money.cs

> **Why does `Money` return `Result<T>` instead of throwing exceptions?** Because `Money` is an immutable `record` and each arithmetic operation can fail for verifiable reasons (incompatible currency, zero divisor, negative result). By returning `Result<T>`, the consumer always knows they must verify the result before using the value. The private constructor of `Money` throws exceptions for unbreakable invariants (like negative amount), but operations between two `Money` objects return `Result<T>` because they are business context errors, not object invariant errors.

> **Production reference:** [`ValueObjects/Money.cs`](../../ValueObjects/Money.cs)

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
}
```

> **Notice the pattern?** Every method that can fail returns `Result` instead of throwing exceptions. This forces the caller to verify the result, eliminating runtime surprises. In `Money`, even simple comparisons like `IsGreaterThan` return `Result<bool>` because there could be an incompatible currency error. Error messages come from the centralized catalog in [`DomainErrors.cs`](../Errors/DomainErrors.cs).

### 2. Entity - Product.cs

```csharp
public Result UpdateName(string newName)
{
	if (string.IsNullOrWhiteSpace(newName))
		return Result.Failure("Product name cannot be empty.");

	Name = newName.Trim();
	UpdatedAt = DateTime.UtcNow;
	return Result.Success();
}

public Result<Money> CalculateSubtotal(int quantity)
{
	if (quantity <= 0)
		return Result<Money>.Failure("Quantity must be greater than zero.");

	return Price.Multiply(quantity);
}

public Result RemoveStock(int quantity)
{
	if (quantity <= 0)
		return Result.Failure("Quantity to remove must be greater than zero.");

	if (StockQuantity < quantity)
		return Result.Failure(
			$"Insufficient stock. Current stock: {StockQuantity}, requested: {quantity}");

	StockQuantity -= quantity;
	UpdatedAt = DateTime.UtcNow;
	return Result.Success();
}
```

> **Notice the pattern?** In `Product`, every method that modifies state (`UpdateName`, `RemoveStock`) returns `Result` instead of `void`. This means the compiler **forces** anyone calling these methods to ask: "what if it fails?". You can't use `RemoveStock` without first checking `IsSuccess`. It's like every method has a "traffic light" you must check before proceeding.

### 3. Aggregate - Order.cs

```csharp
public Result AddProduct(Product product, int quantity)
{
	if (product == null)
		return Result.Failure("Product cannot be null.");

	if (quantity <= 0)
		return Result.Failure("Quantity must be greater than zero.");

	if (quantity > MaxProductsPerLine)
		return Result.Failure(
			$"Cannot add more than {MaxProductsPerLine} units of the same product.");

	if (Status != OrderStatus.Pending)
		return Result.Failure(
			$"Cannot add products to an order in {Status} status.");

	if (!product.HasSufficientStock(quantity))
		return Result.Failure(
			$"Insufficient stock for product '{product.Name}'.");

	var existingLine = _lines.FirstOrDefault(l => l.ProductId.Equals(product.Id));
	if (existingLine != null)
	{
		var newQuantity = existingLine.Quantity + quantity;
		if (newQuantity > MaxProductsPerLine)
			return Result.Failure(
				$"Cannot exceed {MaxProductsPerLine} units of the same product.");

		return existingLine.UpdateQuantity(newQuantity);
	}
	else
	{
		var lineResult = OrderLine.Create(
			OrderLineId.Create(),
			product.Id,
			product.Name,
			product.Price,
			quantity);

		if (!lineResult.IsSuccess)
			return Result.Failure(lineResult.Error!);

		_lines.Add(lineResult.Value!);
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
			return Result<Money>.Failure($"Error calculating subtotal on line {i}");

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
		return Result.Failure("Cannot confirm an empty order.");

	if (Status != OrderStatus.Pending)
		return Result.Failure(
			$"Cannot confirm an order in {Status} status.");

	Status = OrderStatus.Confirmed;
	ConfirmedAt = DateTime.UtcNow;
	return Result.Success();
}
```

> **Notice the pattern?** In `Order`, validation is a cascade of `if/return` that checks each business rule before executing the action. Each `return Result.Failure(...)` is a "short circuit" that stops execution immediately. You don't need exceptions because the control flow is already designed to handle errors explicitly.

### 4. Usage in Application Layer

```csharp
public class OrderService
{
	public Result<OrderDto> CreateOrder(CreateOrderRequest request)
	{
		// Validate customer
		if (!TryGetCustomer(request.CustomerId, out var customer))
			return Result<OrderDto>.Failure("Customer not found");

		// Create address
		var addressResult = Address.Create(
			request.Street, request.City, request.State,
			request.PostalCode, request.Country);

		if (!addressResult.IsSuccess)
			return Result<OrderDto>.Failure($"Invalid address: {addressResult.Error}");

		// Create order
		var orderResult = Order.Create(OrderId.Create(), customer.Id, addressResult.Value!);
		if (!orderResult.IsSuccess)
			return Result<OrderDto>.Failure($"Error creating order: {orderResult.Error}");

		var order = orderResult.Value!;

		// Add products
		foreach (var lineRequest in request.Lines)
		{
			var product = GetProduct(lineRequest.ProductId);
			var addResult = order.AddProduct(product, lineRequest.Quantity);

			if (!addResult.IsSuccess)
				return Result<OrderDto>.Failure($"Error adding product: {addResult.Error}");
		}

		// Confirm
		var confirmResult = order.Confirm();
		if (!confirmResult.IsSuccess)
			return Result<OrderDto>.Failure($"Error confirming: {confirmResult.Error}");

		// Save
		_repository.Save(order);

		// Return
		return Result<OrderDto>.Success(MapToDto(order));
	}
}
```

---

## Best Practices

### 1. Always Verify IsSuccess

```csharp
// ✅ Correct
var result = operation.DoSomething();
if (!result.IsSuccess)
{
	logger.LogError(result.Error);
	return;
}

// ❌ Incorrect - May access Value when it's null
var result = operation.DoSomething();
var value = result.Value; // Dangerous if result.IsSuccess is false
```

> **Why is this so important?** Because `Value` can be `null` when the operation failed. If you access `result.Value` without checking `IsSuccess` first, you're accessing `default(T)`, which for reference types is `null` and for value types is `0` or `false`. This can cause `NullReferenceException` at runtime, exactly the type of error we're trying to avoid with the Result pattern.

### 2. Use Descriptive Error Names

```csharp
// ✅ Descriptive
return Result.Failure("Insufficient stock. Available: 5, Requested: 10");

// ❌ Generic
return Result.Failure("Error");
```

> **Why is a generic error dangerous?** Because when you see "Error" in a log at 3 AM, you have no idea what failed. A descriptive message like "Insufficient stock. Available: 5, Requested: 10" tells you exactly what happened, what the system state was, and how to fix it. It's the difference between solving a problem in 5 minutes or 2 hours.

### 3. Propagate Errors Correctly

```csharp
// ✅ Propagate the original error
public Result<Money> CalculateFinalPrice(Product product, decimal discount)
{
	var discountedResult = product.Price.Multiply(1 - discount);
	if (!discountedResult.IsSuccess)
		return Result<Money>.Failure(discountedResult.Error!);

	return discountedResult;
}

// ❌ Lose error information
public Result<Money> CalculateFinalPrice(Product product, decimal discount)
{
	var discountedResult = product.Price.Multiply(1 - discount);
	if (!discountedResult.IsSuccess)
		return Result<Money>.Failure("Calculation error");

	return discountedResult;
}
```

> **Why propagate the original error?** Because the original error contains the complete context: what operation failed, why it failed, and what the system values were. If you create a new error like "Calculation error", you lose all that information. It's like erasing an exception's stack trace: the next person who sees the error will have no clues about the root cause.

### 4. Use Map for Chained Transformations

```csharp
// ✅ Clean with Map
Result<Money> result = Result<Money>.Success(Money.Create(100, "USD").Value!)
	.Map(money => money.Multiply(0.9))
	.Map(discounted => discounted.Add(Money.Create(5, "USD").Value!));

// ❌ Verbose without Map
var step1 = Result<Money>.Success(Money.Create(100, "USD").Value!);
if (!step1.IsSuccess) return step1;

var step2WithDiscount = step1.Value!.Multiply(0.9);
if (!step2WithDiscount.IsSuccess) return step2WithDiscount;

var step3WithTax = step2WithDiscount.Value!.Add(Money.Create(5, "USD").Value!);
if (!step3WithTax.IsSuccess) return step3WithTax;

Result<Money> result = step3WithTax;
```

> **Why is Map better?** Because it eliminates the "if/else vault" that accumulates when chaining operations. With Map, each line is a pure transformation. Without Map, each line needs its own `if (!result.IsSuccess) return result;`. With 5 transformations, that's 10 lines of boilerplate that Map completely eliminates.

### 5. Document Possible Failures

```csharp
/// <summary>
/// Gets the order total.
/// </summary>
/// <returns>
/// Result<Money>.Success with the total amount if calculation is successful.
/// Result<Money>.Failure if there are errors in line subtotals.
/// </returns>
public Result<Money> GetTotalAmount()
{
	// ...
}
```

> **Why is it important to document failures?** Because when another developer uses your method, they need to know what can fail without reading your source code. XML comments are living documentation that appears in IntelliSense. If you don't document possible failures, the consumer doesn't know if they need to wrap the call in a `try/catch` or if they can use the value directly.

---

## Transitioning from Exceptions to Result<T>

If you have code with exceptions:

```csharp
// ❌ Exceptions
public Money CalculateTotal(int quantity)
{
	if (quantity <= 0)
		throw new DomainException("Quantity must be greater than zero");

	return Price.Multiply(quantity);
}
```

Convert it to Result<T>:

```csharp
// ✅ Result<T>
public Result<Money> CalculateTotal(int quantity)
{
	if (quantity <= 0)
		return Result<Money>.Failure("Quantity must be greater than zero");

	return Price.Multiply(quantity);
}
```

---

## Advantages of Result<T> in Tests

```csharp
[Test]
public void CalculateSubtotal_WithInvalidQuantity_ReturnFailure()
{
	var productResult = Product.Create(
		ProductId.Create(),
		"Laptop",
		"High-end",
		Money.Create(1000, "USD").Value!,
		5);
	var product = productResult.Value!;

	var result = product.CalculateSubtotal(-5);

	Assert.IsFalse(result.IsSuccess);
	Assert.AreEqual("Quantity must be greater than zero.", result.Error);
}

[Test]
public void CalculateSubtotal_WithValidQuantity_ReturnSuccess()
{
	var productResult = Product.Create(
		ProductId.Create(),
		"Laptop",
		"High-end",
		Money.Create(1000, "USD").Value!,
		5);
	var product = productResult.Value!;

	var result = product.CalculateSubtotal(3);

	Assert.IsTrue(result.IsSuccess);
	Assert.AreEqual(Money.Create(3000, "USD").Value!, result.Value);
}
```

---

## Recommended Resources

- 📖 [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- 📖 [Result Pattern in C#](https://martinfowler.com/articles/replaceThrowWithNotification.html)
- 🎥 [Error Handling in Functional Programming](https://pragprog.com/titles/cdc-fsharp/functional-programming-in-csharp/)

---

**Last updated:** 2024
**Project:** SoftwareLearningGuide.Core.Business