using CSharpFunctionalExtensions;

namespace Mcp.TaskAndResearch.Extensions;

/// <summary>
/// Extension methods for Result&lt;T&gt; and Maybe&lt;T&gt; patterns with async support
/// and ConfigureAwait(false) for library code.
/// </summary>
public static class AsyncResultExtensions
{
    /// <summary>
    /// Wraps an async operation that may throw into a Result&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of the successful result.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>A Result containing either the value or the error message.</returns>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation().ConfigureAwait(false);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(ex.Message);
        }
    }

    /// <summary>
    /// Wraps an async operation that may throw into a Result.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <returns>A Result indicating success or containing the error message.</returns>
    public static async Task<Result> TryAsync(Func<Task> operation)
    {
        try
        {
            await operation().ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Wraps a synchronous operation that may throw into a Result&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of the successful result.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>A Result containing either the value or the error message.</returns>
    public static Result<T> Try<T>(Func<T> operation)
    {
        try
        {
            return Result.Success(operation());
        }
        catch (Exception ex)
        {
            return Result.Failure<T>(ex.Message);
        }
    }

    /// <summary>
    /// Executes an async side effect on a successful Result, properly using ConfigureAwait(false).
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">The async side effect to execute.</param>
    /// <returns>The original result after executing the side effect.</returns>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action(result.Value).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Executes an async side effect on a successful Result, properly using ConfigureAwait(false).
    /// </summary>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">The async side effect to execute.</param>
    /// <returns>The original result after executing the side effect.</returns>
    public static async Task<Result> TapAsync(
        this Task<Result> resultTask,
        Func<Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            await action().ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Converts a nullable value to Maybe&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>Maybe with value if not null, otherwise Maybe.None.</returns>
    public static Maybe<T> ToMaybe<T>(this T? value) where T : class
        => value is null ? Maybe<T>.None : Maybe<T>.From(value);

    /// <summary>
    /// Converts a nullable struct value to Maybe&lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable struct value.</param>
    /// <returns>Maybe with value if has value, otherwise Maybe.None.</returns>
    public static Maybe<T> ToMaybe<T>(this T? value) where T : struct
        => value.HasValue ? Maybe<T>.From(value.Value) : Maybe<T>.None;

    /// <summary>
    /// Converts a Result&lt;T&gt; to Maybe&lt;T&gt;, discarding any error information.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>Maybe with value if success, otherwise Maybe.None.</returns>
    public static Maybe<T> ToMaybe<T>(this Result<T> result)
        => result.IsSuccess ? Maybe<T>.From(result.Value) : Maybe<T>.None;

    /// <summary>
    /// Maps a successful async Result to a new value, using ConfigureAwait(false).
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    /// <typeparam name="TResult">The target type.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A new Result with the mapped value or the original error.</returns>
    public static async Task<Result<TResult>> MapAsync<T, TResult>(
        this Task<Result<T>> resultTask,
        Func<T, Task<TResult>> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure<TResult>(result.Error);
        }
        var mappedValue = await mapper(result.Value).ConfigureAwait(false);
        return Result.Success(mappedValue);
    }

    /// <summary>
    /// Binds a successful async Result to a new Result, using ConfigureAwait(false).
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    /// <typeparam name="TResult">The target type.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="binder">The binding function.</param>
    /// <returns>The bound Result or the original error.</returns>
    public static async Task<Result<TResult>> BindAsync<T, TResult>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TResult>>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure<TResult>(result.Error);
        }
        return await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Binds a successful async Result to a new Result, using ConfigureAwait(false).
    /// </summary>
    /// <typeparam name="T">The source type.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="binder">The binding function.</param>
    /// <returns>The bound Result or the original error.</returns>
    public static async Task<Result> BindAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result>> binder)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }
        return await binder(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a side effect on failure, using ConfigureAwait(false).
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="action">The action to execute on failure.</param>
    /// <returns>The original result.</returns>
    public static async Task<Result<T>> OnFailureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<string, Task> action)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            await action(result.Error).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Ensures a condition is met on the result value, or returns a failure.
    /// </summary>
    /// <typeparam name="T">The type of the result value.</typeparam>
    /// <param name="resultTask">The result task.</param>
    /// <param name="predicate">The condition to check.</param>
    /// <param name="errorMessage">The error message if condition fails.</param>
    /// <returns>The original result if condition passes, otherwise a failure.</returns>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        string errorMessage)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result;
        }
        var conditionMet = await predicate(result.Value).ConfigureAwait(false);
        return conditionMet ? result : Result.Failure<T>(errorMessage);
    }
}
