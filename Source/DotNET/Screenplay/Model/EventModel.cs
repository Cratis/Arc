// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents an immutable fact about something that happened.
/// </summary>
/// <param name="Name">The name of the event.</param>
/// <param name="Properties">The properties carried by the event.</param>
/// <param name="Tags">The tags the event is classified by.</param>
public record EventModel(string Name, IEnumerable<PropertyModel> Properties, IEnumerable<string> Tags);
