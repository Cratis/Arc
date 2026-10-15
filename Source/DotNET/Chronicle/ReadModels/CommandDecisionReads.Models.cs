// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

internal sealed partial class CommandDecisionReads
{
    /// <summary>
    /// Resolves and releases a plain model once, sharing the instance with validators, Provide and Handle.
    /// </summary>
    /// <typeparam name="T">The read model.</typeparam>
    /// <param name="key">The resolved key.</param>
    /// <param name="cancellationToken">The invocation cancellation.</param>
    /// <returns>The released model, or null for an absent instance.</returns>
    internal async Task<T?> GetInstance<T>(ReadModelKey key, CancellationToken cancellationToken)
        where T : class
    {
        var read = await Get<T>(key, cancellationToken);
        var invocation = InvocationForCurrentPolicy()!;
        async Task<object?> Release() => read.Instance is null ? null : await readModels.Release(read.Instance);
        var unit = CommandValidationExecution.Current is null ? UnitOfWorkFor(ReadMode.Protected) : null;
        var instance = CacheFor(invocation, unit).Instances.GetOrAdd(read, static (_, release) => new Lazy<Task<object?>>(release, LazyThreadSafetyMode.ExecutionAndPublication), Release);

        return (T?)await instance.Value;
    }
}
