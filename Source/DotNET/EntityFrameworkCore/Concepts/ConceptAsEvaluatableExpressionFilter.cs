// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Concepts;
using Microsoft.EntityFrameworkCore.Query;

namespace Cratis.Arc.EntityFrameworkCore.Concepts;

/// <summary>
/// Custom evaluatable expression filter that marks ConceptAs member accesses as evaluatable
/// so EF Core evaluates them to constants before query translation.
/// </summary>
/// <remarks>
/// This filter tells EF Core that expressions accessing ConceptAs members from closures
/// should be evaluated client-side BEFORE the query is translated. This allows ConceptAs
/// types to be automatically unwrapped to their primitive values without manual .Value calls.
/// </remarks>
/// <param name="dependencies">The evaluatable expression filter dependencies.</param>
/// <param name="relationalDependencies">The relational evaluatable expression filter dependencies.</param>
public class ConceptAsEvaluatableExpressionFilter(
    EvaluatableExpressionFilterDependencies dependencies,
    RelationalEvaluatableExpressionFilterDependencies relationalDependencies)
    : RelationalEvaluatableExpressionFilter(dependencies, relationalDependencies)
{
    /// <inheritdoc/>
    public override bool IsEvaluatableExpression(Expression expression, Microsoft.EntityFrameworkCore.Metadata.IModel model)
    {
        // First, check if the base implementation says this is NOT evaluatable
        // If it's not evaluatable for standard reasons, respect that
        var baseResult = base.IsEvaluatableExpression(expression, model);

        // Mark ANY ConceptAs typed expression as evaluatable if it's not an entity property
        // This ensures ConceptAs values from closures are evaluated to their primitive values
        if (expression.Type.IsConcept())
        {
            // Check if this is NOT an entity property access
            // Entity property access looks like: Property(parameterExpression, "PropertyName")
            // or MemberAccess(parameterExpression.Property)
            if (expression is MemberExpression memberExpr)
            {
                // If the member's declaring type is an entity type in the model, don't evaluate it
                // Otherwise (closure variable, local variable, etc.), mark as evaluatable
                var declaringType = memberExpr.Member.DeclaringType;
                if (declaringType != null && model.FindEntityType(declaringType) != null)
                {
                    // This is an entity property, let EF handle it
                    return baseResult;
                }

                // Check if this accesses a lambda parameter (entity) - don't evaluate those
                if (IsLambdaParameterAccess(memberExpr))
                {
                    return baseResult;
                }

                // Store the closure constant so the parameter evaluator can use it
                if (memberExpr.Expression is ConstantExpression closureConstant && closureConstant.Value != null)
                {
                    ClosureConstantCache.Store(closureConstant);
                }

                // Return FALSE to prevent EF Core from creating a parameter with the ConceptAs type!
                // Our interceptor will evaluate this to a primitive constant instead.
                return false;
            }

            // For ConceptAs constants - don't let EF parameterize them
            // Our rewriter will unwrap them to primitives
            if (expression is ConstantExpression)
            {
                return false;
            }

            // For ParameterExpression, check if it's a lambda parameter (entity) or query parameter (closure)
            // Lambda parameters have names like "e", "o", etc. and should not be evaluated
            // Query parameters have names like "id", "sequenceNumber", etc. from closures
            if (expression is ParameterExpression)
            {
                // Lambda parameters (e, o, x, etc.) are part of the query structure - don't evaluate
                // But captured variables that become parameters should be evaluated
                // The problem: at this stage, we can't easily distinguish them
                // So we mark ConceptAs parameters as evaluatable - they should be constants anyway
                return baseResult;
            }
        }

        // For Convert expressions involving ConceptAs, check the operand
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            // Convert FROM ConceptAs TO primitive - DON'T evaluate, let our interceptor handle it
            if (unary.Operand.Type.IsConcept() && !unary.Type.IsConcept())
            {
                // This is converting from ConceptAs to primitive - DON'T evaluate it!
                return false;
            }

            // Convert TO ConceptAs - check if the operand should be evaluated
            if (unary.Type.IsConcept() || unary.Operand.Type.IsConcept())
            {
                return IsEvaluatableExpression(unary.Operand, model);
            }
        }

        return baseResult;
    }

    /// <summary>
    /// Check if the member expression is accessing a property on a lambda parameter.
    /// </summary>
    /// <param name="memberExpr">The member expression to check.</param>
    /// <returns>True if this is a lambda parameter access (entity property), false otherwise.</returns>
    static bool IsLambdaParameterAccess(MemberExpression memberExpr)
    {
        // Check if the member expression accesses a lambda parameter
        // Pattern: param.PropertyName where param is ParameterExpression
        var expression = memberExpr.Expression;

        // Walk through any nested member accesses (e.g., entity.Navigation.Property)
        while (expression is MemberExpression nested)
        {
            expression = nested.Expression;
        }

        // If we end up at a ParameterExpression, this is a lambda parameter access
        return expression is ParameterExpression;
    }
}
