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
        // Evaluate closure variables containing ConceptAs values into ConceptAs constants.
        // The value converters configured in ConceptAsModelCustomizer will handle the actual
        // translation from ConceptAs types to primitives during SQL generation.
        return ConceptAsParameterEvaluator.Evaluate(queryExpression);
    }
}
