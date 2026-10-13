// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Follows constructor initializers that preserve their generation primitive unchanged.
/// </summary>
/// <param name="models">The models resolving constructor syntax.</param>
internal class UuidConstructorForwarding(SemanticModels models)
{
    /// <summary>
    /// Proves a constructor's forwarding chain without revisiting constructors.
    /// </summary>
    /// <param name="constructor">The constructor to follow.</param>
    /// <param name="visited">The constructors already followed.</param>
    /// <returns>Whether the primitive reaches the framework base unchanged.</returns>
    public bool Read(IMethodSymbol constructor, ISet<IMethodSymbol> visited)
    {
        if (!visited.Add(constructor))
        {
            return false;
        }
        if (GenerationParameter(constructor) is not { } parameter)
        {
            return false;
        }
        if (IsFrameworkBase(constructor.ContainingType))
        {
            return true;
        }

        return constructor.DeclaringSyntaxReferences.Any(reference => ForwardsFrom(InitializerOf(reference.GetSyntax()), parameter, visited));
    }

    static IParameterSymbol? GenerationParameter(IMethodSymbol constructor) => constructor.Parameters is [var parameter] && IsGenerationPrimitive(parameter.Type) ? parameter : null;

    static bool IsGenerationPrimitive(ITypeSymbol type) => type.Is("System.Guid") || type.SpecialType == SpecialType.System_String;

    static bool IsFrameworkBase(INamedTypeSymbol type) => IsConceptBase(type) && type.ContainingAssembly.Name.StartsWith("Cratis.", StringComparison.Ordinal);

    static bool IsConceptBase(INamedTypeSymbol type) => type.Is(WellKnownTypeNames.ConceptAs) || type.Is(WellKnownTypeNames.EventSourceIdOfT);

    static SyntaxNode? InitializerOf(SyntaxNode declaration) => declaration switch
    {
        RecordDeclarationSyntax record => record.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault(),
        ConstructorDeclarationSyntax { Body: null or { Statements.Count: 0 }, ExpressionBody: null } constructor => constructor.Initializer,
        _ => null
    };

    static ArgumentListSyntax? ArgumentsOf(SyntaxNode initializer) => initializer switch
    {
        PrimaryConstructorBaseTypeSyntax primary => primary.ArgumentList,
        ConstructorInitializerSyntax constructor => constructor.ArgumentList,
        _ => null
    };

    static bool IsUnchangedArgument(ExpressionSyntax expression, IParameterSymbol parameter, SemanticModel model)
    {
        if (expression.DescendantNodesAndSelf().Any(node => node is CastExpressionSyntax))
        {
            return false;
        }

        return model.GetConversion(expression).IsIdentity &&
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(MappingSourceReader.Unwrap(expression)).Symbol, parameter);
    }

    bool ForwardsFrom(SyntaxNode? initializer, IParameterSymbol parameter, ISet<IMethodSymbol> visited)
    {
        if (initializer is null || models.For(initializer.SyntaxTree) is not { } model)
        {
            return false;
        }
        if (ArgumentsOf(initializer)?.Arguments is not [var argument])
        {
            return false;
        }
        if (!IsUnchangedArgument(argument.Expression, parameter, model))
        {
            return false;
        }

        return model.GetSymbolInfo(initializer).Symbol is IMethodSymbol target && Read(target, visited);
    }
}
