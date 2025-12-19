// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Provides access to the command context values.
/// </summary>
public interface ICommandContextValuesProvider
{
    /// <summary>
    /// Gets the command context values.
    /// </summary>
    /// <param name="command">The command instance being executed.</param>
    /// <returns>The command context values.</returns>
    CommandContextValues Provide(object command);
}
