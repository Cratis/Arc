// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a system that materializes the model representing a query's argument set as a whole.
/// </summary>
/// <remarks>
/// A query's arguments are flat, and a validator can be declared against that flat shape by declaring a
/// <c>QueryValidator&lt;T&gt;</c> for a type whose properties mirror the query's parameters. The generated TypeScript
/// proxy validates its arguments object against exactly that shape, so without a server-side counterpart such a
/// validator would be enforced in the browser only — which is to say not enforced at all, since anything can call the
/// endpoint directly. This is that counterpart.
/// </remarks>
public interface IQueryArgumentsModels
{
    /// <summary>
    /// Tries to materialize the arguments model for a query from the arguments it was called with.
    /// </summary>
    /// <param name="performer">The <see cref="IQueryPerformer"/> for the query.</param>
    /// <param name="arguments">The <see cref="QueryArguments"/> the query was called with.</param>
    /// <param name="model">The materialized model when the query has one.</param>
    /// <returns>True when the query has an arguments model and it could be materialized; otherwise false.</returns>
    bool TryCreateFor(IQueryPerformer performer, QueryArguments arguments, [MaybeNullWhen(false)] out object model);
}
