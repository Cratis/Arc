// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Arc.EntityFrameworkCore.Concepts;

/// <summary>
/// Interceptor that rewrites LINQ expressions to handle ConceptAs types before EF Core translates them to SQL.
/// </summary>
public class ConceptAsQueryExpressionInterceptor : IQueryExpressionInterceptor
{
    /// <inheritdoc/>
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData) =>

        // Evaluate ConceptAs closure variables to constants (keeping ConceptAs types intact).
        // The value converter handles the conversion from ConceptAs to primitive at SQL translation time.
        ConceptAsParameterEvaluator.Evaluate(queryExpression);
}
