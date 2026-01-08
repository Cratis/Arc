// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Concepts;

namespace Cratis.Arc.EntityFrameworkCore.Concepts;

/// <summary>
/// Query expression visitor that rewrites ConceptAs property accesses and comparisons for EF Core query translation.
/// </summary>
/// <remarks>
/// This rewriter allows EF Core to translate queries that use ConceptAs types directly without
/// needing to manually extract the .Value property. It works by rewriting the expression tree
/// before EF Core processes it by:
/// - Detecting when a member access returns a ConceptAs type and converting it to the underlying primitive value type.
/// - Unwrapping explicit casts to ConceptAs types in binary comparisons (e.g., (ObserverId)stringValue == id) to keep comparisons at the primitive level.
/// - Preserving conversions in Select projections to maintain proper return types.
/// </remarks>
public class ConceptAsExpressionRewriter : ExpressionVisitor
{
    /// <summary>
    /// Rewrite an expression to handle ConceptAs types.
    /// </summary>
    /// <param name="expression">The expression to rewrite.</param>
    /// <returns>The rewritten expression.</returns>
    public static Expression Rewrite(Expression expression)
    {
        var rewriter = new ConceptAsExpressionRewriter();
        return rewriter.Visit(expression);
    }

    /// <inheritdoc/>
    protected override Expression VisitConstant(ConstantExpression node)
    {
        // DO NOT unwrap ConceptAs constants here
        // Let VisitBinary handle extracting .Value when comparing two ConceptAs expressions
        // This preserves the type match needed for binary operations
        return base.VisitConstant(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitParameter(ParameterExpression node)
    {
        // DO NOT transform parameters - leave them as-is
        // The DbCommandInterceptor will unwrap ConceptAs parameter values at execution time
        // Trying to access .Value here creates untranslatable expressions like __p_0.Value
        return base.VisitParameter(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        // Check if the member itself is a Concept type BEFORE visiting children
        // This is important because we need to handle the entire concept expression as one unit
        if (node.Type.IsConcept())
        {
            // For ConceptAs members, just visit children without any special processing
            // The ParameterEvaluator has already handled closures
            // Entity properties will be handled by value converters
            var visitedExpression = Visit(node.Expression);
            
            if (visitedExpression != node.Expression)
            {
                return Expression.MakeMemberAccess(visitedExpression, node.Member);
            }
            
            return node;
        }

        // For non-Concept members, visit the expression that this member belongs to
        var visited = Visit(node.Expression);

        // Rebuild the member access if the expression changed
        if (visited != node.Expression)
        {
            return Expression.MakeMemberAccess(visited, node.Member);
        }

        return base.VisitMember(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        // Remove redundant Convert expressions (e.g., Convert(ulong, ulong))
        // This happens when EF Core evaluates ConceptAs parameters - it extracts the primitive
        // value but wraps it in a Convert expression that SQLite can't translate
        if (node.NodeType == ExpressionType.Convert && node.Operand.Type == node.Type)
        {
            // Same type conversion - just return the operand
            return Visit(node.Operand);
        }

        // Visit the operand to handle any nested expressions
        var visitedOperand = Visit(node.Operand);

        if (visitedOperand != node.Operand)
        {
            return Expression.MakeUnary(node.NodeType, visitedOperand, node.Type, node.Method);
        }

        return base.VisitUnary(node);
    }

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Check if this is a comparison operation
        var isComparison = node.NodeType == ExpressionType.Equal ||
                          node.NodeType == ExpressionType.NotEqual ||
                          node.NodeType == ExpressionType.GreaterThan ||
                          node.NodeType == ExpressionType.GreaterThanOrEqual ||
                          node.NodeType == ExpressionType.LessThan ||
                          node.NodeType == ExpressionType.LessThanOrEqual;

        if (isComparison)
        {
            // Visit children first
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            // Check if we need to handle ConceptAs types
            var leftIsConcept = left.Type.IsConcept();
            var rightIsConcept = right.Type.IsConcept();

            // Handle ConceptAs in binary comparisons
            if (leftIsConcept || rightIsConcept)
            {
                // If right is a ConceptAs constant, extract its primitive value
                if (rightIsConcept && right is ConstantExpression rightConst && rightConst.Value != null)
                {
                    var valueProperty = rightConst.Value.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var primitiveValue = valueProperty.GetValue(rightConst.Value);
                        var valueType = rightConst.Type.GetConceptValueType();
                        right = Expression.Constant(primitiveValue, valueType);
                        rightIsConcept = false; // It's now a primitive
                    }
                }

                // If left is a ConceptAs constant, extract its primitive value
                if (leftIsConcept && left is ConstantExpression leftConst && leftConst.Value != null)
                {
                    var valueProperty = leftConst.Value.GetType().GetProperty("Value");
                    if (valueProperty != null)
                    {
                        var primitiveValue = valueProperty.GetValue(leftConst.Value);
                        var valueType = leftConst.Type.GetConceptValueType();
                        left = Expression.Constant(primitiveValue, valueType);
                        leftIsConcept = false; // It's now a primitive
                    }
                }

                // After extracting primitives from constants, if one side is still ConceptAs (it's a property),
                // add .Value to it so both sides are primitives
                if (leftIsConcept && left is not ConstantExpression)
                {
                    left = Expression.MakeMemberAccess(left, left.Type.GetProperty("Value")!);
                }

                if (rightIsConcept && right is not ConstantExpression)
                {
                    right = Expression.MakeMemberAccess(right, right.Type.GetProperty("Value")!);
                }

                return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, null);
            }

            // If anything changed, rebuild
            if (left != node.Left || right != node.Right)
            {
                return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, null);
            }

            return node;
        }

        // For non-comparison operations, use default behavior
        return base.VisitBinary(node);
    }
}
