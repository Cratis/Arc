// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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
internal sealed class CommandDecisionReads(IDecisionReads inner, IEventStore eventStore, IReadModels readModels) : IDecisionReads
{
    static readonly AsyncLocal<Invocation?> _invocation = new();
    static readonly ConditionalWeakTable<object, Invocation> _validationInvocations = new();

    enum ReadMode
    {
        Protected = 0,
        Unprotected = 1,
        Validation = 2
    }

    /// <inheritdoc/>
    public DecisionReadAdmission Admit<T>()
        where T : class
    {
        try
        {
            return inner.Admit<T>();
        }
        catch (Exception ex) when (CommandDecisionPolicy.IsProtected && ex is not OperationCanceledException)
        {
            throw new DecisionReadCouldNotBeAcquired(ex);
        }
    }

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        // Only [ProtectedDecision] changes the Chronicle contract. Unmarked and [Unprotected] commands, queries and other
        // callers read detached snapshots exactly as Chronicle's own reader does: an explicitly advisory, unguarded profile.
        if (CommandDecisionPolicy.Mode == CommandDecisionMode.Protected)
        {
            throw new DecisionReadCouldNotBeAcquired(new DetachedDecisionReadRefused());
        }

        return inner.GetDetached<T>(key, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            return await GetForInvocation();
        }
        catch (Exception ex) when (CommandDecisionPolicy.IsProtected && ex is not (OperationCanceledException or DecisionReadCouldNotBeAcquired))
        {
            throw new DecisionReadCouldNotBeAcquired(ex);
        }

        async Task<DecisionRead<T>> GetForInvocation()
        {
            var validation = CommandValidationExecution.Current;
            var invocation = validation is { } validating
                ? _validationInvocations.GetValue(validating.Token, _ => new Invocation(validating.CommandType, CommandDecisionPolicy.Token, null))
                : CurrentInvocation();
            if (!CommandDecisionPolicy.IsActive)
            {
                if (invocation is not null)
                {
                    throw new DecisionReadRequiresProtectionProfile();
                }

                // Outside a command, preserve the Chronicle client contract (including its ambient UOW requirement).
                return await inner.Get<T>(key, cancellationToken);
            }

            if (CommandDecisionPolicy.Mode == CommandDecisionMode.Legacy)
            {
                throw new ProtectedDecisionReadRequiresProtectedCommand();
            }
            if (invocation is null || !ReferenceEquals(invocation.PolicyToken, CommandDecisionPolicy.Token))
            {
                throw new ProtectedDecisionReadRequiresActiveInvocation();
            }

            var mode = CommandDecisionPolicy.Mode == CommandDecisionMode.Unprotected ? ReadMode.Unprotected :
                validation is not null ? ReadMode.Validation : ReadMode.Protected;

            IUnitOfWork? unitOfWork = null;
            if (mode == ReadMode.Protected)
            {
                if (!CommandTransaction.TryGetActive(out unitOfWork))
                {
                    throw new DecisionReadRequiresUnitOfWork();
                }

                if (unitOfWork is not UnitOfWork)
                {
                    throw new ProtectedDecisionRequiresOwnerCapableUnitOfWork();
                }
            }

            // The target is part of the cache key: a supplied provider may resolve the same model and key from another
            // store or namespace. Concurrent resolutions within one invocation share one in-flight fold.
            var cacheKey = (typeof(T), (string)key, mode, (string)eventStore.Name, (string)eventStore.Namespace);
            async Task<object> CreateRead() => mode == ReadMode.Unprotected
                ? DecisionRead<T>.Unprotected(key, await ReadLegacy<T>(key))
                : await inner.GetDetached<T>(key, cancellationToken);
            var task = invocation.Reads.GetOrAdd(
                cacheKey,
                static (_, readFactory) => new Lazy<Task<object>>(readFactory, LazyThreadSafetyMode.ExecutionAndPublication),
                CreateRead);
            var read = (DecisionRead<T>)await task.Value;
            if (mode == ReadMode.Protected)
            {
                // Do not trust Chronicle's ambient UOW here: a supplied provider or nested invocation can carry another
                // ambient manager. Re-enrollment is intentional even when a cached token is returned.
                unitOfWork!.AddDecisionRead(read);
            }

            invocation.Issued.TryAdd(read, 0);
            return read;
        }
    }

    /// <summary>
    /// Starts an invocation with its own cache and provenance.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    internal static void Begin(Type commandType) => _invocation.Value = new Invocation(commandType, CommandDecisionPolicy.Token, CurrentInvocation());

    /// <summary>
    /// Restores the enclosing invocation, if any.
    /// </summary>
    internal static void End()
    {
        var invocation = CurrentInvocation();
        if (invocation is not null)
        {
            invocation.Completed = true;
            _invocation.Value = invocation.Previous;
        }
    }

    /// <summary>
    /// Refuses decision tokens not issued during the current invocation.
    /// </summary>
    /// <param name="value">The value returned by Provide.</param>
    /// <exception cref="DecisionReadNotIssuedForInvocation">The supplied token was issued by another invocation.</exception>
    internal static void VerifyProvided(object value)
    {
        var validation = CommandValidationExecution.Current;
        var invocation = validation is { } validating && _validationInvocations.TryGetValue(validating.Token, out var activeValidation)
            ? activeValidation : CurrentInvocation();
        if (value is IDecisionRead read && (invocation is null ||
            !ReferenceEquals(invocation.PolicyToken, CommandDecisionPolicy.Token) || !invocation.HasIssued(read)))
        {
            throw new DecisionReadNotIssuedForInvocation();
        }
    }

    static Invocation? CurrentInvocation()
    {
        var invocation = _invocation.Value;
        while (invocation?.Completed == true)
        {
            invocation = invocation.Previous;
        }

        return invocation;
    }

    async Task<T?> ReadLegacy<T>(ReadModelKey key)
        where T : class
    {
        var instance = await readModels.GetInstanceById<T>(key);
        return instance is null ? null : await readModels.Release(instance);
    }

    /// <summary>
    /// One command invocation. Every command begins one, but only decision reads use its caches, so they are allocated on first use.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="policyToken">The decision policy token of the invocation.</param>
    /// <param name="previous">The enclosing invocation, if any.</param>
    sealed class Invocation(Type commandType, object? policyToken, Invocation? previous)
    {
        ConcurrentDictionary<(Type Model, string Key, ReadMode Mode, string Store, string Namespace), Lazy<Task<object>>>? _reads;
        ConcurrentDictionary<IDecisionRead, byte>? _issued;

        public Type CommandType { get; } = commandType;
        public object? PolicyToken { get; } = policyToken;
        public Invocation? Previous { get; } = previous;
        public bool Completed { get; set; }
        public ConcurrentDictionary<(Type Model, string Key, ReadMode Mode, string Store, string Namespace), Lazy<Task<object>>> Reads =>
            LazyInitializer.EnsureInitialized(ref _reads, static () => new());
        public ConcurrentDictionary<IDecisionRead, byte> Issued =>
            LazyInitializer.EnsureInitialized(ref _issued, static () => new(ReferenceEqualityComparer.Instance));

        public bool HasIssued(IDecisionRead read) => Volatile.Read(ref _issued)?.ContainsKey(read) == true;
    }
}
