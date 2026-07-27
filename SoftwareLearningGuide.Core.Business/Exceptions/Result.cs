namespace SoftwareLearningGuide.Core.Business.Exceptions;

/// <summary>
/// Result genérico que encapsula el resultado de una operación.
/// Puede ser exitosa (con valor) o fallida (con mensaje de error).
/// Permite un manejo explícito de errores sin excepciones.
/// </summary>
/// <typeparam name="T">Tipo del valor en caso de éxito</typeparam>
public class Result<T> {
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(T? value, bool isSuccess, string? error) {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Crea un resultado exitoso.
    /// </summary>
    public static Result<T> Success(T value) {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new Result<T>(value, true, null);
    }

    /// <summary>
    /// Crea un resultado fallido.
    /// </summary>
    public static Result<T> Failure(string error) {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("El mensaje de error no puede estar vacío.", nameof(error));

        return new Result<T>(default, false, error);
    }

    /// <summary>
    /// Aplica una función al valor si es exitoso, manteniendo el error si es fallido.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, Result<TNew>> mapper) {
        if (!IsSuccess)
            return Result<TNew>.Failure(Error!);

        return mapper(Value!);
    }

    /// <summary>
    /// Aplica una función al valor si es exitoso sin cambiar el tipo (efecto secundario).
    /// </summary>
    public Result<T> Tap(Action<T> action) {
        if (IsSuccess)
            action(Value!);

        return this;
    }

    /// <summary>
    /// Obtiene el valor o lanza una excepción si falló.
    /// </summary>
    public T GetValueOrThrow() {
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
public class Result {
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool isSuccess, string? error) {
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
    public static Result Failure(string error) {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("El mensaje de error no puede estar vacío.", nameof(error));

        return new(false, error);
    }

    /// <summary>
    /// Aplica una acción si es exitoso.
    /// </summary>
    public Result Tap(Action action) {
        if (IsSuccess)
            action();

        return this;
    }

    /// <summary>
    /// Convierte Result a Result<T> con un valor específico.
    /// </summary>
    public Result<T> ToResult<T>(T value) {
        if (!IsSuccess)
            return Result<T>.Failure(Error!);

        return Result<T>.Success(value);
    }

    /// <summary>
    /// Obtiene el resultado o lanza una excepción si falló.
    /// </summary>
    public void GetValueOrThrow() {
        if (!IsSuccess)
            throw new InvalidOperationException($"Result falló: {Error}");
    }

    public override string ToString() => IsSuccess ? "Success" : $"Failure: {Error}";
}
