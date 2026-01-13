// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Concepts;
#if NET10_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Query;
#endif

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
        // The value converters will handle the translation when EF Core processes the query
        // Unwrapping here would create type mismatches with entity properties
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
            // The filter has told EF Core to evaluate closure variables to constants
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

        // Remove casts TO ConceptAs types (e.g., (MissionId)p.MissionId)
        // These casts prevent EF Core from translating the query because it doesn't know
        // how to translate the ConceptAs type. The value converter handles the conversion.
        if (node.NodeType == ExpressionType.Convert && node.Type.IsConcept())
        {
            // Strip the cast and just return the operand
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
        // Visit left and right children first
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // Handle comparisons where one or both sides are ConceptAs types
        if (IsComparisonOperator(node.NodeType))
        {
            var leftIsConcept = left.Type.IsConcept();
            var rightIsConcept = right.Type.IsConcept();

            // If both sides are concepts, transform both to .Value
            // If one side is concept and other is primitive, transform the concept to .Value
            if (leftIsConcept || rightIsConcept)
            {
                // Check if either side is a QueryParameterExpression (EF Core internal type)
                // If so, don't transform anything - keep both sides as ConceptAs
                // The value converter handles entity properties, DbCommandInterceptor handles parameters
                var leftIsQueryParam = IsQueryParameterExpression(left);
                var rightIsQueryParam = IsQueryParameterExpression(right);

                if (leftIsQueryParam || rightIsQueryParam)
                {
                    // Keep expression unchanged - rely on value converters and DbCommandInterceptor
                    if (left != node.Left || right != node.Right)
                    {
                        return Expression.MakeBinary(node.NodeType, left, right);
                    }

                    return node;
                }

                // Check if either side is an entity property access
                // If so, we need to handle type mismatches carefully
                var leftIsEntityProperty = IsEntityPropertyAccess(left);
                var rightIsEntityProperty = IsEntityPropertyAccess(right);

                if (leftIsEntityProperty || rightIsEntityProperty)
                {
                    // If both sides are the same concept type, the value converter handles it
                    if (leftIsConcept && rightIsConcept && left.Type == right.Type)
                    {
                        if (left != node.Left || right != node.Right)
                        {
                            return Expression.MakeBinary(node.NodeType, left, right);
                        }

                        return node;
                    }

                    // If there's a type mismatch (one concept, one primitive), transform the concept to .Value
                    // This ensures the comparison is done at the primitive level
                    var transformedLeft = leftIsConcept && !leftIsEntityProperty ? GetValueAccess(left) : left;
                    var transformedRight = rightIsConcept && !rightIsEntityProperty ? GetValueAccess(right) : right;

                    return Expression.MakeBinary(node.NodeType, transformedLeft, transformedRight);
                }

                // No QueryParameters and no entity properties - transform both to .Value for comparison
                var newLeft = leftIsConcept ? GetValueAccess(left) : left;
                var newRight = rightIsConcept ? GetValueAccess(right) : right;

                // After getting .Value, the types should match
                return Expression.MakeBinary(node.NodeType, newLeft, newRight);
            }
        }

        // If either child changed (e.g., a ConceptAs cast was stripped), we need to rebuild
        // the binary expression WITHOUT the original method, because the original op_Equality
        // method expects the original types (with casts) but after stripping casts the types changed.
        if (left != node.Left || right != node.Right)
        {
            // Check if we have a type mismatch after visiting children
            // This can happen when one side got transformed (e.g., cast stripped) and the other didn't
            var leftIsConcept = left.Type.IsConcept();
            var rightIsConcept = right.Type.IsConcept();

            // For comparison operators, ensure types match by extracting .Value from concepts
            if (IsComparisonOperator(node.NodeType))
            {
                // If one side is a concept and the other is a primitive, or if types don't match, normalize
                if (leftIsConcept || rightIsConcept)
                {
                    // Transform any concept to .Value to ensure type compatibility
                    var newLeft = leftIsConcept ? GetValueAccess(left) : left;
                    var newRight = rightIsConcept ? GetValueAccess(right) : right;
                    return Expression.MakeBinary(node.NodeType, newLeft, newRight);
                }
            }

            // Rebuild binary expression without the original method - let the system figure it out
            // This is necessary because stripping (MissionId)x changes the operand type from MissionId to Guid,
            // but the original expression had op_Equality(MissionId, MissionId)
            return Expression.MakeBinary(node.NodeType, left, right);
        }

        return node;
    }

    static bool IsComparisonOperator(ExpressionType nodeType) =>
        nodeType is ExpressionType.Equal or ExpressionType.NotEqual or
            ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or
            ExpressionType.LessThan or ExpressionType.LessThanOrEqual;

    static Expression GetValueAccess(Expression conceptExpression)
    {
        // For constants, extract the primitive value and return a new constant
        if (conceptExpression is ConstantExpression constant && constant.Value != null)
        {
            var valueProperty = constant.Type.GetProperty("Value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (valueProperty != null)
            {
                var primitiveValue = valueProperty.GetValue(constant.Value);
                return Expression.Constant(primitiveValue, valueProperty.PropertyType);
            }
        }

        // For QueryParameterExpression (EF Core internal type), leave it unchanged
        // The DbCommandInterceptor will unwrap the ConceptAs value at execution time
        // EF Core's value converter will handle the comparison with entity properties
        if (IsQueryParameterExpression(conceptExpression))
        {
            return conceptExpression;
        }

        // For entity property access (MemberExpression on a ParameterExpression), don't add .Value
        // The value converter will handle the translation
        if (IsEntityPropertyAccess(conceptExpression))
        {
            return conceptExpression;
        }

        // For non-constants, access the .Value property
        var valueProp = conceptExpression.Type.GetProperty("Value", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (valueProp != null)
        {
            return Expression.MakeMemberAccess(conceptExpression, valueProp);
        }

        // Fallback - shouldn't happen if IsConcept() is true
        return conceptExpression;
    }

    /// <summary>
    /// Check if the expression is an entity property access (accesses a lambda parameter).
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if this is an entity property access, false otherwise.</returns>
    static bool IsEntityPropertyAccess(Expression expression)
    {
        // Check if the expression accesses a lambda parameter
        // Pattern: param.PropertyName or param.Navigation.PropertyName
        var current = expression;

        // Walk through any nested member accesses (e.g., entity.Navigation.Property)
        while (current is MemberExpression nested)
        {
            current = nested.Expression;
        }

        // If we end up at a ParameterExpression, this is a lambda parameter access (entity property)
        return current is ParameterExpression;
    }

    /// <summary>
    /// Check if the expression is a QueryParameterExpression (EF Core internal type).
    /// Uses the actual type on .NET 10+, falls back to reflection-based type name check for earlier versions.
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if this is a QueryParameterExpression, false otherwise.</returns>
#if NET10_0_OR_GREATER
    static bool IsQueryParameterExpression(Expression expression) =>
        expression is QueryParameterExpression;
#else
    static bool IsQueryParameterExpression(Expression expression) =>
        expression.GetType().Name == "QueryParameterExpression";
#endif
}