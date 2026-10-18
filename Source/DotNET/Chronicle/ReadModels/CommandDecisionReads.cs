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
        where T : class => Get<T>(key, null, null, cancellationToken);

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetFor<T>(IUnitOfWork unitOfWork, ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class => Get<T>(key, null, unitOfWork, cancellationToken);

    /// <summary>
    /// Enforces enrollment for protected commands.
    /// </summary>
    /// <exception cref="DecisionReadCouldNotBeAcquired">A protected command requested a detached read.</exception>
    static void RefuseProtectedDetachedRead()
    {
        if (CommandDecisionPolicy.Mode == CommandDecisionMode.Protected && CommandDecisionPolicy.ReadProtection != ReadProtectionMode.On)
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

        if (CommandDecisionPolicy.Mode == CommandDecisionMode.Legacy && CommandDecisionPolicy.ReadProtection != ReadProtectionMode.On)
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
        var unitOfWork = TransactionalCommandScope.CurrentProtectedUnit;
        if (unitOfWork is null && !CommandTransaction.TryGetActive(out unitOfWork))
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
    /// <param name="requestedUnit">The explicitly requested receiving unit, when supplied.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The invocation's decision read.</returns>
    /// <exception cref="DecisionReadCouldNotBeAcquired">The protected read could not be acquired.</exception>
    /// <exception cref="ReadIncomplete">The fresh fold did not reach its probe.</exception>
    /// <exception cref="ProtectedReadModelDoesNotExist">The protected key has a refused shape.</exception>
    async Task<DecisionRead<T>> Get<T>(ReadModelKey key, StreamScope? streamScope, IUnitOfWork? requestedUnit, CancellationToken cancellationToken)
        where T : class
    {
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            ProtectedReadModelKeys.ValidateWireText(typeof(T), key.Value, key.Value);
        }
        ReadProtectionMeasurement.Current?.DecisionRead<T>(inner, key.Value, streamScope?.EventStreamType, streamScope?.EventStreamId, streamScope?.EventSourceType);
        try
        {
            return await GetForInvocation<T>(key, streamScope, requestedUnit, cancellationToken);
        }
        catch (DecisionReadRefused exception) when (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && exception.Reason == DecisionReadRefusalReason.InvalidKey)
        {
            throw new ProtectedReadModelDoesNotExist(typeof(T), key.Value);
        }
        catch (DecisionReadRefused exception) when (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && exception.Reason == DecisionReadRefusalReason.FoldIncomplete)
        {
            throw new ReadIncomplete(typeof(T), key);
        }
        catch (Exception ex) when (CommandDecisionPolicy.IsProtected && CommandDecisionPolicy.ReadProtection != ReadProtectionMode.On && ex is not (OperationCanceledException or DecisionReadCouldNotBeAcquired))
        {
            throw new DecisionReadCouldNotBeAcquired(ex);
        }
    }

    async Task<DecisionRead<T>> GetForInvocation<T>(ReadModelKey key, StreamScope? streamScope, IUnitOfWork? requestedUnit, CancellationToken cancellationToken)
        where T : class
    {
        var validation = CommandValidationExecution.Current;
        var invocation = InvocationForCurrentPolicy();
        VerifyInvocation(invocation);
        if (!CommandDecisionPolicy.IsActive)
        {
            // Outside a command, preserve Chronicle's ambient unit-of-work contract.
            return requestedUnit is null
                ? await ReadAmbient<T>(key, streamScope, cancellationToken)
                : await ReadEnrolled<T>(requestedUnit, key, streamScope, cancellationToken);
        }

        var mode = CommandDecisionPolicy.Mode == CommandDecisionMode.Unprotected ? ReadMode.Unprotected :
            validation is not null ? ReadMode.Validation : ReadMode.Protected;
        var unitOfWork = UnitOfWorkFor(mode);
        if (unitOfWork is { IsCompleted: true } && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            throw new ReadAfterProtectedCommit(typeof(T).Name);
        }
        if (requestedUnit is not null && unitOfWork is not null && !ReferenceEquals(requestedUnit, unitOfWork))
        {
            throw new ReadProtectionUnitMismatch();
        }

        // Include the target and stream: supplied providers and sibling streams must not share a cached fold.
        var cacheKey = (typeof(T), (string)key, mode, (string)eventStore.Name, (string)eventStore.Namespace, streamScope);
        async Task<object> CreateRead() => mode == ReadMode.Unprotected
            ? await ReadAdvisory<T>(key, streamScope, cancellationToken)
            : mode == ReadMode.Protected && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On
                ? await ReadEnrolled<T>(unitOfWork!, key, streamScope, cancellationToken)
                : await ReadDetached<T>(key, streamScope, cancellationToken);
        var task = CacheFor(invocation!, unitOfWork).Reads.GetOrAdd(
            cacheKey,
            static (_, readFactory) => new Lazy<Task<object>>(readFactory, LazyThreadSafetyMode.ExecutionAndPublication),
            CreateRead);
        var read = (DecisionRead<T>)await task.Value;

        // Use the command's owner, not an ambient Chronicle manager, and re-enroll cached tokens intentionally.
        unitOfWork?.AddDecisionRead(read);
        invocation!.Issued.TryAdd(read, 0);

        return read;
    }

    async Task<T?> ReadLegacy<T>(ReadModelKey key)
        where T : class
    {
        var instance = await readModels.GetInstanceById<T>(key);

        return instance is null ? null : await readModels.Release(instance);
    }
}
