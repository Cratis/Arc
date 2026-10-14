// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// An event's resolved destination and metadata, retained through transaction enrollment and immediate batching.
/// </summary>
/// <param name="EventSourceId">The destination source.</param>
/// <param name="Event">The returned event.</param>
/// <param name="Tags">The supplied tags.</param>
/// <param name="Occurred">The supplied occurrence time.</param>
/// <param name="Routing">The resolved routing.</param>
/// <param name="NamedTags">The merged named tags.</param>
internal sealed record CommandAppendTarget(EventSourceId EventSourceId, object Event, IEnumerable<string>? Tags, DateTimeOffset? Occurred, EventRouting Routing, IEnumerable<NamedTag> NamedTags);
