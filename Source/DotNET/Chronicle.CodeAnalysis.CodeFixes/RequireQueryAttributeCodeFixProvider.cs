// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Offers a previewable, whole-type migration to explicit query selection.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequireQueryAttributeCodeFixProvider)), Shared]
public class RequireQueryAttributeCodeFixProvider : CodeFixProvider
{
    const string Title = "Require [Query] and mark existing queries (preview endpoints)";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticDescriptors.ARCCHR0019_ReadModelFoldMayBeQuery.Id];

    /// <inheritdoc/>
    public override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var method = root?.FindNode(context.Diagnostics[0].Location.SourceSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is null)
        {
            return;
        }

        if (semanticModel?.GetDeclaredSymbol(method, context.CancellationToken) is not IMethodSymbol symbol)
        {
            return;
        }

        if (!CanMigrate(symbol))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => Migrate(context.Document.Project.Solution, symbol.ContainingType, cancellationToken),
                Title),
            context.Diagnostics[0]);
    }

    static bool CanMigrate(IMethodSymbol method)
    {
        if (ModelBoundQueryMethodSymbols.IsMarked(method))
        {
            return false;
        }

        return !ModelBoundQueryMethodSymbols.Validate(method.ContainingType).Any() && !WouldSelectOverloads(method.ContainingType);
    }

    static bool WouldSelectOverloads(INamedTypeSymbol type) => type.GetMembers().OfType<IMethodSymbol>()
        .Where(method => ModelBoundQueryMethodSymbols.IsMarked(method) || ShouldRetainAsQuery(method))
        .GroupBy(_ => _.Name).Any(_ => _.Count() > 1);

    static bool ShouldRetainAsQuery(IMethodSymbol method) =>
        ModelBoundQueryMethodSymbols.IsValidExplicitQuery(method) && !ReadModelCreatorAnalyzer.IsPossibleFold(method);

    static bool NeedsQueryMarker(IMethodSymbol method) => ShouldRetainAsQuery(method) && !ModelBoundQueryMethodSymbols.IsMarked(method);

    static async Task<Solution> Migrate(Solution solution, INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        var readModel = type.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.ToDisplayString() == ModelBoundQueryMethodSymbols.ReadModelName)?.ApplicationSyntaxReference;
        var declarations = type.DeclaringSyntaxReferences;
        foreach (var group in declarations.GroupBy(_ => _.SyntaxTree))
        {
            var document = solution.GetDocument(group.Key)!;
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false))!;
            var semanticModel = (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false))!;
            var nodes = group.Select(_ => root.FindNode(_.Span)).OfType<TypeDeclarationSyntax>().ToArray();
            var migration = new MigrationContext(semanticModel, readModel, declarations[0], cancellationToken);
            var rewritten = root.ReplaceNodes(nodes, (original, _) => RewriteDeclaration(original, migration));
            solution = solution.WithDocumentSyntaxRoot(document.Id, rewritten);
        }

        return solution;
    }

    static TypeDeclarationSyntax RewriteDeclaration(TypeDeclarationSyntax original, MigrationContext migration)
    {
        var result = original.ReplaceNodes(
            original.Members.OfType<MethodDeclarationSyntax>(),
            (method, _) => MarkQuery(method, migration));
        if (migration.DeclaresReadModel(original))
        {
            return RequireExistingAttribute(original, result, migration.ReadModel!);
        }

        return migration.NeedsReadModel(original) ? AddReadModelAttribute(result) : result;
    }

    static TypeDeclarationSyntax AddReadModelAttribute(TypeDeclarationSyntax type) => type.AddAttributeLists(AttributeList(SingletonSeparatedList(
        Attribute(ParseName("global::Cratis.Arc.Queries.ModelBound.ReadModel").WithAdditionalAnnotations(Simplifier.Annotation))
            .AddArgumentListArguments(RequireArgument())))).WithAdditionalAnnotations(Formatter.Annotation);

    static MethodDeclarationSyntax MarkQuery(MethodDeclarationSyntax method, MigrationContext migration)
    {
        var symbol = migration.SymbolOf(method);
        if (!NeedsQueryMarker(symbol))
        {
            return method;
        }

        return method.WithoutLeadingTrivia().AddAttributeLists(Marker("Query"))
            .WithLeadingTrivia(method.GetLeadingTrivia()).WithAdditionalAnnotations(Formatter.Annotation);
    }

    static TypeDeclarationSyntax RequireExistingAttribute(TypeDeclarationSyntax original, TypeDeclarationSyntax result, SyntaxReference readModel)
    {
        var originalAttribute = original.AttributeLists.SelectMany(_ => _.Attributes).First(_ => _.Span == readModel.Span);
        var listIndex = original.AttributeLists.IndexOf((AttributeListSyntax)originalAttribute.Parent!);
        var attributeIndex = ((AttributeListSyntax)originalAttribute.Parent!).Attributes.IndexOf(originalAttribute);
        var list = result.AttributeLists[listIndex];
        var attribute = list.Attributes[attributeIndex];
        var existing = attribute.ArgumentList?.Arguments.FirstOrDefault(_ => _.NameEquals?.Name.Identifier.ValueText == "RequireQueryAttribute");
        var updated = existing is null ? attribute.AddArgumentListArguments(RequireArgument()) : attribute.ReplaceNode(existing, RequireArgument());
        return result.WithAttributeLists(result.AttributeLists.Replace(list, list.WithAttributes(list.Attributes.Replace(attribute, updated))))
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    static AttributeArgumentSyntax RequireArgument() => AttributeArgument(LiteralExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.TrueLiteralExpression))
        .WithNameEquals(NameEquals("RequireQueryAttribute"));

    static AttributeListSyntax Marker(string name) => AttributeList(SingletonSeparatedList(
        Attribute(ParseName($"global::Cratis.Arc.Queries.ModelBound.{name}").WithAdditionalAnnotations(Simplifier.Annotation))));

    sealed class MigrationContext(SemanticModel semanticModel, SyntaxReference? readModel, SyntaxReference firstDeclaration, CancellationToken cancellationToken)
    {
        /// <summary>
        /// Gets the directly declared owner attribute, when present.
        /// </summary>
        public SyntaxReference? ReadModel => readModel;

        /// <summary>
        /// Checks whether this partial declaration contains the owner attribute.
        /// </summary>
        /// <param name="declaration">The partial declaration.</param>
        /// <returns>Whether the attribute must be updated here.</returns>
        public bool DeclaresReadModel(TypeDeclarationSyntax declaration)
        {
            var attribute = readModel;
            if (attribute is null)
            {
                return false;
            }

            return attribute.SyntaxTree == declaration.SyntaxTree && declaration.Span.Contains(attribute.Span);
        }

        /// <summary>
        /// Checks whether the first partial needs a direct opt-in declaration.
        /// </summary>
        /// <param name="declaration">The partial declaration.</param>
        /// <returns>Whether an owner attribute must be added here.</returns>
        public bool NeedsReadModel(TypeDeclarationSyntax declaration) => readModel is null && IsFirstDeclaration(declaration);

        /// <summary>
        /// Resolves a method in this document's semantic model.
        /// </summary>
        /// <param name="method">The method declaration.</param>
        /// <returns>The method symbol.</returns>
        public IMethodSymbol SymbolOf(MethodDeclarationSyntax method) =>
            (IMethodSymbol)semanticModel.GetDeclaredSymbol(method, cancellationToken)!;

        bool IsFirstDeclaration(TypeDeclarationSyntax declaration) =>
            declaration.SyntaxTree == firstDeclaration.SyntaxTree && declaration.Span == firstDeclaration.Span;
    }
}
