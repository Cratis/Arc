// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Command-aware decision reads. The invocation, rather than the DI scope, owns both the cache and the issued tokens.
/// </summary>
/// <param name="inner">The Chronicle reader for this target.</param>
/// <param name="eventStore">The event store identifying the read target.</param>
/// <param name="readModels">The legacy reader for command-level opt-out.</param>
internal sealed partial class CommandDecisionReads(IDecisionReads inner, IEventStore eventStore, IReadModels readModels) : IDecisionReads
{
    enum ReadMode
    {
        Protected = 0,
        Unprotected = 1,
        Validation = 2
    }

    /// <inheritdoc/>
    public DecisionReadDescription Describe<T>()
        where T : class => inner.Describe<T>();

    /// <inheritdoc/>
    public DecisionReadAdmission Admit<T>()
        where T : class => Admit<T>(false);

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        // Only [ProtectedDecision] changes the Chronicle contract. Unmarked and [Unprotected] callers keep detached reads.
        ReadProtectionMeasurement.Current?.DecisionRead<T>(inner, key.Value, detached: true);
        RefuseProtectedDetachedRead();

        return inner.GetDetached<T>(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class => Get<T>(key, null, cancellationToken);

    /// <summary>
    /// Enforces enrollment for protected commands.
    /// </summary>
    /// <exception cref="DecisionReadCouldNotBeAcquired">A protected command requested a detached read.</exception>
    static void RefuseProtectedDetachedRead()
    {
        if (CommandDecisionPolicy.Mode == CommandDecisionMode.Protected)
        {
            throw new DecisionReadCouldNotBeAcquired(new DetachedDecisionReadRefused());
        }
    }

    /// <summary>
    /// Checks the command profile and invocation provenance before reading.
    /// </summary>
    /// <param name="invocation">The current read invocation.</param>
    /// <exception cref="DecisionReadRequiresProtectionProfile">The profile ended before the read invocation.</exception>
    /// <exception cref="ProtectedDecisionReadRequiresProtectedCommand">The command has no decision profile.</exception>
    /// <exception cref="ProtectedDecisionReadRequiresActiveInvocation">There is no matching read invocation.</exception>
    static void VerifyInvocation(Invocation? invocation)
    {
        if (!CommandDecisionPolicy.IsActive)
        {
            if (invocation is not null)
            {
                throw new DecisionReadRequiresProtectionProfile();
            }

            return;
        }

        if (CommandDecisionPolicy.Mode == CommandDecisionMode.Legacy)
        {
            throw new ProtectedDecisionReadRequiresProtectedCommand();
        }
        if (invocation is null || !ReferenceEquals(invocation.PolicyToken, CommandDecisionPolicy.Token))
        {
            throw new ProtectedDecisionReadRequiresActiveInvocation();
        }
    }

    /// <summary>
    /// Resolves the command-owned transaction for an enrolled read.
    /// </summary>
    /// <param name="mode">The read mode.</param>
    /// <returns>The command transaction, or null for an advisory read.</returns>
    /// <exception cref="DecisionReadRequiresUnitOfWork">There is no active command transaction.</exception>
    /// <exception cref="ProtectedDecisionRequiresOwnerCapableUnitOfWork">The transaction cannot own protected reads.</exception>
    static IUnitOfWork? UnitOfWorkFor(ReadMode mode)
    {
        if (mode != ReadMode.Protected)
        {
            return null;
        }
        if (!CommandTransaction.TryGetActive(out var unitOfWork))
        {
            throw new DecisionReadRequiresUnitOfWork();
        }
        if (unitOfWork is not UnitOfWork)
        {
            throw new ProtectedDecisionRequiresOwnerCapableUnitOfWork();
        }

        return unitOfWork;
    }

    /// <summary>
    /// Checks the selected admission profile.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="forStream">Whether to check stream admission.</param>
    /// <returns>The admission result.</returns>
    /// <exception cref="DecisionReadCouldNotBeAcquired">Protected admission could not be checked.</exception>
    DecisionReadAdmission Admit<T>(bool forStream)
        where T : class
    {
        try
        {
            return forStream ? inner.AdmitForStream<T>() : inner.Admit<T>();
        }
        catch (Exception ex) when (CommandDecisionPolicy.IsProtected && ex is not OperationCanceledException)
        {
            throw new DecisionReadCouldNotBeAcquired(ex);
        }
    }

    /// <summary>
    /// Reads through the shared invocation policy, surfacing protected acquisition failures.
    /// </summary>
    /// <typeparam name="T">The read model type.</typeparam>
    /// <param name="key">The cache's source key.</param>
    /// <param name="streamScope">The optional stream dimensions.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The invocation's decision read.</returns>
    /// <exception cref="DecisionReadCouldNotBeAcquired">The protected read could not be acquired.</exception>
    async Task<DecisionRead<T>> Get<T>(ReadModelKey key, StreamScope? streamScope, CancellationToken cancellationToken)
        where T : class
    {
        ReadProtectionMeasurement.Current?.DecisionRead<T>(inner, key.Value, streamScope?.EventStreamType, streamScope?.EventStreamId, streamScope?.EventSourceType);
        try
        {
            return await GetForInvocation<T>(key, streamScope, cancellationToken);
        }
        catch (Exception ex) when (CommandDecisionPolicy.IsProtected && ex is not (OperationCanceledException or DecisionReadCouldNotBeAcquired))
        {
            throw new DecisionReadCouldNotBeAcquired(ex);
        }
    }

    async Task<DecisionRead<T>> GetForInvocation<T>(ReadModelKey key, StreamScope? streamScope, CancellationToken cancellationToken)
        where T : class
    {
        var validation = CommandValidationExecution.Current;
        var invocation = validation is { } validating
            ? _validationInvocations.GetValue(validating.Token, _ => new Invocation(validating.CommandType, CommandDecisionPolicy.Token, null))
            : CurrentInvocation();
        VerifyInvocation(invocation);
        if (!CommandDecisionPolicy.IsActive)
        {
            // Outside a command, preserve Chronicle's ambient unit-of-work contract.
            return await ReadAmbient<T>(key, streamScope, cancellationToken);
        }

        var mode = CommandDecisionPolicy.Mode == CommandDecisionMode.Unprotected ? ReadMode.Unprotected :
            validation is not null ? ReadMode.Validation : ReadMode.Protected;
        var unitOfWork = UnitOfWorkFor(mode);

        // Include the target and stream: supplied providers and sibling streams must not share a cached fold.
        var cacheKey = (typeof(T), (string)key, mode, (string)eventStore.Name, (string)eventStore.Namespace, streamScope);
        async Task<object> CreateRead() => mode == ReadMode.Unprotected
            ? await ReadAdvisory<T>(key, streamScope, cancellationToken)
            : await ReadDetached<T>(key, streamScope, cancellationToken);
        var task = invocation!.Reads.GetOrAdd(
            cacheKey,
            static (_, readFactory) => new Lazy<Task<object>>(readFactory, LazyThreadSafetyMode.ExecutionAndPublication),
            CreateRead);
        var read = (DecisionRead<T>)await task.Value;

        // Use the command's owner, not an ambient Chronicle manager, and re-enroll cached tokens intentionally.
        unitOfWork?.AddDecisionRead(read);
        invocation.Issued.TryAdd(read, 0);

        return read;
    }

    async Task<T?> ReadLegacy<T>(ReadModelKey key)
        where T : class
    {
        var instance = await readModels.GetInstanceById<T>(key);

        return instance is null ? null : await readModels.Release(instance);
    }
}
