// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents a query a slice answers.
/// </summary>
/// <param name="Id">The identity of the query.</param>
/// <param name="Name">The name of the query.</param>
/// <param name="Parameters">The parameters the query takes.</param>
public record QueryItem(Guid Id, string Name, IReadOnlyList<QueryParameter> Parameters);
