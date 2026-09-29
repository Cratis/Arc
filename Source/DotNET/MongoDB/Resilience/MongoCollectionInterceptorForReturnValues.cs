// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.Arc.MongoDB.Resilience;

/// <summary>
/// Represents an interceptor for <see cref="IMongoCollection{TDocument}"/> for methods that returns a <see cref="Task{T}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoCollectionInterceptorForReturnValues"/> class.
/// </remarks>
/// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
/// <param name="openConnectionSemaphore">The <see cref="SemaphoreSlim"/> for keeping track of open connections.</param>
public class MongoCollectionInterceptorForReturnValues(
    ResiliencePipeline resiliencePipeline,
    SemaphoreSlim openConnectionSemaphore) : IInterceptor
{
    /// <inheritdoc/>
    public void Intercept(IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType.GetGenericArguments()[0];
        var taskCompletionSource = CreateTaskCompletionSource(returnType);

        invocation.ReturnValue = GetTaskFromCompletionSource(taskCompletionSource);
        var cancellationToken = ExtractCancellationToken(invocation);

        _ = ExecuteThroughPipeline(invocation, taskCompletionSource, returnType, cancellationToken);
    }

    static object CreateTaskCompletionSource(Type returnType)
    {
        var taskType = typeof(TaskCompletionSource<>).MakeGenericType(returnType);
        return Activator.CreateInstance(taskType, TaskCreationOptions.RunContinuationsAsynchronously)!;
    }

    static Task GetTaskFromCompletionSource(object taskCompletionSource)
    {
        var tcsType = taskCompletionSource.GetType();
        return (tcsType.GetProperty(nameof(TaskCompletionSource<object>.Task))!.GetValue(taskCompletionSource) as Task)!;
    }

    static CancellationToken ExtractCancellationToken(IInvocation invocation) =>
        invocation.Arguments.FirstOrDefault(argument => argument is CancellationToken) as CancellationToken? ?? CancellationToken.None;

    static async Task ExecuteMongoOperation(IInvocation invocation, object taskCompletionSource)
    {
        var result = (invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments) as Task)!;
        await result.ConfigureAwait(false);

        if (result.IsCanceled)
        {
            SetCanceled(taskCompletionSource);
        }
        else
        {
            SetResult(taskCompletionSource, result);
        }
    }

    static void SetResult(object taskCompletionSource, Task result)
    {
        var tcsType = taskCompletionSource.GetType();
        var setResultMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.SetResult))!;

#pragma warning disable CA1849 // Synchronous blocks
        var taskResult = result.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(result);
        setResultMethod.Invoke(taskCompletionSource, [taskResult]);
#pragma warning restore CA1849 // Synchronous blocks
    }

    static void SetException(object taskCompletionSource, Exception exception)
    {
        var tcsType = taskCompletionSource.GetType();
        var setExceptionMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.SetException), [typeof(Exception)])!;
        setExceptionMethod.Invoke(taskCompletionSource, [exception]);
    }

    static void TrySetCanceled(object taskCompletionSource)
    {
        var tcsType = taskCompletionSource.GetType();
        var trySetCanceledMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.TrySetCanceled), [])!;
        trySetCanceledMethod.Invoke(taskCompletionSource, []);
    }

    static void SetCanceled(object taskCompletionSource)
    {
        var tcsType = taskCompletionSource.GetType();
        var setCanceledMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.SetCanceled), [])!;
        setCanceledMethod.Invoke(taskCompletionSource, []);
    }

    static void SetDefaultValueForCollectionNotFound(object taskCompletionSource, Type returnType, IInvocation invocation)
    {
        var defaultValue = CreateDefaultValueForType(returnType, invocation);
        var tcsType = taskCompletionSource.GetType();
        var setResultMethod = tcsType.GetMethod(nameof(TaskCompletionSource<object>.SetResult))!;
        setResultMethod.Invoke(taskCompletionSource, [defaultValue]);
    }

    static object? CreateDefaultValueForType(Type returnType, IInvocation invocation)
    {
        if (IsAsyncCursorType(returnType))
        {
            return CreateEmptyAsyncCursor(returnType);
        }

        if (IsChangeStreamCursorType(returnType))
        {
            return CreateRetryingChangeStreamCursor(returnType, invocation);
        }

        if (returnType.IsValueType)
        {
            return Activator.CreateInstance(returnType);
        }

        return null;
    }

    static bool IsAsyncCursorType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncCursor<>);

    static bool IsChangeStreamCursorType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IChangeStreamCursor<>);

    static object CreateEmptyAsyncCursor(Type asyncCursorType)
    {
        var elementType = asyncCursorType.GetGenericArguments()[0];
        var emptyAsyncCursorType = typeof(EmptyAsyncCursor<>).MakeGenericType(elementType);
        return Activator.CreateInstance(emptyAsyncCursorType)!;
    }

    static object CreateRetryingChangeStreamCursor(Type changeStreamCursorType, IInvocation invocation)
    {
        var elementType = changeStreamCursorType.GetGenericArguments()[0];
        var retryingChangeStreamCursorType = typeof(RetryingChangeStreamCursor<>).MakeGenericType(elementType);
        return Activator.CreateInstance(retryingChangeStreamCursorType, invocation, TimeSpan.FromSeconds(1))!;
    }

    async Task ExecuteThroughPipeline(IInvocation invocation, object taskCompletionSource, Type returnType, CancellationToken cancellationToken)
    {
        try
        {
            await resiliencePipeline.ExecuteAsync(
                async (_) =>
                {
                    if (!await TryAcquireSemaphore(taskCompletionSource, cancellationToken))
                    {
                        return ValueTask.CompletedTask;
                    }

                    try
                    {
                        await ExecuteMongoOperation(invocation, taskCompletionSource);
                    }
                    catch (OperationCanceledException)
                    {
                        SetCanceled(taskCompletionSource);
                    }
                    catch (MongoCommandException ex) when (ex.Message.Contains(WellKnownErrorMessages.CollectionNotFound, StringComparison.OrdinalIgnoreCase))
                    {
                        SetDefaultValueForCollectionNotFound(taskCompletionSource, returnType, invocation);
                    }
                    catch (Exception ex)
                    {
                        SetException(taskCompletionSource, ex);
                    }
                    finally
                    {
                        openConnectionSemaphore.Release();
                    }

                    return ValueTask.CompletedTask;
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The pipeline does not invoke the callback for a token that is already cancelled, so nothing else would
            // complete the caller's task.
            TrySetCanceled(taskCompletionSource);
        }
    }

    async Task<bool> TryAcquireSemaphore(object taskCompletionSource, CancellationToken cancellationToken)
    {
        try
        {
            if (!await openConnectionSemaphore.WaitAsync(1000, cancellationToken))
            {
                SetException(taskCompletionSource, new TimeoutException("Failed to acquire semaphore."));
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled while waiting for a slot. The pipeline's result is not awaited, so the caller's task has to
            // be completed here or it would never finish.
            SetCanceled(taskCompletionSource);
            return false;
        }

        return true;
    }
}
