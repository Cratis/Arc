// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.Commands;

/// <summary>
/// An immutable, ordered batch of command operation declarations. Default and an empty collection expression are empty.
/// Membership is copied immediately; descriptors should themselves be immutable business data.
/// </summary>
[CollectionBuilder(typeof(CommandOperations), nameof(Create))]
public readonly struct CommandOperations : IReadOnlyList<ICommandOperation>, IEquatable<CommandOperations>
{
    readonly ICommandOperation[]? _operations;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOperations"/> struct by materializing a sequence once.
    /// </summary>
    /// <param name="operations">The declarations in execution order.</param>
    /// <exception cref="InvalidCommandOperation">A declaration is null.</exception>
    public CommandOperations(IEnumerable<ICommandOperation> operations)
    {
        _operations = operations.ToArray();
        if (_operations.Any(operation => operation is null))
        {
            throw new InvalidCommandOperation("CommandOperations cannot contain null declarations.");
        }
    }

    /// <inheritdoc/>
    public int Count => _operations?.Length ?? 0;

    /// <inheritdoc/>
    public ICommandOperation this[int index] => (_operations ?? [])[index];

    /// <summary>
    /// Compares ordered declaration membership.
    /// </summary>
    /// <param name="left">The first batch.</param>
    /// <param name="right">The second batch.</param>
    /// <returns>Whether membership is equal.</returns>
    public static bool operator ==(CommandOperations left, CommandOperations right) => left.Equals(right);

    /// <summary>
    /// Compares ordered declaration membership for inequality.
    /// </summary>
    /// <param name="left">The first batch.</param>
    /// <param name="right">The second batch.</param>
    /// <returns>Whether membership differs.</returns>
    public static bool operator !=(CommandOperations left, CommandOperations right) => !left.Equals(right);

    /// <summary>
    /// Creates an immutable batch from a collection expression.
    /// </summary>
    /// <param name="operations">The declarations to copy.</param>
    /// <returns>The ordered batch.</returns>
    public static CommandOperations Create(ReadOnlySpan<ICommandOperation> operations) => new(operations.ToArray());

    /// <inheritdoc/>
    public bool Equals(CommandOperations other) => this.SequenceEqual(other);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CommandOperations other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var operation in this)
        {
            hash.Add(operation);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public IEnumerator<ICommandOperation> GetEnumerator() => ((IEnumerable<ICommandOperation>)(_operations ?? [])).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
