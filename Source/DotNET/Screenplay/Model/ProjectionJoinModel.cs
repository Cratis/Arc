// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a block joining data from another event onto a read model property.
/// </summary>
/// <param name="Property">The read model property holding the joined data.</param>
/// <param name="EventType">The identifier of the event type joined from.</param>
/// <param name="On">The read model property the join keys on.</param>
/// <param name="Properties">The read model property to event expression map.</param>
/// <remarks>
/// The grammar names the property holding the joined data and the property the join keys on separately. The
/// model-bound form declares both directly - the attribute sits on the property being filled in - while the fluent
/// form has no such property, so analysis names the join after the first property it fills in.
/// </remarks>
public record ProjectionJoinModel(
    string Property,
    string EventType,
    string On,
    IReadOnlyDictionary<string, string> Properties);
