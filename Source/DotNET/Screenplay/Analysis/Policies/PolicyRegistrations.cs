// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Finds every named authorization policy the application registers, wherever it is composed.
/// </summary>
/// <remarks>
/// An artifact names a policy; the composition root says what it means. Both shapes the framework offers are read -
/// the options form <c>AddAuthorization(options =&gt; options.AddPolicy(...))</c> and the builder form
/// <c>AddAuthorizationBuilder().AddPolicy(...)</c> - because an application uses whichever it prefers.
/// </remarks>
public static class PolicyRegistrations
{
    /// <summary>
    /// The name of the call registering a policy.
    /// </summary>
    public const string AddPolicy = "AddPolicy";

    const string ArcRegistration = "AddArcAuthorizationPolicy";
    const string ArcRegistrations = "Cratis.Arc.Authorization.AuthorizationPolicyServiceCollectionExtensions";

    /// <summary>
    /// Finds every policy a compilation registers.
    /// </summary>
    /// <param name="compilation">The compilation to read.</param>
    /// <returns>The registrations, keyed by the name each policy is registered under.</returns>
    /// <remarks>
    /// The first registration of a name wins, which is the framework's own behavior for the options form and is the
    /// only choice that keeps the same compilation yielding the same document.
    /// </remarks>
    public static IReadOnlyDictionary<string, PolicyRegistration> In(Compilation compilation) => In([compilation]);

    /// <summary>
    /// Finds every policy the projects of an application register.
    /// </summary>
    /// <param name="compilations">The compilations to read, ordered.</param>
    /// <returns>The registrations, keyed by the name each policy is registered under.</returns>
    /// <remarks>
    /// An artifact naming a policy and the composition root registering it routinely sit in different projects - that
    /// the host is not where the behavior lives is the whole point of layering an application - so every project is
    /// read and the registrations of all of them form one set. The first registration of a name still wins, and the
    /// projects arrive already ordered, so which one that is does not depend on how the caller listed them.
    /// </remarks>
    public static IReadOnlyDictionary<string, PolicyRegistration> In(IReadOnlyList<Compilation> compilations)
    {
        var registrations = new Dictionary<string, PolicyRegistration>(StringComparer.Ordinal);

        foreach (var compilation in compilations)
        {
            foreach (var tree in compilation.SyntaxTrees.OrderBy(_ => _.FilePath, StringComparer.Ordinal))
            {
                Collect(compilation.GetSemanticModel(tree), tree, registrations);
            }
        }

        return registrations;
    }

    /// <summary>
    /// Collects the registrations one syntax tree holds.
    /// </summary>
    /// <param name="semanticModel">The semantic model of the tree.</param>
    /// <param name="tree">The tree to read.</param>
    /// <param name="registrations">The registrations collected so far.</param>
    static void Collect(SemanticModel semanticModel, SyntaxTree tree, Dictionary<string, PolicyRegistration> registrations)
    {
        foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!IsRegistration(invocation, semanticModel))
            {
                continue;
            }

            var name = NameOf(invocation, semanticModel);
            if (name is null || registrations.ContainsKey(name))
            {
                continue;
            }

            var method = (IMethodSymbol)semanticModel.GetSymbolInfo(invocation).Symbol!;
            registrations[name] = IsArcRegistration(method)
                ? new ArcPolicyRegistration(name, method.TypeArguments[0], semanticModel, tree.FilePath)
                : new PolicyRegistration(name, InvocationChain.ArgumentOf(invocation, 1), semanticModel, tree.FilePath);
        }
    }

    /// <summary>
    /// Determines whether a call registers a named policy.
    /// </summary>
    /// <param name="invocation">The call to check.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <returns>True when the call registers a policy.</returns>
    static bool IsRegistration(InvocationExpressionSyntax invocation, SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
        (IsArcRegistration(method) || IsAspNetRegistration(method));

    static bool IsArcRegistration(IMethodSymbol method) =>
        method.Name == ArcRegistration && method.ContainingType.Is(ArcRegistrations);

    static bool IsAspNetRegistration(IMethodSymbol method) =>
        method.Name == AddPolicy &&
        (method.ContainingType.Is(WellKnownTypeNames.AuthorizationOptions) ||
         method.ContainingType.Is(WellKnownTypeNames.AuthorizationBuilder));

    /// <summary>
    /// Reads the name a policy is registered under.
    /// </summary>
    /// <param name="invocation">The call to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <returns>The name, or <see langword="null"/> when it is not a constant.</returns>
    static string? NameOf(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var expression = semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && IsArcRegistration(method)
            ? (semanticModel.GetOperation(invocation) as IInvocationOperation)?.Arguments
                .FirstOrDefault(item => item.Parameter?.Name == "name")?.Value.Syntax as ExpressionSyntax
            : InvocationChain.ArgumentOf(invocation);
        if (expression is not { } argument)
        {
            return null;
        }

        var name = semanticModel.GetConstantValue(argument).Value as string;

        return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }
}
