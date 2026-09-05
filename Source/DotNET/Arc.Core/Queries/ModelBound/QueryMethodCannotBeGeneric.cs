// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// The exception that is thrown when a query method still carries unbound type parameters.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="QueryMethodCannotBeGeneric"/> class.
/// </remarks>
/// <param name="method">The method that cannot be used as a query.</param>
public class QueryMethodCannotBeGeneric(MethodInfo method)
    : Exception($"Query method '{method.DeclaringType?.FullName}.{method.Name}' is generic and can therefore never be invoked - a query is invoked with arguments resolved from the request, which leaves nothing to close its type parameters with")
{
    /// <summary>
    /// Gets the method that cannot be used as a query.
    /// </summary>
    public MethodInfo Method { get; } = method;
}
