// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels;

internal sealed partial class CommandDecisionReads
{
    static readonly AsyncLocal<Invocation?> _invocation = new();
    static readonly ConditionalWeakTable<object, Invocation> _validationInvocations = new();
    static readonly ConditionalWeakTable<IUnitOfWork, ReadCache> _unitReads = new();

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

    static Invocation? InvocationForCurrentPolicy() => CommandValidationExecution.Current is { } validating
        ? _validationInvocations.GetValue(validating.Token, _ => new Invocation(validating.CommandType, CommandDecisionPolicy.Token, null))
        : CurrentInvocation();

    static ReadCache CacheFor(Invocation invocation, IUnitOfWork? unit) =>
        CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && unit is not null
            ? _unitReads.GetValue(unit, static _ => new ReadCache())
            : invocation.Cache;

    static Invocation? CurrentInvocation()
    {
        var invocation = _invocation.Value;
        while (invocation?.Completed == true)
        {
            invocation = invocation.Previous;
        }

        return invocation;
    }

    /// <summary>
    /// One command invocation. Caches and provenance are allocated on first use.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="policyToken">The decision policy token of the invocation.</param>
    /// <param name="previous">The enclosing invocation, if any.</param>
    sealed class Invocation(Type commandType, object? policyToken, Invocation? previous)
    {
        ConcurrentDictionary<IDecisionRead, byte>? _issued;

        public ReadCache Cache { get; } = new();

        public Type CommandType { get; } = commandType;
        public object? PolicyToken { get; } = policyToken;
        public Invocation? Previous { get; } = previous;
        public bool Completed { get; set; }
        public ConcurrentDictionary<IDecisionRead, byte> Issued =>
            LazyInitializer.EnsureInitialized(ref _issued, static () => new(ReferenceEqualityComparer.Instance));

        public bool HasIssued(IDecisionRead read) => Volatile.Read(ref _issued)?.ContainsKey(read) == true;
    }

    sealed class ReadCache
    {
        public ConcurrentDictionary<(Type Model, string Key, ReadMode Mode, string Store, string Namespace, StreamScope? Stream), Lazy<Task<object>>> Reads { get; } = new();
        public ConcurrentDictionary<IDecisionRead, Lazy<Task<object?>>> Instances { get; } = new(ReferenceEqualityComparer.Instance);
    }
}
