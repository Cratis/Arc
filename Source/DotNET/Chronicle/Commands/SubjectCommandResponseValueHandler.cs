// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents a command response value handler that captures an explicit <see cref="Subject"/> from a command response.
/// </summary>
public class SubjectCommandResponseValueHandler : ICommandResponseValueContextUpdater
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is Subject;

    /// <inheritdoc/>
    public void UpdateContext(CommandContext commandContext, object value)
    {
        if (value is Subject subject)
        {
            commandContext.Values[WellKnownCommandContextKeys.Subject] = subject;
        }
    }

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
        Task.FromResult(CommandResult.Success(commandContext.CorrelationId));
}
