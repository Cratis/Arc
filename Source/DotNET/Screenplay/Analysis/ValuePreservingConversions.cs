// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Proves that conversions preserve a concept's backing value.
/// </summary>
internal static class ValuePreservingConversions
{
    /// <summary>
    /// Checks contextual conversions and explicit wrappers before an expression is unwrapped.
    /// </summary>
    /// <param name="expression">The expression as authored.</param>
    /// <param name="model">The expression's semantic model.</param>
    /// <param name="models">The models of every analyzed project, or null for a single-compilation reading.</param>
    /// <param name="trustFrameworkMetadata">Whether fixture reading may trust a metadata-only framework backing conversion.</param>
    /// <returns>Whether every applied user-defined conversion forwards unchanged.</returns>
    internal static bool Preserves(ExpressionSyntax expression, SemanticModel model, SemanticModels? models = null, bool trustFrameworkMetadata = false)
    {
        models ??= new([model.Compilation]);
        var conversion = model.GetConversion(expression);
        if (conversion.IsUserDefined && !Forwards(conversion.MethodSymbol, models, trustFrameworkMetadata))
        {
            return false;
        }

        return PreservesOperation(model.GetOperation(expression), models, trustFrameworkMetadata) && PreservesWrapper(expression, model, models, trustFrameworkMetadata);
    }

    static bool PreservesWrapper(ExpressionSyntax expression, SemanticModel model, SemanticModels models, bool trustFrameworkMetadata) => expression switch
    {
        ParenthesizedExpressionSyntax parentheses => Preserves(parentheses.Expression, model, models, trustFrameworkMetadata),
        CastExpressionSyntax cast => Preserves(cast.Expression, model, models, trustFrameworkMetadata),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppress => Preserves(suppress.Operand, model, models, trustFrameworkMetadata),
        _ => true
    };

    static bool PreservesOperation(IOperation? operation, SemanticModels models, bool trustFrameworkMetadata) => operation switch
    {
        IConversionOperation conversion => (conversion.OperatorMethod is null || Forwards(conversion.OperatorMethod, models, trustFrameworkMetadata)) && PreservesOperation(conversion.Operand, models, trustFrameworkMetadata),
        IParenthesizedOperation parentheses => PreservesOperation(parentheses.Operand, models, trustFrameworkMetadata),
        _ => true
    };

    static bool Forwards(IMethodSymbol? method, SemanticModels models, bool trustFrameworkMetadata)
    {
        if (IsFrameworkValueExtraction(method))
        {
            return true;
        }
        if (!HasBackingParameter(method))
        {
            return false;
        }

        method = models.SourceOf(method!) ?? method!;
        if (trustFrameworkMetadata && method.DeclaringSyntaxReferences.Length == 0 && method.ContainingAssembly.Name.StartsWith("Cratis.", StringComparison.Ordinal))
        {
            return true;
        }

        return method.DeclaringSyntaxReferences.Select(reference => ReturnedExpression(reference.GetSyntax()))
            .OfType<ExpressionSyntax>().Any(expression => ConstructsUnchanged(expression, method.Parameters[0], method.ReturnType, models));
    }

    static bool HasBackingParameter(IMethodSymbol? method) =>
        method is { Parameters: [var parameter] } &&
        method.ReturnType.FindBase(WellKnownTypeNames.ConceptAs) is { TypeArguments: [var backing] } &&
        SymbolEqualityComparer.Default.Equals(parameter.Type, backing);

    /// <summary>
    /// Recognizes the framework base's unchanged Value extraction, the same trusted boundary as constructor forwarding.
    /// </summary>
    /// <param name="method">The applied conversion.</param>
    /// <returns>Whether this is the framework's backing-value conversion.</returns>
    static bool IsFrameworkValueExtraction(IMethodSymbol? method) =>
        method is { Parameters: [var parameter], ContainingType: { TypeArguments: [var backing] } type } &&
        string.Equals(method.Name, "op_Implicit", StringComparison.Ordinal) && type.Is(WellKnownTypeNames.ConceptAs) && type.ContainingAssembly.Name.StartsWith("Cratis.", StringComparison.Ordinal) &&
        SymbolEqualityComparer.Default.Equals(parameter.Type, type) && SymbolEqualityComparer.Default.Equals(method.ReturnType, backing);

    static ExpressionSyntax? ReturnedExpression(SyntaxNode declaration) => declaration switch
    {
        ConversionOperatorDeclarationSyntax { ExpressionBody.Expression: var expression } => expression,
        ConversionOperatorDeclarationSyntax { Body.Statements: [ReturnStatementSyntax { Expression: var expression }] } => expression,
        _ => null
    };

    static bool ConstructsUnchanged(ExpressionSyntax expression, IParameterSymbol parameter, ITypeSymbol target, SemanticModels models)
    {
        if (models.For(expression.SyntaxTree) is not { } model)
        {
            return false;
        }
        if (model.GetOperation(expression) is not IObjectCreationOperation { Constructor: { } constructor, Initializer: null, Arguments: [var argument] } creation)
        {
            return false;
        }
        if (!SymbolEqualityComparer.Default.Equals(creation.Type, target) || !IsUnchangedArgument(argument, parameter))
        {
            return false;
        }

        return new UuidConstructorForwarding(models).Read(constructor, new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default));
    }

    static bool IsUnchangedArgument(IArgumentOperation argument, IParameterSymbol parameter) =>
        argument.InConversion.IsIdentity && argument.Value is IParameterReferenceOperation reference &&
        SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);
}
