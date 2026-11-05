// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Capabilities;

/// <summary>Adds the current capability caller to command causation for this async frame.</summary>
[Singleton]
internal sealed class CapabilityCausationScope : ICommandOperationExecutionScope
{
    static readonly AsyncLocal<IDisposable?> _scope = new();

    /// <inheritdoc/>
    public bool IsCommitParticipant => false;

    /// <inheritdoc/>
    public CommandCommitDisposition GetCommitDisposition(CommandContext context) => CommandCommitDisposition.NoCommit;

    /// <inheritdoc/>
    public void Begin(CommandContext context)
    {
        if (context.ServiceProvider?.GetService<ICapabilityCallers>()?.Current is not { } caller ||
            context.ServiceProvider.GetService<ICausationManager>() is not { } causationManager)
        {
            // A nested command without a caller or manager must not dispose its outer frame's scope.
            _scope.Value = null;
            return;
        }
        _scope.Value = causationManager.BeginScope(CapabilityCausation.Type, CapabilityCausation.PropertiesFor(caller));
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
