// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a rejection a specification expects.
/// </summary>
/// <param name="Id">The identity of the expected rejection.</param>
/// <param name="Name">The message the rejection is expected to carry, <c>error</c> when the specification states none, or <c>denied</c> for an expected authorization denial.</param>
public record SpecificationError(Guid Id, string Name);
