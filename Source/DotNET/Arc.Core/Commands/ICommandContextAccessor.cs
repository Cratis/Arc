// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Provides access to the current <see cref="CommandContext"/>.
/// </summary>
public interface ICommandContextAccessor
{
    /// <summary>
    /// Gets the current <see cref="CommandContext"/>.
    /// </summary>
    CommandContext Current { get; }
}
