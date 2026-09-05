// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.CodeAnalysis.CodeFixes;

/// <summary>
/// Code fix that unwraps a command Handle() method from its Task wrapper to a synchronous signature.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnwrapCommandHandleTaskCodeFixProvider)), Shared]
public class UnwrapCommandHandleTaskCodeFixProvider : CodeFixProvider
{
    const string Title = "Unwrap to synchronous Handle()";

    /// <inheritdoc/>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [DiagnosticDescriptors.ARC0010_CommandHandleWrapsSynchronousResultInTask.Id];

    /// <inheritdoc/>
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        if (root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MethodDeclarationSyntax>() is not { } method)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken => UnwrapAsync(context.Document, method, cancellationToken),
                equivalenceKey: Title),
            diagnostic);
    }

    static async Task<Document> UnwrapAsync(Document document, MethodDeclarationSyntax method, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (root is null || semanticModel is null)
        {
            return document;
        }

        var newReturnType = UnwrapReturnType(method.ReturnType).WithTriviaFrom(method.ReturnType);
        var newModifiers = TokenList(method.Modifiers.Where(modifier => !modifier.IsKind(SyntaxKind.AsyncKeyword)));

        // Unwrap the body using the original in-tree nodes so semantic queries stay valid, then apply the
        // synchronous signature (dropping 'async' and the Task wrapper) on top of the transformed method.
        var newMethod = (method.ExpressionBody is not null
                ? UnwrapExpressionBody(method, method.ExpressionBody, semanticModel)
                : UnwrapBlockBody(method, semanticModel))
            .WithModifiers(newModifiers)
            .WithReturnType(newReturnType);

        return document.WithSyntaxRoot(root.ReplaceNode(method, newMethod));
    }

    static TypeSyntax UnwrapReturnType(TypeSyntax returnType) =>
        GetTaskTypeArgument(returnType) is { } typeArgument
            ? typeArgument
            : PredefinedType(Token(SyntaxKind.VoidKeyword));

    static TypeSyntax? GetTaskTypeArgument(TypeSyntax returnType)
    {
        var genericName = returnType switch
        {
            GenericNameSyntax generic => generic,
            QualifiedNameSyntax { Right: GenericNameSyntax generic } => generic,
            _ => null
        };

        return genericName?.Identifier.ValueText == "Task" && genericName.TypeArgumentList.Arguments.Count == 1
            ? genericName.TypeArgumentList.Arguments[0]
            : null;
    }

    static MethodDeclarationSyntax UnwrapExpressionBody(MethodDeclarationSyntax method, ArrowExpressionClauseSyntax expressionBody, SemanticModel semanticModel)
    {
        var (isWrapper, inner) = Unwrap(expressionBody.Expression, semanticModel);

        if (isWrapper && inner is null)
        {
            return method
                .WithExpressionBody(null)
                .WithSemicolonToken(Token(SyntaxKind.None))
                .WithBody(Block());
        }

        var newExpression = (isWrapper ? inner! : expressionBody.Expression).WithTriviaFrom(expressionBody.Expression);
        return method.WithExpressionBody(expressionBody.WithExpression(newExpression));
    }

    static MethodDeclarationSyntax UnwrapBlockBody(MethodDeclarationSyntax method, SemanticModel semanticModel)
    {
        if (method.Body is null)
        {
            return method;
        }

        var returnStatements = method.Body
            .DescendantNodes(node => node == method.Body || !IsNestedFunction(node))
            .OfType<ReturnStatementSyntax>()
            .Where(statement => statement.Expression is not null)
            .ToArray();

        var newBody = method.Body.ReplaceNodes(returnStatements, (original, _) =>
        {
            var (isWrapper, inner) = Unwrap(original.Expression!, semanticModel);

            if (!isWrapper)
            {
                return original;
            }

            return inner is null
                ? ReturnStatement().WithTriviaFrom(original)
                : original.WithExpression(inner.WithTriviaFrom(original.Expression!));
        });

        return method.WithBody(newBody);
    }

    static (bool IsWrapper, ExpressionSyntax? Inner) Unwrap(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is InvocationExpressionSyntax invocation &&
            semanticModel.GetSymbolInfo(invocation.Expression).Symbol is IMethodSymbol method &&
            method.Name == "FromResult" &&
            IsTaskType(method.ContainingType) &&
            invocation.ArgumentList.Arguments.Count == 1)
        {
            return (true, invocation.ArgumentList.Arguments[0].Expression);
        }

        if (semanticModel.GetSymbolInfo(expression).Symbol is IPropertySymbol property &&
            property.Name == "CompletedTask" &&
            IsTaskType(property.ContainingType))
        {
            return (true, null);
        }

        return (false, expression);
    }

    static bool IsTaskType(INamedTypeSymbol type) =>
        type.Name == "Task" && type.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks";

    static bool IsNestedFunction(SyntaxNode node) =>
        node is SimpleLambdaExpressionSyntax or
            ParenthesizedLambdaExpressionSyntax or
            AnonymousMethodExpressionSyntax or
            LocalFunctionStatementSyntax;
}
