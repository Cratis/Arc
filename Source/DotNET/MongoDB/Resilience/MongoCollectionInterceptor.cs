// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Castle.DynamicProxy;
using MongoDB.Driver;
using Polly;

namespace Cratis.Arc.MongoDB.Resilience;

/// <summary>
/// Represents an interceptor for <see cref="IMongoCollection{TDocument}"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MongoCollectionInterceptor"/> class.
/// </remarks>
/// <param name="resiliencePipeline">The <see cref="ResiliencePipeline"/> to use.</param>
/// <param name="openConnectionSemaphore">The <see cref="SemaphoreSlim"/> for keeping track of open connections.</param>
public class MongoCollectionInterceptor(
    ResiliencePipeline resiliencePipeline,
    SemaphoreSlim openConnectionSemaphore) : IInterceptor
{
    /// <inheritdoc/>
    public void Intercept(IInvocation invocation)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        invocation.ReturnValue = tcs.Task;

        var cancellationToken = ExtractCancellationToken(invocation);

        _ = ExecuteThroughPipeline(invocation, tcs, cancellationToken);
    }

    static CancellationToken ExtractCancellationToken(IInvocation invocation) =>
        invocation.Arguments.FirstOrDefault(argument => argument is CancellationToken) as CancellationToken? ?? CancellationToken.None;

    static async Task ExecuteMongoOperation(IInvocation invocation, TaskCompletionSource tcs)
    {
        var result = (invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments) as Task)!;
        await result.ConfigureAwait(false);

        if (result.IsCanceled)
        {
            tcs.SetCanceled();
        }
        else
        {
            tcs.SetResult();
        }
    }

    async Task ExecuteThroughPipeline(IInvocation invocation, TaskCompletionSource tcs, CancellationToken cancellationToken)
    {
        try
        {
            await resiliencePipeline.ExecuteAsync(
                async _ =>
                {
                    if (!await TryAcquireSemaphore(tcs, cancellationToken))
                    {
                        return ValueTask.CompletedTask;
                    }

                    try
                    {
                        await ExecuteMongoOperation(invocation, tcs);
                    }
                    catch (OperationCanceledException)
                    {
                        tcs.SetCanceled();
                    }
                    catch (MongoCommandException ex) when (ex.Message.Contains(WellKnownErrorMessages.CollectionNotFound, StringComparison.OrdinalIgnoreCase))
                    {
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
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
            tcs.TrySetCanceled(cancellationToken);
        }
    }

    async Task<bool> TryAcquireSemaphore(TaskCompletionSource tcs, CancellationToken cancellationToken)
    {
        try
        {
            if (!await openConnectionSemaphore.WaitAsync(1000, cancellationToken))
            {
                tcs.SetException(new TimeoutException("Failed to acquire semaphore."));
                return false;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled while waiting for a slot. The pipeline's result is not awaited, so the caller's task has to
            // be completed here or it would never finish.
            tcs.SetCanceled(cancellationToken);
            return false;
        }

        return true;
    }
}
