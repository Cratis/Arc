// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Keeps the invocation identity through context copies that only add dependencies or authorization state.
/// </summary>
internal sealed class AvailabilityInvocationIdentity : IEquatable<AvailabilityInvocationIdentity>
{
    readonly Lock _lock = new();
    readonly List<Entry> _invocations = [];
    AvailabilityInvocation? _current;

    /// <summary>Gets the most recently selected invocation, if one has been captured.</summary>
    internal AvailabilityInvocation? Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>A transient cache is not part of the containing record's value equality.</remarks>
    public bool Equals(AvailabilityInvocationIdentity? other) => other is not null;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AvailabilityInvocationIdentity;

    /// <inheritdoc/>
    public override int GetHashCode() => 0;

    /// <summary>Gets the identity shared by readers of the same bound invocation.</summary>
    /// <param name="part">The operation identity selected by the pipeline.</param>
    /// <param name="arguments">The bound command instance or query arguments.</param>
    /// <param name="receivedAt">The pinned operation receipt.</param>
    /// <returns>The invocation identity, isolated from other operations and arguments.</returns>
    internal AvailabilityInvocation For(Type part, object? arguments, DateTimeOffset receivedAt)
    {
        lock (_lock)
        {
            var operation = OperationContextScope.Identity;
            _current = _invocations.Find(entry => ReferenceEquals(entry.Operation, operation) && Matches(entry.Invocation, part, arguments, receivedAt))?.Invocation;
            if (_current is null)
            {
                _current = new(part, arguments, receivedAt);
                _invocations.Add(new(operation, _current));
            }

            return _current;
        }
    }

    static bool Matches(AvailabilityInvocation current, Type part, object? arguments, DateTimeOffset receivedAt) =>
        current.Part == part && ReferenceEquals(current.Arguments, arguments) && current.ReceivedAt == receivedAt;

    sealed record Entry(object? Operation, AvailabilityInvocation Invocation);
}
