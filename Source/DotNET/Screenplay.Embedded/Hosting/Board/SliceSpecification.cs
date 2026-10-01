// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a specification on a slice.
/// </summary>
/// <param name="Id">The identity of the specification.</param>
/// <param name="Name">The name of the specification.</param>
/// <remarks>
/// A generated document never holds one. Carrying a specification means carrying its given events, its
/// command values and its expected outcomes against the identities assigned here; until that is built,
/// every specification a document declares is reported as a warning rather than dropped in silence.
/// </remarks>
public record SliceSpecification(Guid Id, string Name);
