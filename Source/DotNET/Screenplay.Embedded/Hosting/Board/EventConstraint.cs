// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a single uniqueness constraint declared for an event.
/// </summary>
/// <param name="Name">The name of the constraint - the property it is declared on, for a unique property.</param>
/// <param name="Message">The message stated when the constraint is broken.</param>
public record EventConstraint(string Name, string Message);
