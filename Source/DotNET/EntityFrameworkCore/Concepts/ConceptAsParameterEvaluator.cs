// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Concepts;

namespace Cratis.Arc.EntityFrameworkCore.Concepts;

/// <summary>
/// Expression visitor that evaluates ConceptAs closure variables to constants BEFORE EF Core parametrizes them.
/// </summary>
public class ConceptAsParameterEvaluator : ExpressionVisitor
{
    /// <summary>
    /// Evaluate ConceptAs expressions to constants in the expression tree.
    /// </summary>
    /// <param name="expression">The expression to process.</param>
    /// <returns>The expression with ConceptAs values evaluated to constants.</returns>
    public static Expression Evaluate(Expression expression)
    {
        var evaluator = new ConceptAsParameterEvaluator();
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
                        System.Reflection.FieldInfo field => field.GetValue(closureInstance),
                        System.Reflection.PropertyInfo prop => prop.GetValue(closureInstance),
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

        // Check if this is a ConceptAs member access on a closure parameter
        // Pattern: __p_0.id where __p_0 is the closure parameter and id is ConceptAs<T>
        if (node.Type.IsConcept() && visitedExpression is ParameterExpression closureParam)
        {
            // Try to find the constant expression for this closure from the cache
            var closureKey = closureParam.Type.FullName ?? closureParam.Type.Name;
            var closureConstant = ClosureConstantCache.Get(closureKey);

            if (closureConstant != null)
            {
                // Get the closure instance value
                var closureInstance = closureConstant.Value;
                if (closureInstance != null)
                {
                    try
                    {
                        // Get the ConceptAs value from the closure instance
                        var conceptValue = node.Member switch
                        {
                            System.Reflection.FieldInfo field => field.GetValue(closureInstance),
                            System.Reflection.PropertyInfo prop => prop.GetValue(closureInstance),
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
}
