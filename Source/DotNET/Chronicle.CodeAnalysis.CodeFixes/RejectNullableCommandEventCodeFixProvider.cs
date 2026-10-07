// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Replaces a nullable event result with an explicit validation rejection for supported local handlers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RejectNullableCommandEventCodeFixProvider)), Shared]
public class RejectNullableCommandEventCodeFixProvider : CodeFixProvider
{
    const string Title = "Return an event or an explicit validation rejection";
    const string ValidationResult = "global::Cratis.Arc.Validation.ValidationResult";
    const string Rejection = ValidationResult + ".Error(\"TODO: explain why\")";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ARCCHR0015_NullableCommandEventReturn.Id];

    /// <inheritdoc/>
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var method = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<MethodDeclarationSyntax>();
        var model = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || method?.ReturnType.Span.Contains(diagnostic.Location.SourceSpan) != true ||
            model?.GetDeclaredSymbol(method, cancellationToken) is not IMethodSymbol { IsAbstract: false, IsVirtual: false, IsOverride: false } symbol ||
            symbol.ExplicitInterfaceImplementations.Length > 0 ||
            !RawGuidResponseAnalysis.HasAttribute(symbol.ContainingType, "Cratis.Arc.Commands.ModelBound.CommandAttribute", model.Compilation))
        {
            return;
        }

        var response = RawGuidResponseAnalysis.UnwrapAwaitable(symbol.ReturnType, model.Compilation);
        var events = NullableCommandEventReturnAnalyzer.NullableEvents(symbol.ReturnType, model.Compilation).ToArray();
        if (events.Length != 1 || (!SymbolEqualityComparer.Default.Equals(response, symbol.ReturnType) && !symbol.IsAsync) ||
            !CanReplaceResponse(response, events[0], model.Compilation) ||
            model.Compilation.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        var resultType = ParseTypeName($"global::Cratis.Monads.Result<{events[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {ValidationResult}>");
        var returnType = ReplaceReturnType(method.ReturnType, response, symbol.ReturnType, resultType);
        if (returnType is null)
        {
            return;
        }

        var rewriter = new NullableEventReturnRewriter(Rejection, model, cancellationToken);
        var replacement = (MethodDeclarationSyntax)rewriter.Visit(method);
        if (!rewriter.IsSafe)
        {
            return;
        }

        replacement = replacement.WithReturnType(returnType.WithTriviaFrom(method.ReturnType));
        var changedDocument = context.Document.WithSyntaxRoot(root.ReplaceNode(method, replacement));
        var changedCompilation = await changedDocument.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (changedCompilation?.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) != false)
        {
            return;
        }

        context.RegisterCodeFix(CodeAction.Create(Title, _ => Task.FromResult(changedDocument), Title), diagnostic);
    }

    static bool CanReplaceResponse(ITypeSymbol response, ITypeSymbol eventType, Compilation compilation)
    {
        if (SymbolEqualityComparer.Default.Equals(response, eventType))
        {
            return true;
        }

        // Preserve only the event/rejection contract; do not discard other response branches.
        return response is INamedTypeSymbol { TypeArguments.Length: 2 } union &&
            RawGuidResponseAnalysis.UnionBranches(union, compilation).Count() == 2 &&
            union.TypeArguments.Any(type => SymbolEqualityComparer.Default.Equals(type, compilation.GetTypeByMetadataName("Cratis.Arc.Validation.ValidationResult")));
    }

    static TypeSyntax? ReplaceReturnType(TypeSyntax syntax, ITypeSymbol response, ITypeSymbol returnType, TypeSyntax resultType)
    {
        if (SymbolEqualityComparer.Default.Equals(response, returnType))
        {
            return syntax is NullableTypeSyntax or GenericNameSyntax or QualifiedNameSyntax or AliasQualifiedNameSyntax ? resultType : null;
        }

        // Keep Task/ValueTask syntax, including qualification. Aliases and invariant factories may need a manual edit.
        var generic = syntax switch
        {
            GenericNameSyntax name => name,
            QualifiedNameSyntax { Right: GenericNameSyntax name } => name,
            AliasQualifiedNameSyntax { Name: GenericNameSyntax name } => name,
            _ => null
        };
        if (generic?.TypeArgumentList.Arguments.Count != 1)
        {
            return null;
        }

        return syntax.ReplaceNode(generic.TypeArgumentList.Arguments[0], resultType.WithTriviaFrom(generic.TypeArgumentList.Arguments[0]));
    }
}
