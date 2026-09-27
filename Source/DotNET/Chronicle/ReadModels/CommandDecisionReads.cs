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
        where T : class => inner.Admit<T>();

    /// <inheritdoc/>
    public Task<DecisionRead<T>> GetDetached<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        if (CurrentInvocation() is not null && !CommandValidationExecution.IsActive)
        {
            throw new InvalidOperationException("Detached decision reads cannot be used inside an executing command.");
        }

        return inner.GetDetached<T>(key, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DecisionRead<T>> Get<T>(ReadModelKey key, CancellationToken cancellationToken = default)
        where T : class
    {
        var validation = CommandValidationExecution.Current;
        var invocation = validation is { } validating
            ? _validationInvocations.GetValue(validating.Token, _ => new Invocation(validating.CommandType, null))
            : CurrentInvocation();
        if (invocation is null)
        {
            // Outside a command, preserve the Chronicle client contract (including its ambient UOW requirement).
            return await inner.Get<T>(key, cancellationToken);
        }

        var mode = invocation.CommandType.IsDefined(typeof(UnprotectedAttribute), true) ? ReadMode.Unprotected :
            validation is not null ? ReadMode.Validation : ReadMode.Protected;

        IUnitOfWork? unitOfWork = null;
        if (mode == ReadMode.Protected)
        {
            if (!CommandTransaction.TryGetActive(out unitOfWork)) throw new DecisionReadRequiresUnitOfWork();
            if (unitOfWork is not UnitOfWork)
            {
                throw new InvalidOperationException("Protected command decisions require Chronicle's owner-capable UnitOfWork.");
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

    /// <summary>Starts an invocation with its own cache and provenance.</summary>
    /// <param name="commandType">The command type.</param>
    internal static void Begin(Type commandType) => _invocation.Value = new Invocation(commandType, CurrentInvocation());

    /// <summary>Restores the enclosing invocation, if any.</summary>
    internal static void End()
    {
        var invocation = CurrentInvocation();
        if (invocation is not null)
        {
            invocation.Completed = true;
            _invocation.Value = invocation.Previous;
        }
    }

    /// <summary>Refuses decision tokens not issued during the current invocation.</summary>
    /// <param name="value">The value returned by Provide.</param>
    /// <exception cref="InvalidOperationException">The supplied token was issued by another invocation.</exception>
    internal static void VerifyProvided(object value)
    {
        if (value is IDecisionRead read && (CurrentInvocation() is not { } invocation || !invocation.Issued.ContainsKey(read)))
        {
            throw new InvalidOperationException("A DecisionRead returned by Provide must be issued for this command invocation.");
        }
    }

    static Invocation? CurrentInvocation()
    {
        var invocation = _invocation.Value;
        while (invocation?.Completed == true) invocation = invocation.Previous;
        return invocation;
    }

    async Task<T?> ReadLegacy<T>(ReadModelKey key)
        where T : class
    {
        var instance = await readModels.GetInstanceById<T>(key);
        return instance is null ? null : await readModels.Release(instance);
    }

    sealed class Invocation(Type commandType, Invocation? previous)
    {
        public Type CommandType { get; } = commandType;
        public Invocation? Previous { get; } = previous;
        public bool Completed { get; set; }
        public ConcurrentDictionary<(Type Model, string Key, ReadMode Mode, string Store, string Namespace), Lazy<Task<object>>> Reads { get; } = new();
        public ConcurrentDictionary<IDecisionRead, byte> Issued { get; } = new(ReferenceEqualityComparer.Instance);
    }
}
