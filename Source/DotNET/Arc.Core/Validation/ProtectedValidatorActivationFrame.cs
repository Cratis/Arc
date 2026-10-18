// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Validation;

/// <summary>
/// Owns freshly constructed validator graphs until model validation finishes, before transaction completion.
/// </summary>
internal sealed class ProtectedValidatorActivationFrame : IAsyncDisposable
{
    static readonly AsyncLocal<ProtectedValidatorActivationFrame?> _current = new();
    readonly List<object> _owned = [];
    readonly ProtectedValidatorActivationFrame? _previous;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtectedValidatorActivationFrame"/> class.
    /// </summary>
    internal ProtectedValidatorActivationFrame()
    {
        _previous = Current;
        Token = CommandDecisionPolicy.Token;
        _current.Value = this;
    }

    /// <summary>
    /// Gets the active model-validation frame.
    /// </summary>
    internal static ProtectedValidatorActivationFrame? Current
    {
        get
        {
            var frame = _current.Value;
            while (frame?.Completed == true)
            {
                frame = frame._previous;
            }
            return frame;
        }
    }

    /// <summary>
    /// Gets the command invocation identity this frame belongs to.
    /// </summary>
    internal object? Token { get; }

    /// <summary>
    /// Gets whether this frame has finished, including failed cleanup.
    /// </summary>
    internal bool Completed { get; private set; }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Completed)
        {
            return;
        }
        Completed = true;
        _current.Value = _previous;
        var failures = new List<Exception>();
        for (var index = _owned.Count - 1; index >= 0; index--)
        {
            try
            {
                await DisposeOwned(_owned[index]);
            }
            catch (Exception failure)
            {
                failures.Add(failure);
            }
        }
        _owned.Clear();
        if (failures.Count > 0)
        {
            throw DiscoverableValidatorRefusedInProtectedDecision.CleanupFailed(failures);
        }
    }

    /// <summary>
    /// Disposes a command-owned object, preferring asynchronous cleanup.
    /// </summary>
    /// <param name="value">The object to release.</param>
    /// <returns>The asynchronous cleanup.</returns>
    internal static ValueTask DisposeOwned(object value) => value switch
    {
        IAsyncDisposable asynchronous => asynchronous.DisposeAsync(),
        IDisposable synchronous => DisposeSynchronously(synchronous),
        _ => ValueTask.CompletedTask
    };

    /// <summary>
    /// Tracks a successfully constructed object before any callback can fail.
    /// </summary>
    /// <param name="value">The freshly constructed object, never a DI-owned read leaf.</param>
    internal void Own(object value) => _owned.Add(value);

    static ValueTask DisposeSynchronously(IDisposable value)
    {
        value.Dispose();
        return ValueTask.CompletedTask;
    }
}
