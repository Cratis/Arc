// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Manages the current <see cref="CommandContext"/> in an async local manner.
/// </summary>
public class CommandContextManager : ICommandContextModifier, ICommandContextAccessor
{
    static readonly AsyncLocal<CommandContext?> _current = new();

    /// <inheritdoc/>
    public CommandContext Current => _current.Value ?? throw new InvalidOperationException("No command context is available in the current scope.");

    /// <inheritdoc/>
    public void SetCurrent(CommandContext context) => _current.Value = context;
}
