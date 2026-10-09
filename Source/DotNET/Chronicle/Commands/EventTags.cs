// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Named tags returned from a command handler, applied to all its returned events.
/// </summary>
/// <param name="Tags">The named tags.</param>
public record EventTags(IEnumerable<NamedTag> Tags)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventTags"/> record.
    /// </summary>
    /// <param name="tags">The named tags.</param>
    public EventTags(params NamedTag[] tags) : this((IEnumerable<NamedTag>)tags)
    {
    }
}
