// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Marks a possible read-model fold creator as not a query.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddNotAQueryCodeFixProvider)), Shared]
public class AddNotAQueryCodeFixProvider : CodeFixProvider
{
    const string Title = "Add [NotAQuery]";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticDescriptors.ARCCHR0019_ReadModelCreatorNeedsNotAQuery.Id];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var method = root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method is null)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                cancellationToken => AddAttribute(context.Document, method, cancellationToken),
                Title),
            diagnostic);
    }

    static async Task<Document> AddAttribute(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var attribute = Attribute(ParseName("global::Cratis.Arc.Queries.ModelBound.NotAQuery").WithAdditionalAnnotations(Simplifier.Annotation));
        var rewritten = method.WithoutLeadingTrivia()
            .AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)))
            .WithLeadingTrivia(method.GetLeadingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        return document.WithSyntaxRoot(root.ReplaceNode(method, rewritten));
    }
}
