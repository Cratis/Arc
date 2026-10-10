// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Provides named tags for every event returned by a command.
/// </summary>
public interface ICanProvideEventTags
{
    /// <summary>
    /// Gets the command's event tags.
    /// </summary>
    /// <returns>The named tags.</returns>
    IEnumerable<NamedTag> GetEventTags();
}
