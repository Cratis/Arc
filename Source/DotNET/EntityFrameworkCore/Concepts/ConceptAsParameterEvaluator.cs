// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Cratis.Concepts;
#if NET10_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Query;
#endif

namespace Cratis.Arc.EntityFrameworkCore.Concepts;

/// <summary>
/// Expression visitor that evaluates ConceptAs closure variables to constants BEFORE EF Core parametrizes them.
/// </summary>
public class ConceptAsParameterEvaluator : ExpressionVisitor
{
    readonly Dictionary<Expression, object?> _queryParameterValues = [];

    /// <summary>
    /// Evaluate ConceptAs expressions to constants in the expression tree.
    /// </summary>
    /// <param name="expression">The expression to process.</param>
    /// <returns>The expression with ConceptAs values evaluated to constants.</returns>
    public static Expression Evaluate(Expression expression)
    {
        var evaluator = new ConceptAsParameterEvaluator();

        // First pass: collect QueryParameterExpression values from the tree
        evaluator.CollectQueryParameterValues(expression);

        // Second pass: evaluate with the collected values
        return evaluator.Visit(expression);
    }

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Visit children to evaluate any closure variables
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // If either operand changed, we need to rebuild
        // For comparison operators, use null for method to avoid type mismatch with op_Equality
        if (left != node.Left || right != node.Right)
        {
            if (node.NodeType == ExpressionType.Equal ||
                node.NodeType == ExpressionType.NotEqual ||
                node.NodeType == ExpressionType.GreaterThan ||
                node.NodeType == ExpressionType.GreaterThanOrEqual ||
                node.NodeType == ExpressionType.LessThan ||
                node.NodeType == ExpressionType.LessThanOrEqual)
            {
                // Use null for method to let Expression.MakeBinary infer the operator
                return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, null);
            }

            // For other operators, preserve the original method
            return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method, node.Conversion);
        }

        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        // First, visit the expression to handle nested member accesses
        var visitedExpression = Visit(node.Expression);

        // Check if this is a ConceptAs member access on a closure (constant expression)
        // Pattern: closure.conceptField where conceptField is ConceptAs<T>
        if (node.Type.IsConcept() && visitedExpression is ConstantExpression constantExpr)
        {
            try
            {
                // Extract the ConceptAs instance from the closure
                var closureInstance = constantExpr.Value;
                if (closureInstance != null)
                {
                    var conceptValue = node.Member switch
                    {
                        FieldInfo field => field.GetValue(closureInstance),
                        PropertyInfo prop => prop.GetValue(closureInstance),
                        _ => null
                    };

                    if (conceptValue != null)
                    {
                        // Return a constant expression with the ConceptAs value
                        // The ConceptAsExpressionRewriter will extract the primitive when needed
                        return Expression.Constant(conceptValue, node.Type);
                    }
                }
            }
            catch
            {
                // Failed to evaluate - fall through to base implementation
            }
        }

        // Check if this is a ConceptAs member access on a QueryParameterExpression (method parameter closure)
        // Pattern: @p.missionId where @p is QueryParameterExpression for the display class
        // In this case, we can't evaluate the closure at this point because QueryParameterExpression
        // is resolved at runtime. Leave it as-is - the ConceptAsExpressionRewriter will handle
        // transforming the comparison to use .Value on both sides.
        if (node.Type.IsConcept() && IsQueryParameterExpression(visitedExpression))
        {
            // Return the member access unchanged - the rewriter will handle .Value access
            return Expression.MakeMemberAccess(visitedExpression, node.Member);
        }

        // Check if this is a ConceptAs member access on a regular closure parameter
        // Pattern: __p_0.id where __p_0 is the closure parameter and id is ConceptAs<T>
        if (node.Type.IsConcept() && visitedExpression is ParameterExpression closureParam)
        {
            // Fall back to the static cache (from ConceptAsEvaluatableExpressionFilter)
            var closureKey = closureParam.Type.FullName ?? closureParam.Type.Name;
            var cachedClosureConstant = ClosureConstantCache.Get(closureKey);

            if (cachedClosureConstant != null)
            {
                // Get the closure instance value
                var closureInstance = cachedClosureConstant.Value;
                if (closureInstance != null)
                {
                    try
                    {
                        // Get the ConceptAs value from the closure instance
                        var conceptValue = node.Member switch
                        {
                            FieldInfo field => field.GetValue(closureInstance),
                            PropertyInfo prop => prop.GetValue(closureInstance),
                            _ => null
                        };

                        if (conceptValue != null)
                        {
                            // Return a constant expression with the ConceptAs value
                            // The ConceptAsExpressionRewriter will extract the primitive when needed
                            return Expression.Constant(conceptValue, node.Type);
                        }
                    }
                    catch
                    {
                        // Failed to extract value - fall through to base implementation
                    }
                }
            }
        }

        // If the expression changed, rebuild the member access
        if (visitedExpression != node.Expression)
        {
            return Expression.MakeMemberAccess(visitedExpression, node.Member);
        }

        return base.VisitMember(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        // Handle Convert expressions - if we're converting FROM primitive TO ConceptAs,
        // and the operand is now a constant (because we evaluated it), we can skip the convert
        var visitedOperand = Visit(node.Operand);

        if (node.NodeType == ExpressionType.Convert &&
            node.Type.IsConcept() &&
            visitedOperand is ConstantExpression)
        {
            // The operand is now a constant primitive value, don't convert it back to ConceptAs
            return visitedOperand;
        }

        if (visitedOperand != node.Operand)
        {
            // Clear the method if operand changed - the original method might expect different types
            return Expression.MakeUnary(node.NodeType, visitedOperand, node.Type, null);
        }

        return base.VisitUnary(node);
    }

    /// <summary>
    /// Check if the expression is a QueryParameterExpression (EF Core internal type).
    /// Uses the actual type on .NET 10+, falls back to reflection-based type name check for earlier versions.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if this is a QueryParameterExpression, false otherwise.</returns>
#if NET10_0_OR_GREATER
    static bool IsQueryParameterExpression(Expression? expression) =>
        expression is QueryParameterExpression;
#else
    static bool IsQueryParameterExpression(Expression? expression) =>
        expression?.GetType().Name == "QueryParameterExpression";
#endif

    void CollectQueryParameterValues(Expression expression)
    {
        var collector = new QueryParameterValueCollector(_queryParameterValues);
        collector.Visit(expression);
    }

    sealed class QueryParameterValueCollector(Dictionary<Expression, object?> values) : ExpressionVisitor
    {
        protected override Expression VisitExtension(Expression node)
        {
            // QueryParameterExpression stores a reference to the closure parameter
            // We need to find and evaluate it to get the actual closure instance
            if (IsQueryParameterExpression(node))
            {
                // Store the QueryParameterExpression itself as a key - we'll look it up during evaluation
                // The QueryParameterExpression has the closure type info
                values[node] = null; // Mark that we've seen this parameter
            }

            return base.VisitExtension(node);
        }
    }
}
