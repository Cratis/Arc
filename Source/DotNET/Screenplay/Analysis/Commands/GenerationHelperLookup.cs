// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Finds the same-file static helper a generation factory hands its own context to.
/// </summary>
internal static class GenerationHelperLookup
{
    /// <summary>
    /// Resolves <c>New(context) =&gt; new(Helper(context))</c> to the helper declaration.
    /// </summary>
    /// <param name="call">The helper call.</param>
    /// <param name="type">The concept owning both methods.</param>
    /// <param name="factory">The factory taking the generation context.</param>
    /// <param name="model">The model resolving the call.</param>
    /// <returns>The helper declaration, or null when the call is anything else.</returns>
    internal static MethodDeclarationSyntax? Find(InvocationExpressionSyntax call, INamedTypeSymbol type, MethodDeclarationSyntax factory, SemanticModel model)
    {
        if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol helper || !IsContextHelper(helper, type) || !PassesOwnContext(call, factory, model))
        {
            return null;
        }

        return helper.DeclaringSyntaxReferences is [var reference] && reference.GetSyntax() is MethodDeclarationSyntax declaration && declaration.SyntaxTree == factory.SyntaxTree ? declaration : null;
    }

    static bool IsContextHelper(IMethodSymbol helper, INamedTypeSymbol type) =>
        helper is { IsStatic: true, IsGenericMethod: false, ReturnType.SpecialType: SpecialType.System_String, Parameters: [var parameter] } &&
        SymbolEqualityComparer.Default.Equals(helper.ContainingType, type) && parameter.Type.Is("Cratis.Concepts.GenerationContext");

    static bool PassesOwnContext(InvocationExpressionSyntax call, MethodDeclarationSyntax factory, SemanticModel model) =>
        call.ArgumentList.Arguments is [{ NameColon: null } argument] && factory.ParameterList.Parameters.Count == 1 &&
        model.GetDeclaredSymbol(factory.ParameterList.Parameters[0]) is { } own &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(argument.Expression).Symbol, own);
}
