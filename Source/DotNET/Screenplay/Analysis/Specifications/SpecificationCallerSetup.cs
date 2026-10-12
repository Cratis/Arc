// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Checks the identity, registration, and uses of the documented substituted caller accessor.
/// </summary>
/// <param name="models">The application semantic models.</param>
/// <param name="held">The held-value stability checker.</param>
internal sealed class SpecificationCallerSetup(SemanticModels models, HeldValues held)
{
    /// <summary>
    /// Gets source calls in the specification chain, base first.
    /// </summary>
    /// <param name="type">The specification type.</param>
    /// <returns>The source calls.</returns>
    internal IReadOnlyList<SpecificationCallerCall> CallsIn(INamedTypeSymbol type) =>
        SpecificationMembers.ChainOf(type).SelectMany(member => member.DeclaringSyntaxReferences)
            .Select(reference => reference.GetSyntax()).SelectMany(CallsIn).ToList();

    /// <summary>
    /// Checks whether the factory produces a stable held accessor.
    /// </summary>
    /// <param name="factory">The substitute construction.</param>
    /// <returns>The failure reason, or null.</returns>
    internal string? CheckFactory(SpecificationCallerCall factory)
    {
        var symbol = AssignedSymbol(factory);
        if (symbol is null || !held.IsStable(symbol, factory.Model.Compilation))
        {
            return "the caller accessor is not a stable local or field";
        }

        if (!IsExecutedFactory(factory))
        {
            return "the caller accessor construction is conditional, deferred, or outside Establish and field initialization";
        }

        return factory.Syntax.ArgumentList.Arguments.Count == 0 ? null : "the caller substitute factory must have no constructor arguments";
    }

    /// <summary>
    /// Checks the registration's instance and scenario identity.
    /// </summary>
    /// <param name="registration">The registration call.</param>
    /// <param name="action">The command execution call.</param>
    /// <param name="accessor">The substituted accessor symbol.</param>
    /// <returns>The failure reason, or null.</returns>
    internal string? CheckRegistration(SpecificationCallerCall registration, SpecificationCallerCall action, ISymbol accessor)
    {
        if (!IsInstanceRegistration(registration))
        {
            return "the caller registration is not AddSingleton with one accessor instance";
        }

        var argument = registration.Syntax.ArgumentList.Arguments[0].Expression;
        if (!SymbolEqualityComparer.Default.Equals(registration.Model.GetSymbolInfo(argument).Symbol, accessor))
        {
            return "the caller registration names a different accessor";
        }

        return CheckScenario(registration, action);
    }

    /// <summary>
    /// Checks the single Returns configuration's receiver and argument count.
    /// </summary>
    /// <param name="configuration">The configuration call.</param>
    /// <param name="accessor">The substituted accessor symbol.</param>
    /// <returns>The failure reason, or null.</returns>
    internal string? CheckConfiguration(SpecificationCallerCall configuration, ISymbol accessor)
    {
        var configured = configuration.Model.GetSymbolInfo(SpecificationCallerCalls.CurrentReceiver(configuration)!.Expression).Symbol;
        if (!SymbolEqualityComparer.Default.Equals(configured, accessor))
        {
            return "the caller configuration names a different accessor";
        }

        return configuration.Method?.Name == "Returns" && configuration.Syntax.ArgumentList.Arguments.Count == 1
            ? null : "the caller configuration must set Current.Returns with exactly one principal";
    }

    /// <summary>
    /// Resolves the accessor assigned by the substitute factory.
    /// </summary>
    /// <param name="factory">The substitute construction.</param>
    /// <returns>The assigned symbol, or null.</returns>
    internal ISymbol? AssignedSymbol(SpecificationCallerCall factory) => factory.Syntax.Parent switch
    {
        EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax variable } => factory.Model.GetDeclaredSymbol(variable),
        AssignmentExpressionSyntax assignment => factory.Model.GetSymbolInfo(assignment.Left).Symbol,
        _ => null
    };

    /// <summary>
    /// Rejects extra accessor uses that could mutate or replace its configuration.
    /// </summary>
    /// <param name="type">The specification type.</param>
    /// <param name="accessor">The substituted accessor symbol.</param>
    /// <param name="allowed">The three proven setup calls.</param>
    /// <returns>Whether another use exists.</returns>
    internal bool HasOtherAccessorUse(INamedTypeSymbol type, ISymbol accessor, IReadOnlyList<SpecificationCallerCall> allowed)
    {
        var allowedNodes = allowed.Select(call => call.Syntax).ToList();
        var references = SpecificationMembers.ChainOf(type).SelectMany(member => member.DeclaringSyntaxReferences)
            .Select(reference => reference.GetSyntax()).SelectMany(node => node.DescendantNodes().OfType<IdentifierNameSyntax>());
        return references.Any(reference => IsOtherUse(reference, accessor, allowedNodes));
    }

    static bool IsAllowed(IdentifierNameSyntax reference, IReadOnlyList<InvocationExpressionSyntax> allowed)
    {
        if (reference.Parent is AssignmentExpressionSyntax { Right: InvocationExpressionSyntax factory } assignment && reference == assignment.Left)
        {
            return allowed.Contains(factory);
        }

        return allowed.Any(node => Contains(node, reference));
    }

    static bool Contains(SyntaxNode parent, SyntaxNode child) => parent.SyntaxTree == child.SyntaxTree && parent.Span.Contains(child.Span);

    static bool IsInstanceRegistration(SpecificationCallerCall call) =>
        call.Method?.ContainingType.Is("Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions") == true && HasInstanceArgument(call);

    static bool HasInstanceArgument(SpecificationCallerCall call) =>
        HasAccessorServiceType(call.Method) && call.Syntax.ArgumentList.Arguments.Count == 1;

    static bool HasAccessorServiceType(IMethodSymbol? method) =>
        method?.TypeArguments is [var type] && type.Is("Cratis.Arc.Authorization.ICurrentPrincipalAccessor");

    static bool IsExecutedFactory(SpecificationCallerCall factory) =>
        factory.Unconditional && IsSetupLocation(factory);

    static bool IsSetupLocation(SpecificationCallerCall factory) =>
        factory.InEstablish || factory.Syntax.FirstAncestorOrSelf<MemberDeclarationSyntax>() is FieldDeclarationSyntax;

    static bool IsScenarioServices(MemberAccessExpressionSyntax? services, SemanticModel model) =>
        services is { } access && string.Equals(access.Name.Identifier.ValueText, "Services", StringComparison.Ordinal) &&
        model.GetTypeInfo(access.Expression).Type.Is(WellKnownTypeNames.CommandScenario);

    bool IsOtherUse(IdentifierNameSyntax reference, ISymbol accessor, IReadOnlyList<InvocationExpressionSyntax> allowed) =>
        SymbolEqualityComparer.Default.Equals(models.For(reference.SyntaxTree)?.GetSymbolInfo(reference).Symbol, accessor) && !IsAllowed(reference, allowed);

    string? CheckScenario(SpecificationCallerCall registration, SpecificationCallerCall action)
    {
        var services = ((MemberAccessExpressionSyntax)registration.Syntax.Expression).Expression as MemberAccessExpressionSyntax;
        if (!IsScenarioServices(services, registration.Model))
        {
            return "the caller registration is not on the same command scenario as its action";
        }

        var scenario = registration.Model.GetSymbolInfo(services!.Expression).Symbol;
        if (!SymbolEqualityComparer.Default.Equals(scenario, action.Receiver))
        {
            return "the caller registration is not on the same command scenario as its action";
        }

        return scenario is not null && held.IsStable(scenario, registration.Model.Compilation)
            ? null : "the caller's command scenario is not stable";
    }

    IEnumerable<SpecificationCallerCall> CallsIn(SyntaxNode node)
    {
        if (models.For(node.SyntaxTree) is not { } model)
        {
            return [];
        }

        return node.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(invocation => new SpecificationCallerCall(invocation, model));
    }
}
