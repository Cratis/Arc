// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a link between two module collections.
/// </summary>
/// <param name="Id">The identity of the link.</param>
/// <param name="SourceCollectionId">The collection the link starts at.</param>
/// <param name="TargetCollectionId">The collection the link ends at.</param>
/// <remarks>
/// A generated document never holds one: everything it describes lands in a single collection.
/// </remarks>
public record ModuleCollectionLink(Guid Id, Guid SourceCollectionId, Guid TargetCollectionId);
