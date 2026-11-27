// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Captures;

/// <summary>
/// Recovers only the property assignments proven by a bound positional record constructor.
/// </summary>
/// <remarks>
/// An explicit constructor can rename, transform, or discard its inputs. Without an identity-assignment proof it
/// stays in the reactor file rather than treating parameter names as event properties.
/// </remarks>
internal static class CaptureArguments
{
    /// <summary>
    /// Reads mappings only when the selected constructor proves the corresponding positional event properties.
    /// </summary>
    /// <param name="model">The semantic model containing the append.</param>
    /// <param name="appended">The local event construction.</param>
    /// <returns>The constructed event and its proven mappings, or null.</returns>
    internal static (INamedTypeSymbol Type, List<CaptureArgument> Arguments)? Read(SemanticModel? model, ObjectCreationExpressionSyntax appended)
    {
        if (model?.GetSymbolInfo(appended).Symbol is not IMethodSymbol { DeclaredAccessibility: Accessibility.Public } constructor)
        {
            return null;
        }

        var fields = appended.ArgumentList?.Arguments.Select(argument => FieldOf(argument.Expression)).ToList() ?? [];
        if (fields.Count != constructor.Parameters.Length)
        {
            return null;
        }

        var arguments = new List<CaptureArgument>();
        foreach (var parameter in constructor.Parameters)
        {
            var property = PropertyOf(parameter, constructor.ContainingType);
            if (property is null || fields[parameter.Ordinal] is not { } field)
            {
                return null;
            }

            arguments.Add(new(property.Name, parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), field));
        }

        return (constructor.ContainingType, arguments);
    }

    static IPropertySymbol? PropertyOf(IParameterSymbol parameter, INamedTypeSymbol type) =>
        type.GetMembers(parameter.Name).OfType<IPropertySymbol>().FirstOrDefault(property =>
            property.DeclaredAccessibility == Accessibility.Public &&
            SymbolEqualityComparer.Default.Equals(property.Type, parameter.Type) &&
            property.DeclaringSyntaxReferences.Any(reference =>
                reference.GetSyntax() is ParameterSyntax { Parent.Parent: RecordDeclarationSyntax } &&
                parameter.DeclaringSyntaxReferences.Any(declaration => declaration.SyntaxTree == reference.SyntaxTree && declaration.Span == reference.Span)));

    static string? FieldOf(ExpressionSyntax expression) =>
        expression is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 3 } invocation && invocation.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax literal
            ? literal.Token.ValueText
            : null;
}
