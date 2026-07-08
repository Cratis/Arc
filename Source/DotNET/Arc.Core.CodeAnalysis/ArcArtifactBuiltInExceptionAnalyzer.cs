// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that flags built-in exception types thrown from Arc artifacts.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ArcArtifactBuiltInExceptionAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string HandleMethodName = "Handle";
    const string ReactorInterfaceName = "IReactor";
    const string ReactorsNamespace = "Cratis.Chronicle.Reactors";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARC0012_ArcArtifactThrowsBuiltInException];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeThrow, SyntaxKind.ThrowStatement, SyntaxKind.ThrowExpression);
    }

    static void AnalyzeThrow(SyntaxNodeAnalysisContext context)
    {
        var thrownExpression = context.Node switch
        {
            ThrowStatementSyntax throwStatement => throwStatement.Expression,
            ThrowExpressionSyntax throwExpression => throwExpression.Expression,
            _ => null
        };

        if (thrownExpression is not ObjectCreationExpressionSyntax objectCreation)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(objectCreation.Type).Symbol is not INamedTypeSymbol exceptionType ||
            !IsBuiltInException(exceptionType, context.Compilation))
        {
            return;
        }

        if (!IsInArcArtifactScope(context.Node, context.SemanticModel))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARC0012_ArcArtifactThrowsBuiltInException,
            objectCreation.GetLocation(),
            exceptionType.Name));
    }

    static bool IsBuiltInException(INamedTypeSymbol type, Compilation compilation)
    {
        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");

        if (exceptionType is null || !InheritsFromOrEquals(type, exceptionType))
        {
            return false;
        }

        var @namespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return @namespace == "System" || @namespace.StartsWith("System.", StringComparison.Ordinal);
    }

    static bool InheritsFromOrEquals(INamedTypeSymbol type, INamedTypeSymbol candidateBase)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, candidateBase))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsInArcArtifactScope(SyntaxNode node, SemanticModel semanticModel)
    {
        if (node.FirstAncestorOrSelf<TypeDeclarationSyntax>() is not { } typeDeclaration ||
            semanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol)
        {
            return false;
        }

        if (IsValidator(typeSymbol) || IsReactor(typeSymbol))
        {
            return true;
        }

        if (!HasCommandAttribute(typeSymbol))
        {
            return false;
        }

        return node.FirstAncestorOrSelf<MethodDeclarationSyntax>() is { } method &&
            method.Identifier.ValueText == HandleMethodName;
    }

    static bool HasCommandAttribute(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == CommandAttributeName);

    static bool IsValidator(INamedTypeSymbol typeSymbol)
    {
        for (var baseType = typeSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            var @namespace = baseType.ContainingNamespace?.ToDisplayString();

            if ((baseType.Name == "CommandValidator" && @namespace == "Cratis.Arc.Commands") ||
                (baseType.Name == "ConceptValidator" && @namespace == "Cratis.Arc.Validation"))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsReactor(INamedTypeSymbol typeSymbol) =>
        typeSymbol.AllInterfaces.Any(@interface =>
            @interface.Name == ReactorInterfaceName &&
            @interface.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);
}
