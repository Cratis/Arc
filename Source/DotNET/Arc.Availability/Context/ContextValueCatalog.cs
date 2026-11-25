// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// Holds the 'provide' declarations of a model and finds the one that wins for a scope.
/// </summary>
[IgnoreConvention]
public class ContextValueCatalog
{
    readonly List<ContextValueDeclaration> _declarations = [];

    /// <summary>
    /// Gets every declaration.
    /// </summary>
    public IReadOnlyList<ContextValueDeclaration> Declarations => _declarations;

    /// <summary>
    /// Adds a declaration.
    /// </summary>
    /// <param name="declaration">The declaration to add.</param>
    /// <returns>The catalog, for chaining.</returns>
    /// <exception cref="ReservedContextValueNameException">The name is a built-in context name.</exception>
    /// <exception cref="InvalidOperationException">The same name is already declared at the same level.</exception>
    public ContextValueCatalog Declare(ContextValueDeclaration declaration)
    {
        if (ReservedContextNames.IsReserved(declaration.Name))
        {
            throw new ReservedContextValueNameException(declaration.Name);
        }

        if (_declarations.Exists(_ => _.Name == declaration.Name && _.Scope.Path.SequenceEqual(declaration.Scope.Path)))
        {
            throw new InvalidOperationException($"The context value '{declaration.Name}' is already declared at this level.");
        }

        _declarations.Add(declaration);
        return this;
    }

    /// <summary>
    /// Finds the declaration that wins for a name in a scope: the nearest enclosing level.
    /// </summary>
    /// <param name="name">The name of the value.</param>
    /// <param name="scope">The scope the request runs in.</param>
    /// <returns>The winning declaration, or <see langword="null"/> when none applies.</returns>
    public ContextValueDeclaration? Find(string name, ContextScope scope) =>
        _declarations
            .Where(_ => _.Name == name && _.Scope.Encloses(scope))
            .OrderByDescending(_ => _.Scope.Path.Count)
            .FirstOrDefault();

    /// <summary>
    /// Verifies no provided values depend on each other in a cycle, at any declared level.
    /// </summary>
    /// <exception cref="ContextValueCycleException">A cycle exists.</exception>
    public void Validate()
    {
        foreach (var declaration in _declarations)
        {
            Walk(declaration, declaration.Scope, []);
        }
    }

    /// <summary>
    /// Creates the resolver for one request.
    /// </summary>
    /// <param name="scope">The scope the request runs in.</param>
    /// <param name="ambient">The values the runtime supplies, such as tenant, identity and arguments.</param>
    /// <param name="audiences">Optional lazy source of the effective audiences; <see langword="null"/> result means unknown.</param>
    /// <returns>The resolver.</returns>
    public ContextValueResolver CreateResolver(
        ContextScope scope,
        IReadOnlyDictionary<string, object?>? ambient = null,
        Func<CancellationToken, ValueTask<Audiences.EffectiveAudiences?>>? audiences = null) =>
        new(this, scope, ambient ?? new Dictionary<string, object?>(), audiences);

    void Walk(ContextValueDeclaration declaration, ContextScope scope, List<string> chain)
    {
        if (chain.Contains(declaration.Name))
        {
            throw new ContextValueCycleException([.. chain, declaration.Name]);
        }

        chain.Add(declaration.Name);
        foreach (var dependency in declaration.Provider.Dependencies)
        {
            var next = Find(dependency, scope);
            if (next is not null)
            {
                Walk(next, scope, chain);
            }
        }

        chain.RemoveAt(chain.Count - 1);
    }
}
