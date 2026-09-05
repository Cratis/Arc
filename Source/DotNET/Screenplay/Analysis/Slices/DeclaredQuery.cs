// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Represents a query together with the type that declared it.
/// </summary>
/// <param name="DeclaringName">The name of the read model or controller declaring the query.</param>
/// <param name="Query">The query itself.</param>
/// <remarks>
/// Two read models in one namespace can both declare a query called the same thing, and the model has nowhere to
/// record which one a query came from. Keeping the declaring type alongside the query until names are resolved is
/// what lets both survive under names that tell them apart.
/// </remarks>
public record DeclaredQuery(string DeclaringName, QueryModel Query);
