// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The ambient identity of the operation phase that is running. Command operations run inside the command request, so
/// collaborators called from Execute or Compensate use it to name the work they perform.
/// </summary>
/// <param name="operation">The operation type.</param>
/// <param name="index">The operation's position in the batch.</param>
/// <param name="phase">The phase that is running.</param>
public sealed class CommandOperationPhaseScope(Type operation, int index, CommandOperationPhase phase)
{
    static readonly AsyncLocal<CommandOperationPhaseScope?> _current = new();
    int _calls;

    /// <summary>Gets the phase that is running, or null when no command operation is running.</summary>
    public static CommandOperationPhaseScope? Current => _current.Value;

    /// <summary>Gets the operation type.</summary>
    public Type Operation { get; } = operation;

    /// <summary>Gets the operation's position in the batch.</summary>
    public int Index { get; } = index;

    /// <summary>Gets the phase that is running.</summary>
    public CommandOperationPhase Phase { get; } = phase;

    /// <summary>Enters a phase, restoring the previous one when the returned scope is disposed.</summary>
    /// <param name="operation">The operation type.</param>
    /// <param name="index">The operation index.</param>
    /// <param name="phase">The phase.</param>
    /// <returns>The scope to dispose.</returns>
    internal static IDisposable Enter(Type operation, int index, CommandOperationPhase phase)
    {
        var previous = _current.Value;
        _current.Value = new(operation, index, phase);
        return new Restore(previous);
    }

    /// <summary>Takes the next call number within this phase, so repeated calls get distinct identities.</summary>
    /// <returns>The zero-based call number.</returns>
    internal int NextCall() => Interlocked.Increment(ref _calls) - 1;

    /// <summary>Restores the previous phase.</summary>
    /// <param name="previous">The previous phase.</param>
    sealed class Restore(CommandOperationPhaseScope? previous) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose() => _current.Value = previous;
    }
}
