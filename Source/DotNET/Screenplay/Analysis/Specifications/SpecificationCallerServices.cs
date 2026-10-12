// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Refuses extra service-collection uses that could remove, replace, or hide the caller registration.
/// </summary>
/// <param name="models">The models resolving service-property accesses.</param>
internal sealed class SpecificationCallerServices(SemanticModels models)
{
    /// <summary>
    /// Checks for Services access outside the one supported instance registration.
    /// </summary>
    /// <param name="type">The scenario type and its inherited setup.</param>
    /// <param name="registration">The supported registration call.</param>
    /// <returns>The failure reason, or null when the registration is the only use.</returns>
    internal string? Check(INamedTypeSymbol type, SpecificationCallerCall registration)
    {
        var accesses = SpecificationMembers.ChainOf(type).SelectMany(member => member.DeclaringSyntaxReferences)
            .Select(reference => reference.GetSyntax()).SelectMany(node => node.DescendantNodes().OfType<ExpressionSyntax>())
            .Where(IsMemberAccess);
        return accesses.Any(access => IsOtherUse(access, registration))
            ? "the command scenario's services are used outside the supported caller registration and may change its caller" : null;
    }

    /// <summary>
    /// Recognizes ordinary and conditional property access syntax.
    /// </summary>
    /// <param name="expression">The source expression.</param>
    /// <returns>Whether the expression can be a service-property access.</returns>
    static bool IsMemberAccess(ExpressionSyntax expression) => expression is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax;

    /// <summary>
    /// Determines whether an expression belongs to the proven registration.
    /// </summary>
    /// <param name="access">The property access.</param>
    /// <param name="registration">The supported registration call.</param>
    /// <returns>Whether the registration contains the access.</returns>
    static bool IsSupportedAccess(ExpressionSyntax access, SpecificationCallerCall registration) =>
        access.SyntaxTree == registration.Syntax.SyntaxTree && registration.Syntax.Span.Contains(access.Span);

    /// <summary>
    /// Recognizes the command scenario's actual Services property by metadata identity.
    /// </summary>
    /// <param name="property">The resolved property.</param>
    /// <returns>Whether this is a command-scenario service collection.</returns>
    static bool IsServices(IPropertySymbol property) =>
        property.Name == "Services" && property.ContainingType.Is(WellKnownTypeNames.CommandScenario);

    /// <summary>
    /// Determines whether an extra command-scenario Services access exists.
    /// </summary>
    /// <param name="access">The possible property access.</param>
    /// <param name="registration">The supported registration call.</param>
    /// <returns>Whether the access prevents proving an unchanged caller registration.</returns>
    bool IsOtherUse(ExpressionSyntax access, SpecificationCallerCall registration)
    {
        if (IsSupportedAccess(access, registration))
        {
            return false;
        }

        var property = models.For(access.SyntaxTree)?.GetSymbolInfo(access).Symbol as IPropertySymbol;
        return property is not null && IsServices(property);
    }
}
