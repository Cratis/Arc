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
        if (method is null || semanticModel?.GetDeclaredSymbol(method, context.CancellationToken) is not IMethodSymbol symbol ||
            ModelBoundQueryMethodSymbols.IsMarked(symbol) || ModelBoundQueryMethodSymbols.Validate(symbol.ContainingType).Any() ||
            WouldSelectOverloads(symbol.ContainingType))
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

    static bool WouldSelectOverloads(INamedTypeSymbol type) => type.GetMembers().OfType<IMethodSymbol>()
        .Where(method => ModelBoundQueryMethodSymbols.IsMarked(method) ||
            (ModelBoundQueryMethodSymbols.IsValidExplicitQuery(method) && !ReadModelCreatorAnalyzer.IsPossibleFold(method)))
        .GroupBy(_ => _.Name).Any(_ => _.Count() > 1);

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
            var rewritten = root.ReplaceNodes(nodes, (original, _) => RewriteDeclaration(original, semanticModel, readModel, declarations[0], cancellationToken));
            solution = solution.WithDocumentSyntaxRoot(document.Id, rewritten);
        }

        return solution;
    }

    static TypeDeclarationSyntax RewriteDeclaration(TypeDeclarationSyntax original, SemanticModel semanticModel, SyntaxReference? readModel, SyntaxReference first, CancellationToken cancellationToken)
    {
        var result = original.ReplaceNodes(
            original.Members.OfType<MethodDeclarationSyntax>(),
            (method, _) => MarkQuery(method, semanticModel, cancellationToken));
        if (readModel is not null && readModel.SyntaxTree == original.SyntaxTree && original.Span.Contains(readModel.Span))
        {
            return RequireExistingAttribute(original, result, readModel);
        }

        return readModel is null && original.SyntaxTree == first.SyntaxTree && original.Span == first.Span
            ? result.AddAttributeLists(AttributeList(SingletonSeparatedList(
                Attribute(ParseName("global::Cratis.Arc.Queries.ModelBound.ReadModel").WithAdditionalAnnotations(Simplifier.Annotation))
                    .AddArgumentListArguments(RequireArgument())))).WithAdditionalAnnotations(Formatter.Annotation)
            : result;
    }

    static MethodDeclarationSyntax MarkQuery(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var symbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(method, cancellationToken)!;
        if (!ModelBoundQueryMethodSymbols.IsValidExplicitQuery(symbol) || ModelBoundQueryMethodSymbols.IsMarked(symbol) || ReadModelCreatorAnalyzer.IsPossibleFold(symbol))
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
}
