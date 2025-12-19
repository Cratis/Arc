// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a system that can modify the current <see cref="CommandContext"/>.
/// </summary>
public interface ICommandContextModifier
{
    /// <summary>
    /// Sets the current <see cref="CommandContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="CommandContext"/> to set as current.</param>
    void SetCurrent(CommandContext context);
}
