namespace SoftwareLearningGuide.Core.Business.Entities;

using SoftwareLearningGuide.Core.Business.DomainEvents;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using SoftwareLearningGuide.Core.Business.ValueObjects;

/// <summary>
/// Entidad que representa un Producto en el dominio de e-commerce.
/// Tiene identidad única y puede cambiar a lo largo del tiempo.
/// Los atributos cambian pero mantienen el ID como identificador.
/// </summary>
public class Product : ProduceEvents {
    public ProductId Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int StockQuantity { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    private Product() { }

    public Product(ProductId id, string name, string description, Money price, int stockQuantity) {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(DomainErrors.Product.NameCannotBeEmpty(id.Value));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(DomainErrors.Product.DescriptionCannotBeEmpty(id.Value));

        if (price == null)
            throw new ArgumentNullException(nameof(price));

        if (price.Amount <= 0)
            throw new ArgumentException(DomainErrors.Product.PriceMustBeGreaterThanZero(id.Value));

        if (stockQuantity < 0)
            throw new ArgumentException(DomainErrors.Product.StockCannotBeNegative(id.Value));

        Id = id;
        Name = name.Trim();
        Description = description.Trim();
        Price = price;
        StockQuantity = stockQuantity;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductCreatedDomainEvent {
            ProductId = id.Value,
            Name = name.Trim(),
            Price = price.Amount,
            Currency = price.Currency
        });
    }

    /// <summary>
    /// Crea una instancia de Product con validación.
    /// </summary>
    public static Result<Product> Create(ProductId id, string name, string description, Money price, int stockQuantity) {
        try {
            return Result<Product>.Success(new Product(id, name, description, price, stockQuantity));
        }
        catch (Exception ex) {
            return Result<Product>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Actualiza el nombre del producto.
    /// </summary>
    public Result UpdateName(string newName) {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure(DomainErrors.Product.NameCannotBeEmpty(Id.Value));

        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Actualiza la descripción del producto.
    /// </summary>
    public Result UpdateDescription(string newDescription) {
        if (string.IsNullOrWhiteSpace(newDescription))
            return Result.Failure(DomainErrors.Product.DescriptionCannotBeEmpty(Id.Value));

        Description = newDescription.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Actualiza el precio del producto.
    /// </summary>
    public Result UpdatePrice(Money newPrice) {
        if (newPrice == null)
            return Result.Failure(DomainErrors.Product.PriceCannotBeNull());

        if (newPrice.Amount <= 0)
            return Result.Failure(DomainErrors.Product.PriceMustBeGreaterThanZero(Id.Value));

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Añade stock al producto.
    /// </summary>
    public Result AddStock(int quantity) {
        if (quantity <= 0)
            return Result.Failure(DomainErrors.Product.QuantityToAddMustBeGreaterThanZero(Id.Value));

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Reduce stock del producto. Devuelve Failure si no hay suficiente stock.
    /// </summary>
    public Result RemoveStock(int quantity) {
        if (quantity <= 0)
            return Result.Failure(DomainErrors.Product.QuantityToRemoveMustBeGreaterThanZero(Id.Value));

        if (StockQuantity < quantity)
            return Result.Failure(
                DomainErrors.Product.InsufficientStock(Id.Value, StockQuantity, quantity));

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        if (StockQuantity <= 10) {
            AddDomainEvent(new ProductStockLowDomainEvent {
                ProductId = Id.Value,
                CurrentStock = StockQuantity
            });
        }

        return Result.Success();
    }

    /// <summary>
    /// Verifica si hay stock disponible (más de 0 unidades).
    /// </summary>
    public bool IsInStock() => StockQuantity > 0;

    /// <summary>
    /// Verifica si hay suficiente stock disponible.
    /// </summary>
    public bool HasSufficientStock(int quantity) => StockQuantity >= quantity;

    /// <summary>
    /// Verifica si el producto puede ser comprado en la cantidad indicada.
    /// Valida stock y precio usando Money.IsGreaterThan.
    /// </summary>
    public Result<bool> CanBePurchased(int quantity) {
        if (quantity <= 0)
            return Result<bool>.Failure(DomainErrors.Product.QuantityMustBeGreaterThanZero(Id.Value));

        if (!HasSufficientStock(quantity))
            return Result<bool>.Success(false);

        var minimumPrice = Money.Create(0, Price.Currency);
        if (!minimumPrice.IsSuccess)
            return Result<bool>.Failure(DomainErrors.Product.PriceValidationFailed(Id.Value));

        var hasValidPrice = Price.IsGreaterThan(minimumPrice.Value!);
        if (!hasValidPrice.IsSuccess)
            return Result<bool>.Failure(hasValidPrice.Error ?? DomainErrors.Product.PriceValidationFailed(Id.Value));

        return Result<bool>.Success(hasValidPrice.Value!);
    }

    /// <summary>
    /// Calcula el subtotal para una cantidad específica del producto.
    /// </summary>
    public Result<Money> CalculateSubtotal(int quantity) {
        if (quantity <= 0)
            return Result<Money>.Failure(DomainErrors.Product.QuantityMustBeGreaterThanZero(Id.Value));

        return Price.Multiply(quantity);
    }

    public override bool Equals(object? obj) {
        if (obj is not Product other)
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
