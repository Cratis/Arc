// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads a key that identifies a read model by several properties at once.
/// </summary>
/// <remarks>
/// A composite key names the type it identifies as well as its parts, and the type comes from the type argument the
/// builder was given rather than from anything inside the body.
/// </remarks>
public static class FluentCompositeKeys
{
    /// <summary>
    /// Reads a composite key.
    /// </summary>
    /// <param name="call">The call declaring the key.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <returns>The key expression, or <see langword="null"/> when no part could be read.</returns>
    public static string? Read(InvocationExpressionSyntax call, SemanticModel semanticModel)
    {
        var type = InvocationChain.TypeArgumentOf(call) is { } argument
            ? semanticModel.GetTypeInfo(argument).Type?.Name
            : null;

        var body = InvocationChain.ArgumentOf(call);
        if (type is null || body is null)
        {
            return null;
        }

        var parts = Parts(body).ToList();

        return parts.Count == 0 ? null : ProjectionExpressions.Composite(type, parts);
    }

    /// <summary>
    /// Reads the parts a composite key is made of.
    /// </summary>
    /// <param name="body">The body declaring the parts.</param>
    /// <returns>The property to expression pairs.</returns>
    static IEnumerable<KeyValuePair<string, string>> Parts(ExpressionSyntax body)
    {
        foreach (var chain in ProjectionPaths.ChainsIn(body))
        {
            string? property = null;

            foreach (var call in InvocationChain.Sequence(chain))
            {
                var name = InvocationChain.NameOf(call);
                var argument = InvocationChain.ArgumentOf(call);

                if (string.Equals(name, "Set", StringComparison.Ordinal))
                {
                    property = ProjectionPaths.ReadDeclared(argument);
                }
                else if (string.Equals(name, "To", StringComparison.Ordinal) &&
                    property is not null &&
                    ProjectionPaths.Read(argument) is { } source)
                {
                    yield return new(property, source);
                    property = null;
                }
            }
        }
    }
}
