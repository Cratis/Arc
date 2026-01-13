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
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        // Two-step process:
        // 1. Evaluate closure variables containing ConceptAs values into ConceptAs constants
        var evaluated = ConceptAsParameterEvaluator.Evaluate(queryExpression);

        // 2. Rewrite the expression to remove ConceptAs casts that EF Core can't translate
        return ConceptAsExpressionRewriter.Rewrite(evaluated);
    }
}
