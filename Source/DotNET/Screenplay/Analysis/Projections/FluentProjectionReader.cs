// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the projection a type declares by defining it against a builder.
/// </summary>
/// <param name="compilation">The compilation being analyzed.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Reading the chain from the source is what makes a fluent projection expressible at all - at runtime it is an
/// expression tree that has already been compiled down to something a document cannot be recovered from.
/// </remarks>
public class FluentProjectionReader(Compilation compilation, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The method a projection is defined in.
    /// </summary>
    public const string DefineMethod = "Define";

    /// <summary>
    /// The identifier of the sequence a projection observes unless it says otherwise.
    /// </summary>
    public const string EventLogSequence = "event-log";

    readonly FluentScopeReader _scopes = new(diagnostics);

    /// <summary>
    /// Determines whether a type defines a projection against a builder, and for what read model.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The read model, or <see langword="null"/> when the type is not a projection.</returns>
    public static ITypeSymbol? ReadModelOf(ITypeSymbol type) =>
        type is { IsAbstract: false, TypeKind: TypeKind.Class }
            ? type.FindInterface(WellKnownTypeNames.ProjectionFor)?.TypeArguments[0]
            : null;

    /// <summary>
    /// Reads the projection a type defines.
    /// </summary>
    /// <param name="type">The type declaring the projection.</param>
    /// <param name="readModel">The read model the projection builds.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProjectionModel"/>, or <see langword="null"/> when nothing could be read.</returns>
    public ProjectionModel? Read(INamedTypeSymbol type, ITypeSymbol readModel, string location)
    {
        var scope = new FluentScope();

        foreach (var body in Bodies(type))
        {
            _scopes.Read(body, compilation.GetSemanticModel(body.SyntaxTree), scope, location);
        }

        if (scope.IsEmpty)
        {
            return null;
        }

        var attribute = type.GetAttribute(WellKnownTypeNames.ProjectionAttribute);

        return new(
            attribute?.GetArgument(0) as string is { Length: > 0 } id ? id : type.ToDisplayString(),
            readModel.Name,
            scope.Sequence ?? attribute?.GetArgument(1) as string ?? EventLogSequence,
            scope.AutoMap,
            scope.SubscribesToAllEvents,
            scope.ToModel());
    }

    /// <summary>
    /// Gets the bodies of the defining method.
    /// </summary>
    /// <param name="type">The type declaring the projection.</param>
    /// <returns>The bodies, empty when the type has no source.</returns>
    static IEnumerable<SyntaxNode> Bodies(INamedTypeSymbol type) =>
        type.GetMembers(DefineMethod)
            .OfType<IMethodSymbol>()
            .SelectMany(_ => _.DeclaringSyntaxReferences)
            .Select(_ => _.GetSyntax())
            .Select(BodyOf)
            .OfType<SyntaxNode>();

    /// <summary>
    /// Gets the body of a declaration.
    /// </summary>
    /// <param name="node">The declaration to read.</param>
    /// <returns>The body, or <see langword="null"/> when the declaration has none.</returns>
    static SyntaxNode? BodyOf(SyntaxNode node) =>
        node is MethodDeclarationSyntax method ? (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression : null;
}
