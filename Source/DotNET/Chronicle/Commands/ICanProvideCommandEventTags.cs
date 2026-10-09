// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Provides application-wide named tags for events returned by commands.
/// </summary>
public interface ICanProvideCommandEventTags
{
    /// <summary>
    /// Gets event tags for a command.
    /// </summary>
    /// <param name="command">The command being executed.</param>
    /// <returns>The named tags.</returns>
    IEnumerable<NamedTag> GetEventTags(object command);
}
