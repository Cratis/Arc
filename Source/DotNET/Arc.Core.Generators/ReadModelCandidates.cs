// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Generators;

/// <summary>
/// Filters declarations before binding while retaining inherited, aliased and partial query declarations.
/// </summary>
internal static class ReadModelCandidates
{
    /// <summary>
    /// Checks whether a declaration can contribute a read model or a query diagnostic.
    /// </summary>
    /// <param name="node">The candidate syntax node.</param>
    /// <returns>Whether semantic binding is needed.</returns>
    internal static bool HasRelatedSyntax(SyntaxNode node) => node is TypeDeclarationSyntax type &&
        (type.AttributeLists.Count > 0 || type.BaseList is not null || type.Modifiers.Any(SyntaxKind.PartialKeyword) ||
         type.Members.Any(member => member.AttributeLists.Count > 0));

    /// <summary>
    /// Binds only the first declaration so partial types are processed once, in declaration order.
    /// </summary>
    /// <param name="context">The syntax context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The declared type, or null for a subsequent declaration.</returns>
    internal static INamedTypeSymbol? FirstDeclaration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var type = context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken) as INamedTypeSymbol;
        var first = type?.DeclaringSyntaxReferences.FirstOrDefault();
        return first?.SyntaxTree == context.Node.SyntaxTree && first.Span == context.Node.Span ? type : null;
    }

    /// <summary>
    /// Determines whether a type owns read-model queries, including inherited explicit selection.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <param name="compilation">The current compilation.</param>
    /// <returns>Whether the type owns read-model queries.</returns>
    internal static bool IsReadModel(INamedTypeSymbol type, Compilation compilation)
    {
        var readModelAttribute = compilation.GetTypeByMetadataName(ModelBoundQueryMethodSymbols.ReadModelName);
        return type.GetAttributes().Any(attribute => attribute.AttributeClass is { } attributeType &&
            SymbolEqualityComparer.Default.Equals(attributeType, readModelAttribute)) ||
            ModelBoundQueryMethodSymbols.RequiresQueryAttribute(type);
    }
}
