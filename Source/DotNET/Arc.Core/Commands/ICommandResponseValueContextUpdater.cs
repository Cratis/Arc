// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a handler that can update the <see cref="CommandContext"/> for a handled response value
/// before any response values are processed.
/// </summary>
public interface ICommandResponseValueContextUpdater : ICommandResponseValueHandler
{
    /// <summary>
    /// Updates the <see cref="CommandContext"/> based on the given value.
    /// </summary>
    /// <param name="commandContext">The command context to update.</param>
    /// <param name="value">The value that should update the context.</param>
    void UpdateContext(CommandContext commandContext, object value);
}
