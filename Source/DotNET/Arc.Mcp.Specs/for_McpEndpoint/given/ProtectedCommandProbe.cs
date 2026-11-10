// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Tracks handler and filter entry and optionally schedules a competing write after the decision read.
/// </summary>
public class ProtectedCommandProbe : ICommandExecutionScope
{
    /// <summary>Gets the real mutable Arc result observed when the scopes complete.</summary>
    public CommandResult? Result { get; private set; }

    /// <inheritdoc/>
    public void Begin(CommandContext context)
    {
    }

    /// <inheritdoc/>
    public Task Complete(CommandContext context, CommandResult result)
    {
        Result = result;
        return Task.CompletedTask;
    }

    /// <summary>Gets or sets the effective read-protection mode observed by the handler.</summary>
    public ReadProtectionMode ReadProtection { get; set; }

    /// <summary>Gets or sets the handler invocation count.</summary>
    public int Handles { get; set; }

    /// <summary>Gets or sets the filter invocation count.</summary>
    public int Filters { get; set; }

    /// <summary>Gets or sets whether the filter attempts a raw read.</summary>
    public bool ReadInFilter { get; set; }

    /// <summary>Gets or sets whether the raw-read boundary refused the filter.</summary>
    public bool RawReadRefused { get; set; }

    /// <summary>Gets or sets the action run between the decision read and commit.</summary>
    public Func<Task> BeforeReturn { get; set; } = () => Task.CompletedTask;
}
