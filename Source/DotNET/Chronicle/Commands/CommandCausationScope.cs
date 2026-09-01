// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an <see cref="ICommandExecutionScope"/> that records which command is executing on the causation
/// chain, so every event the command appends carries the command that produced it.
/// </summary>
/// <remarks>
/// <para>
/// The chain already says how the work arrived - an HTTP request, a reactor - but not what was asked for. Without
/// this link an appended event can be traced back to a request path and no further, and a command executed from
/// another command is indistinguishable from one executed directly.
/// </para>
/// <para>
/// The causation is added in <see cref="Begin"/>, which the pipeline calls synchronously immediately before the
/// command's filters and handler run, so it is on the chain for every append the command makes - the events it
/// returns, the ones an aggregate root commits, and any immediate append it performs. A nested command adds its own
/// link on top of the outer command's, which is what makes "the command one level up" answerable.
/// </para>
/// <para>
/// The link lasts exactly as long as the command. Two commands run one after the other - a reactor executing both,
/// a job looping - are siblings, and leaving the first link on the chain would make the second read as caused by
/// the first: an ordering nothing established, which anything mining the chain would learn as a fact.
/// </para>
/// </remarks>
[Singleton]
public class CommandCausationScope : ICommandExecutionScope
{
    /// <summary>
    /// The causation scope owned by the command executing on this asynchronous flow.
    /// </summary>
    /// <remarks>
    /// Ambient rather than keyed on the context: the pipeline hands <see cref="Complete"/> a context that has been
    /// copied since <see cref="Begin"/> saw it - dependencies and response are set on it as the command runs - so
    /// the two calls never see equal values to key on. A nested command establishes its own value for the duration
    /// of its own execution and leaves this frame's untouched, which is exactly the nesting the chain describes.
    /// </remarks>
    static readonly AsyncLocal<IDisposable?> _scope = new();

    /// <inheritdoc/>
    public void Begin(CommandContext context)
    {
        if (context.ServiceProvider?.GetService<ICausationManager>() is not { } causationManager)
        {
            // Nothing to record through, and this frame must not inherit a scope from an outer one - disposing
            // another command's scope would take its causation off the chain while it is still running.
            _scope.Value = null;
            return;
        }

        _scope.Value = causationManager.BeginScope(CommandCausation.Type, CommandCausation.PropertiesFor(context.Type, context.Command));
    }

    /// <inheritdoc/>
    public Task Complete(CommandContext context, CommandResult result)
    {
        var scope = _scope.Value;
        _scope.Value = null;
        scope?.Dispose();
        return Task.CompletedTask;
    }
}
