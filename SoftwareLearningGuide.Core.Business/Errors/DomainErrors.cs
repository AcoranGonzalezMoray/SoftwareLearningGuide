namespace SoftwareLearningGuide.Core.Business.Errors;

/// <summary>
/// Catálogo centralizado de mensajes de error del dominio.
/// Cada método genera un mensaje descriptivo con contexto de la entidad afectada y su ID.
/// Evita mensajes genéricos y facilita la localización/estandarización de errores.
/// </summary>
public static class DomainErrors {
    /// <summary>
    /// Errores de validación del Producto.
    /// </summary>
    public static class Product {
        public static string NameCannotBeEmpty(Guid productId)
            => $"El nombre del Producto con ID: {productId} no puede estar vacío.";

        public static string NameCannotBeEmpty()
            => "El nombre del Producto no puede estar vacío.";

        public static string DescriptionCannotBeEmpty(Guid productId)
            => $"La descripción del Producto con ID: {productId} no puede estar vacía.";

        public static string DescriptionCannotBeEmpty()
            => "La descripción del Producto no puede estar vacía.";

        public static string PriceMustBeGreaterThanZero(Guid productId)
            => $"El precio del Producto con ID: {productId} debe ser mayor a cero.";

        public static string PriceMustBeGreaterThanZero()
            => "El precio del Producto debe ser mayor a cero.";

        public static string PriceCannotBeNull()
            => "El precio del Producto no puede ser nulo.";

        public static string StockCannotBeNegative(Guid productId)
            => $"La cantidad de stock del Producto con ID: {productId} no puede ser negativa.";

        public static string QuantityMustBeGreaterThanZero(Guid productId)
            => $"La cantidad del Producto con ID: {productId} debe ser mayor a cero.";

        public static string QuantityMustBeGreaterThanZero()
            => "La cantidad debe ser mayor a cero.";

        public static string QuantityToAddMustBeGreaterThanZero(Guid productId)
            => $"La cantidad a añadir del Producto con ID: {productId} debe ser mayor a cero.";

        public static string QuantityToRemoveMustBeGreaterThanZero(Guid productId)
            => $"La cantidad a remover del Producto con ID: {productId} debe ser mayor a cero.";

        public static string InsufficientStock(Guid productId, int currentStock, int requested)
            => $"Stock insuficiente del Producto con ID: {productId}. Stock actual: {currentStock}, solicitado: {requested}";

        public static string InsufficientStock(string productName, int available, int requested)
            => $"Stock insuficiente para '{productName}'. Disponible: {available}, solicitado: {requested}";

        public static string PriceValidationFailed(Guid productId)
            => $"Error al validar el precio del Producto con ID: {productId}.";

        public static string NotFound(Guid productId)
            => $"No se encontró el Producto con ID: {productId} en el sistema.";
    }

    /// <summary>
    /// Errores de validación de la Línea de Pedido (OrderLine).
    /// </summary>
    public static class OrderLine {
        public static string ProductNameCannotBeEmpty(Guid productId)
            => $"El nombre del producto en la línea con ProductId: {productId} no puede estar vacío.";

        public static string ProductNameCannotBeEmpty()
            => "El nombre del producto en la línea no puede estar vacío.";

        public static string UnitPriceMustBeGreaterThanZero(Guid productId)
            => $"El precio unitario del Producto con ID: {productId} debe ser mayor a cero.";

        public static string UnitPriceMustBeGreaterThanZero()
            => "El precio unitario debe ser mayor a cero.";

        public static string QuantityMustBeGreaterThanZero(Guid productId)
            => $"La cantidad del Producto con ID: {productId} en la línea de pedido debe ser mayor a cero.";

        public static string QuantityMustBeGreaterThanZero()
            => "La cantidad de la línea de pedido debe ser mayor a cero.";
    }

    /// <summary>
    /// Errores de validación del Pedido (Order).
    /// </summary>
    public static class Order {
        public static string ProductCannotBeNull()
            => "El producto no puede ser nulo al agregar al Pedido.";

        public static string QuantityMustBeGreaterThanZero(Guid productId)
            => $"La cantidad del Producto con ID: {productId} debe ser mayor a cero.";

        public static string QuantityCannotBeNegative(Guid productId)
            => $"La cantidad del Producto con ID: {productId} no puede ser negativa.";

        public static string ExceedsMaxQuantityPerProduct(Guid productId, int maxQuantity)
            => $"No se pueden agregar más de {maxQuantity} unidades del Producto con ID: {productId}.";

        public static string ExceedsMaxQuantityPerProduct(int maxQuantity)
            => $"No se pueden superar {maxQuantity} unidades del mismo producto.";

        public static string ProductNotFoundInOrder(Guid productId)
            => $"El Producto con ID: {productId} no existe en el Pedido.";

        public static string RequiresPendingState(string currentState)
            => $"La operación requiere estado Pending. Estado actual: {currentState}";

        public static string CannotConfirmEmptyOrder()
            => "No se puede confirmar un Pedido vacío.";

        public static string TotalAmountMustBeGreaterThanZero()
            => "El monto total del Pedido debe ser mayor a cero.";

        public static string TotalAmountValidationFailed()
            => "Error al validar el monto total del Pedido.";

        public static string CannotShipOrderInState(string currentState)
            => $"Solo se pueden enviar Pedidos confirmados. Estado actual: {currentState}";

        public static string CannotDeliverOrderInState(string currentState)
            => $"Solo se pueden entregar Pedidos enviados. Estado actual: {currentState}";

        public static string CannotCancelOrderInState(string currentState)
            => $"No se puede cancelar un Pedido en estado {currentState}.";

        public static string ShippingAddressCannotBeNull()
            => "La dirección de envío del Pedido no puede ser nula.";

        public static string RefundQuantityMustBeGreaterThanZero(Guid productId)
            => $"La cantidad a reembolsar del Producto con ID: {productId} debe ser mayor a cero.";

        public static string RefundExceedsAvailable(Guid productId, int available)
            => $"No se pueden reembolsar unidades del Producto con ID: {productId}. Solo hay {available} en el Pedido.";

        public static string NoLinesToCalculateAverage()
            => "No hay líneas en el Pedido para calcular el promedio.";

        public static string ErrorCalculatingSubtotal(int lineIndex, string error)
            => $"Error al calcular subtotal en línea {lineIndex}: {error}";

        public static string PriceThresholdCannotBeNull()
            => "El umbral de precio no puede ser nulo.";

        public static string MinimumOrderValueCannotBeNull()
            => "El monto mínimo no puede ser nulo.";

        public static string OtherTotalCannotBeNull()
            => "El otro total no puede ser nulo.";

        public static string NotFound(Guid orderId)
            => $"No se encontró el Pedido con ID: {orderId}.";

        public static string ProductIdCannotBeNull()
            => "El ProductId no puede ser nulo.";

        public static string ProductId1CannotBeNull()
            => "El Primer ProductId no puede ser nulo.";

        public static string ProductId2CannotBeNull()
            => "El Segundo ProductId no puede ser nulo.";
    }

    /// <summary>
    /// Errores de validación del Cliente (Customer).
    /// </summary>
    public static class Customer {
        public static string FirstNameCannotBeEmpty(Guid customerId)
            => $"El nombre del Cliente con ID: {customerId} no puede estar vacío.";

        public static string FirstNameCannotBeEmpty()
            => "El nombre del Cliente no puede estar vacío.";

        public static string LastNameCannotBeEmpty(Guid customerId)
            => $"El apellido del Cliente con ID: {customerId} no puede estar vacío.";

        public static string LastNameCannotBeEmpty()
            => "El apellido del Cliente no puede estar vacío.";

        public static string EmailCannotBeNull()
            => "El correo del Cliente no puede ser nulo.";

        public static string ShippingAddressCannotBeNull()
            => "La dirección de envío del Cliente no puede ser nula.";

        public static string NotFound(Guid customerId)
            => $"No se encontró el Cliente con ID: {customerId}.";

        public static string NotFound(string customerId)
            => $"No se encontró el Cliente con ID: {customerId}.";
    }


    /// <summary>
    /// Errores de validación del Value Object Money.
    /// </summary>
    public static class MoneyErrors {
        public static string AmountCannotBeNegative(decimal amount)
            => $"El monto de dinero no puede ser negativo. Monto recibido: {amount}";

        public static string CurrencyCannotBeEmpty()
            => "La moneda no puede estar vacía.";

        public static string CurrencyMustBeThreeCharacters()
            => "La moneda debe ser un código de 3 caracteres (ej: USD, EUR, MXN).";

        public static string OtherMoneyCannotBeNull()
            => "El otro valor Money no puede ser nulo.";

        public static string CannotAddDifferentCurrencies(string currency1, string currency2)
            => $"No se pueden sumar montos con monedas diferentes: {currency1} y {currency2}";

        public static string CannotSubtractDifferentCurrencies(string currency1, string currency2)
            => $"No se pueden restar montos con monedas diferentes: {currency1} y {currency2}";

        public static string SubtractionResultCannotBeNegative(decimal result)
            => $"El resultado de la resta no puede ser negativo: {result}";

        public static string MultiplyFactorCannotBeNegative(decimal factor)
            => $"El factor de multiplicación no puede ser negativo: {factor}";

        public static string CannotDivideByZero()
            => "No se puede dividir un monto Money por cero.";

        public static string DivisorCannotBeNegative(decimal divisor)
            => $"El divisor no puede ser negativo: {divisor}";

        public static string CannotCompareDifferentCurrencies(string currency1, string currency2)
            => $"No se pueden comparar montos con monedas diferentes: {currency1} y {currency2}";
    }

    /// <summary>
    /// Errores de validación del Value Object Address.
    /// </summary>
    public static class AddressErrors {
        public static string StreetCannotBeEmpty()
            => "La calle de la Dirección no puede estar vacía.";

        public static string CityCannotBeEmpty()
            => "La ciudad de la Dirección no puede estar vacía.";

        public static string StateCannotBeEmpty()
            => "El estado/provincia de la Dirección no puede estar vacío.";

        public static string PostalCodeCannotBeEmpty()
            => "El código postal de la Dirección no puede estar vacío.";

        public static string CountryCannotBeEmpty()
            => "El país de la Dirección no puede estar vacío.";

        public static string CannotBeNull()
            => "La Dirección no puede ser nula.";
    }

    /// <summary>
    /// Errores de validación del Value Object Email.
    /// </summary>
    public static class EmailErrors {
        public static string CannotBeEmpty()
            => "El correo electrónico no puede estar vacío.";

        public static string CannotExceedMaxLength(int maxLength)
            => $"El correo electrónico no puede exceder {maxLength} caracteres.";

        public static string InvalidFormat(string email)
            => $"El correo electrónico '{email}' tiene un formato inválido.";
    }

    /// <summary>
    /// Errores de validación de Value Objects de ID.
    /// </summary>
    public static class IdErrors {
        public static string OrderIdCannotBeEmpty()
            => "El OrderId no puede ser un GUID vacío.";

        public static string ProductIdCannotBeEmpty()
            => "El ProductId no puede ser un GUID vacío.";

        public static string CustomerIdCannotBeEmpty()
            => "El CustomerId no puede ser un GUID vacío.";

        public static string OrderLineIdCannotBeEmpty()
            => "El OrderLineId no puede ser un GUID vacío.";
    }
}
