// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Concepts;
using Microsoft.EntityFrameworkCore.Query;

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
            return Expression.MakeUnary(node.NodeType, visitedOperand, node.Type, MethodAcceptingOperand(node.Method, visitedOperand));
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
        // If both sides are concepts, transform both to .Value
        // If one side is concept and other is primitive, transform the concept to .Value
        if (IsConceptComparison(node.NodeType, left, right))
        {
            return RewriteConceptComparison(node, left, right);
        }

        return RebuildIfChanged(node, left, right);
    }

    /// <summary>
    /// Keeps the unary method only when it still accepts the visited operand.
    /// </summary>
    /// <param name="method">The original unary method, if any.</param>
    /// <param name="operand">The visited operand.</param>
    /// <returns>The method, or null when its first parameter no longer matches the operand type.</returns>
    static System.Reflection.MethodInfo? MethodAcceptingOperand(System.Reflection.MethodInfo? method, Expression operand)
    {
        var parameters = method?.GetParameters();
        return parameters is { Length: > 0 } && parameters[0].ParameterType != operand.Type ? null : method;
    }

    static bool IsConceptComparison(ExpressionType nodeType, Expression left, Expression right) =>
        IsComparisonOperator(nodeType) && (left.Type.IsConcept() || right.Type.IsConcept());

    /// <summary>
    /// Rewrites a comparison where at least one side is a ConceptAs type.
    /// </summary>
    /// <param name="node">The original expression.</param>
    /// <param name="left">The visited left operand.</param>
    /// <param name="right">The visited right operand.</param>
    /// <returns>The rewritten comparison.</returns>
    static BinaryExpression RewriteConceptComparison(BinaryExpression node, Expression left, Expression right)
    {
        // Check if either side is a QueryParameterExpression (EF Core internal type)
        // If so, don't transform anything - keep both sides as ConceptAs
        // The value converter handles entity properties, DbCommandInterceptor handles parameters
        if (IsQueryParameterExpression(left) || IsQueryParameterExpression(right))
        {
            // Keep expression unchanged - rely on value converters and DbCommandInterceptor
            return RebuildIfChanged(node, left, right);
        }

        // Check if either side is an entity property access
        // If so, we need to handle type mismatches carefully
        if (IsEntityPropertyAccess(left) || IsEntityPropertyAccess(right))
        {
            return RewriteEntityPropertyComparison(node, left, right);
        }

        // No QueryParameters and no entity properties - transform both to .Value for comparison
        // After getting .Value, the types should match
        return Expression.MakeBinary(node.NodeType, ValueOfConcept(left), ValueOfConcept(right));
    }

    /// <summary>
    /// Rewrites a concept comparison where at least one side is an entity property.
    /// </summary>
    /// <param name="node">The original expression.</param>
    /// <param name="left">The visited left operand.</param>
    /// <param name="right">The visited right operand.</param>
    /// <returns>The rewritten comparison.</returns>
    static BinaryExpression RewriteEntityPropertyComparison(BinaryExpression node, Expression left, Expression right)
    {
        // If both sides are the same concept type, the value converter handles it
        if (left.Type.IsConcept() && right.Type.IsConcept() && left.Type == right.Type)
        {
            return RebuildIfChanged(node, left, right);
        }

        // If there's a type mismatch (one concept, one primitive), transform the concept to .Value
        // This ensures the comparison is done at the primitive level
        return Expression.MakeBinary(
            node.NodeType,
            IsEntityPropertyAccess(left) ? left : ValueOfConcept(left),
            IsEntityPropertyAccess(right) ? right : ValueOfConcept(right));
    }

    static Expression ValueOfConcept(Expression expression) =>
        expression.Type.IsConcept() ? GetValueAccess(expression) : expression;

    /// <summary>
    /// If either child changed (e.g., a ConceptAs cast was stripped), rebuild the binary expression WITHOUT the
    /// original method, because the original op_Equality method expects the original types (with casts) but after
    /// stripping casts the types changed. Stripping (MissionId)x changes the operand type from MissionId to Guid,
    /// but the original expression had op_Equality(MissionId, MissionId).
    /// </summary>
    /// <param name="node">The original expression.</param>
    /// <param name="left">The visited left operand.</param>
    /// <param name="right">The visited right operand.</param>
    /// <returns>The original expression when nothing changed; otherwise the rebuilt one.</returns>
    static BinaryExpression RebuildIfChanged(BinaryExpression node, Expression left, Expression right) =>
        left != node.Left || right != node.Right
            ? Expression.MakeBinary(node.NodeType, left, right)
            : node;

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
    /// </summary>
    /// <param name="expression">The expression to check.</param>
    /// <returns>True if this is a QueryParameterExpression, false otherwise.</returns>
    static bool IsQueryParameterExpression(Expression expression) =>
        expression is QueryParameterExpression;
}