// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads one scope of a fluent projection - the projection itself, or a child or nested object within it.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
public class FluentScopeReader(ScreenplayDiagnostics diagnostics)
{
    readonly FluentBlocks _blocks = new(diagnostics);

    /// <summary>
    /// Reads every chain declared within a scope.
    /// </summary>
    /// <param name="node">The node holding the scope.</param>
    /// <param name="semanticModel">The semantic model of the tree the scope lives in.</param>
    /// <param name="scope">The scope being collected into.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    public void Read(SyntaxNode node, SemanticModel semanticModel, FluentScope scope, string location)
    {
        foreach (var chain in ProjectionPaths.ChainsIn(node))
        {
            foreach (var call in InvocationChain.Sequence(chain))
            {
                ReadCall(call, semanticModel, scope, location);
            }
        }
    }

    /// <summary>
    /// Reads the expression identifying each instance of a child collection.
    /// </summary>
    /// <param name="body">The body declaring the child collection.</param>
    /// <returns>The expression, or <see langword="null"/> when the body declares none.</returns>
    static string? IdentifiedBy(SyntaxNode body) =>
        body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Where(_ => string.Equals(InvocationChain.NameOf(_), "IdentifiedBy", StringComparison.Ordinal))
            .Select(_ => ProjectionPaths.Read(InvocationChain.ArgumentOf(_)))
            .FirstOrDefault(_ => _ is not null);

    /// <summary>
    /// Reads one call of a scope's chain.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="scope">The scope being collected into.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    void ReadCall(InvocationExpressionSyntax call, SemanticModel semanticModel, FluentScope scope, string location)
    {
        switch (InvocationChain.NameOf(call))
        {
            case "AutoMap":
                scope.AutoMap = ProjectionAutoMapMode.Enabled;
                break;

            case "NoAutoMap":
                scope.AutoMap = ProjectionAutoMapMode.Disabled;
                break;

            case "FromEventSequence":
                scope.Sequence = semanticModel.GetConstantValue(InvocationChain.ArgumentOf(call)!).Value as string;
                break;

            case "From":
                _blocks.ReadFrom(call, semanticModel, scope, location);
                break;

            case "Join":
                _blocks.ReadJoin(call, semanticModel, scope, location);
                break;

            case "FromEvery":
                scope.Every = _blocks.ReadEvery(call, semanticModel, location);
                break;

            case "FromAll":
                scope.Every = _blocks.ReadEvery(call, semanticModel, location);
                scope.SubscribesToAllEvents = true;
                break;

            case "RemovedWith":
                _blocks.Remove(call, semanticModel, scope.RemovedWith, location);
                break;

            case "RemovedWithJoin":
                _blocks.Remove(call, semanticModel, scope.RemovedWithJoin, location);
                break;

            case "Children":
                ReadChild(call, semanticModel, scope.Children, location, identified: true);
                break;

            case "Nested":
                ReadChild(call, semanticModel, scope.Nested, location, identified: false);
                break;

            case "Passive" or "NotRewindable" or "ContainerName" or "WithInitialValues":
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                    $"'{InvocationChain.NameOf(call)}' configures how the projection runs, which a document does not describe",
                    location);
                break;

            case "" or "IdentifiedBy" or "FromEventProperty":
                break;

            default:
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                    $"'{InvocationChain.NameOf(call)}' has no counterpart in the projection definition language and was left out",
                    location);
                break;
        }
    }

    /// <summary>
    /// Reads a child collection or a nested object.
    /// </summary>
    /// <param name="call">The call declaring the scope.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="scopes">The scopes being collected into.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <param name="identified">Whether each instance of the scope is identified by an expression.</param>
    void ReadChild(
        InvocationExpressionSyntax call,
        SemanticModel semanticModel,
        IList<ProjectionChildScopeModel> scopes,
        string location,
        bool identified)
    {
        var property = ProjectionPaths.ReadDeclared(InvocationChain.ArgumentOf(call));
        var body = InvocationChain.ArgumentOf(call, 1);
        if (property is null || body is null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                "A child or nested object does not name the property it fills in directly, so it was left out",
                location);

            return;
        }

        var inner = new FluentScope();
        Read(body, semanticModel, inner, location);

        scopes.Add(new(property, identified ? IdentifiedBy(body) ?? string.Empty : string.Empty, inner.AutoMap, inner.ToModel()));
    }
}
