// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents the uniqueness constraints declared for an event.
/// </summary>
/// <param name="Unique">The constraint stating a property of the event is unique, when one is declared.</param>
/// <param name="UniqueEventType">The constraint stating the event occurs once, when one is declared.</param>
public record EventConstraints(EventConstraint? Unique, EventConstraint? UniqueEventType);
