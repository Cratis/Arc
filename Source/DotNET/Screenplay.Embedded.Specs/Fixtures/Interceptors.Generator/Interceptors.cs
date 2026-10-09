// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EmbeddedInterceptors.Generator;

/// <summary>
/// Emits interceptors in namespaces enabled through each of Csc's feature properties.
/// </summary>
[Generator]
public class Interceptors : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var calls = context.SyntaxProvider.CreateSyntaxProvider(
            (node, _) => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifier } } && identifier.Identifier.ValueText.Equals("CompilerFeatureCalls", StringComparison.Ordinal),
            (syntax, token) =>
            {
                var invocation = (InvocationExpressionSyntax)syntax.Node;
                var name = ((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier.ValueText;
                var location = syntax.SemanticModel.GetInterceptableLocation(invocation, token)!;

                return (Name: name, Attribute: location.GetInterceptsLocationAttributeSyntax());
            });

        context.RegisterSourceOutput(calls.Combine(context.ParseOptionsProvider), (production, input) =>
        {
            var (call, options) = input;
            var value = options.Features.TryGetValue("embedded-test", out var stated) ? stated : "missing";
            production.AddSource($"{call.Name}.g.cs",
                $"namespace EmbeddedInterceptors.{call.Name}; public static class Generated {{ {call.Attribute} public static string Intercept() => \"{call.Name}:{value}\"; }}");
        });
    }
}
