// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Proves that a copied rule depends only on its body and the native generation context.
/// </summary>
/// <param name="concept">The concept whose forwarding constructions will be removed.</param>
/// <param name="model">The model resolving the original body.</param>
internal class GenerationRuleDependencies(INamedTypeSymbol concept, SemanticModel model)
{
    static readonly HashSet<string> _types = new(StringComparer.Ordinal)
    {
        "System.String", "System.Math", "System.Convert", "System.Guid",
        "System.Security.Cryptography.RandomNumberGenerator", "Cratis.Concepts.GenerateValue",
        "Cratis.Concepts.Checksum", "Cratis.Concepts.ValueEncoding"
    };
    static readonly HashSet<string> _contextMembers = new(StringComparer.Ordinal) { "Now", "Tenant" };
    static readonly HashSet<string> _timeBased = new(StringComparer.Ordinal) { "Ulid", "UuidV7", "TypeId" };

    /// <summary>
    /// Checks symbols, imports and context use without executing a factory.
    /// </summary>
    /// <param name="body">The original factory body.</param>
    /// <param name="primitives">The proven primitive returns.</param>
    /// <returns>Whether the body is self-contained.</returns>
    internal bool Allows(SyntaxNode body, ExpressionSyntax[] primitives)
    {
        if (body.DescendantNodesAndSelf().Any(IsUnsupportedSyntax))
        {
            return false;
        }

        return body.DescendantNodesAndSelf().OfType<ExpressionSyntax>().All(node => AllowsExpression(node, primitives));
    }

    static bool IsUnsupportedSyntax(SyntaxNode node) => node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or AwaitExpressionSyntax or TypeOfExpressionSyntax;

    static bool IsAllowedType(INamedTypeSymbol type) => type.DeclaringSyntaxReferences.IsEmpty && _types.Contains(type.FullMetadataName());

    static bool IsLocalType(ITypeSymbol type) => type.SpecialType != SpecialType.None || IsNamedLocalType(type);

    static bool IsNamedLocalType(ITypeSymbol type) => type.Is("System.DateTimeOffset") || (type is INamedTypeSymbol named && IsAllowedType(named));

    static bool IsFactoryContext(IParameterSymbol parameter) => parameter.Name == "context" && parameter.Type.Is("Cratis.Concepts.GenerationContext");

    static bool HasFactoryContext(IInvocationOperation call) => call.Arguments.Select(argument => argument.Value)
        .OfType<IParameterReferenceOperation>().Any(reference => IsFactoryContext(reference.Parameter));

    static bool IsTimeBased(IMethodSymbol method) => method.ContainingType.Is("Cratis.Concepts.GenerateValue") && _timeBased.Contains(method.Name);

    static bool IsBareStaticMember(IdentifierNameSyntax name, ISymbol? symbol)
    {
        if (symbol is not (IMethodSymbol or IPropertySymbol or IFieldSymbol))
        {
            return false;
        }

        return symbol.IsStatic && name.Parent is not MemberAccessExpressionSyntax;
    }

    static bool AllowedMember(ISymbol symbol)
    {
        if (symbol.ContainingType.Is("Cratis.Concepts.GenerationContext"))
        {
            return symbol is IPropertySymbol && _contextMembers.Contains(symbol.Name);
        }
        if (symbol.ContainingType.Is("System.DateTimeOffset"))
        {
            return !symbol.IsStatic && symbol.DeclaringSyntaxReferences.IsEmpty;
        }

        return IsAllowedType(symbol.ContainingType) && symbol.DeclaringSyntaxReferences.IsEmpty;
    }

    static bool IsNonReference(ExpressionSyntax node) => node is not IdentifierNameSyntax || node.Parent is MemberAccessExpressionSyntax or NameColonSyntax;

    static bool Owns(ILocalSymbol local, SyntaxNode node) => local.DeclaringSyntaxReferences.Any(reference =>
        reference.SyntaxTree == node.SyntaxTree && reference.GetSyntax().Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault() == node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault());

    bool AllowsExpression(ExpressionSyntax node, ExpressionSyntax[] primitives) => node switch
    {
        LiteralExpressionSyntax literal => literal.Token.ValueText != "Default",
        BaseObjectCreationExpressionSyntax creation => AllowsConstruction(creation, primitives),
        IdentifierNameSyntax name => AllowsName(name),
        InvocationExpressionSyntax call => AllowsInvocation(call),
        _ => AllowsSymbol(model.GetSymbolInfo(node).Symbol, node)
    };

    bool AllowsName(IdentifierNameSyntax name)
    {
        if (model.GetAliasInfo(name) is not null)
        {
            return false;
        }
        var symbol = model.GetSymbolInfo(name).Symbol;

        return !IsBareStaticMember(name, symbol) && AllowsSymbol(symbol, name);
    }

    bool AllowsInvocation(InvocationExpressionSyntax call)
    {
        if (model.GetOperation(call) is not IInvocationOperation invocation)
        {
            return false;
        }
        if (IsTimeBased(invocation.TargetMethod) && !HasFactoryContext(invocation))
        {
            return false;
        }

        return AllowedMember(invocation.TargetMethod);
    }

    bool AllowsConstruction(BaseObjectCreationExpressionSyntax creation, ExpressionSyntax[] primitives) =>
        primitives.Any(primitive => primitive.Parent?.Parent?.Parent == creation) ||
        (model.GetTypeInfo(creation).Type is INamedTypeSymbol created && IsAllowedType(created));

    bool AllowsSymbol(ISymbol? symbol, ExpressionSyntax node) => symbol switch
    {
        IParameterSymbol parameter => AllowsContext(parameter, node),
        ILocalSymbol local => Owns(local, node) && IsLocalType(local.Type),
        IMethodSymbol or IPropertySymbol or IFieldSymbol => AllowedMember(symbol),
        INamedTypeSymbol named => AllowsNamedType(named),
        _ => IsNonReference(node)
    };

    bool AllowsNamedType(INamedTypeSymbol named) => IsLocalType(named) || SymbolEqualityComparer.Default.Equals(named, concept);

    bool AllowsContext(IParameterSymbol parameter, ExpressionSyntax node) => IsFactoryContext(parameter) && IsContextUse(node);

    bool IsContextUse(ExpressionSyntax node) => node.Parent switch
    {
        MemberAccessExpressionSyntax member => _contextMembers.Contains(member.Name.Identifier.ValueText),
        ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax call } => model.GetSymbolInfo(call).Symbol is IMethodSymbol method && method.ContainingType.Is("Cratis.Concepts.GenerateValue"),
        _ => false
    };
}
