// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the keys and mappings declared inside one block of a fluent projection.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
public class FluentBlockReader(ScreenplayDiagnostics diagnostics)
{
    readonly FluentMappings _mappings = new(diagnostics);

    /// <summary>
    /// Gets the mappings the block declares, keyed by the read model property they fill in.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties => _mappings.Properties;

    /// <summary>
    /// Gets the key expression the block declares, if it declares one.
    /// </summary>
    public string? Key { get; private set; }

    /// <summary>
    /// Gets the parent key expression the block declares, if it declares one.
    /// </summary>
    public string? ParentKey { get; private set; }

    /// <summary>
    /// Gets the read model property the block keys a join on, if it declares one.
    /// </summary>
    public string? On { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the block excludes child objects.
    /// </summary>
    public bool ExcludesChildren { get; private set; }

    /// <summary>
    /// Reads every chain declared within a block.
    /// </summary>
    /// <param name="scope">The body of the block.</param>
    /// <param name="semanticModel">The semantic model of the tree the block lives in.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    public void Read(SyntaxNode scope, SemanticModel semanticModel, string location)
    {
        foreach (var chain in ProjectionPaths.ChainsIn(scope))
        {
            ReadChain(InvocationChain.Sequence(chain), semanticModel, location);
        }
    }

    /// <summary>
    /// Builds the expression naming a value from the event context.
    /// </summary>
    /// <param name="argument">The lambda selecting the context property.</param>
    /// <returns>The expression, or <see langword="null"/> when the lambda does not select a property.</returns>
    static string? Context(ExpressionSyntax? argument) =>
        ProjectionPaths.Read(argument) is { } path ? ProjectionExpressions.EventContext(path) : null;

    /// <summary>
    /// Reads one chain of calls, pairing each mapping with the call that gives it a value.
    /// </summary>
    /// <param name="calls">The calls of the chain, in source order.</param>
    /// <param name="semanticModel">The semantic model of the tree the chain lives in.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    void ReadChain(IReadOnlyList<InvocationExpressionSyntax> calls, SemanticModel semanticModel, string location)
    {
        string? pending = null;

        foreach (var call in calls)
        {
            pending = ReadCall(call, pending, semanticModel, location);
        }

        if (pending is not null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                $"The mapping onto '{pending}' was never given a value that could be read, so it was left out",
                location);
        }
    }

    /// <summary>
    /// Reads one call of a chain.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <param name="pending">The read model property waiting for a value, if there is one.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <returns>The read model property still waiting for a value, if there is one.</returns>
    string? ReadCall(InvocationExpressionSyntax call, string? pending, SemanticModel semanticModel, string location)
    {
        var name = InvocationChain.NameOf(call);
        var argument = InvocationChain.ArgumentOf(call);

        switch (name)
        {
            case FluentMappings.Set or FluentMappings.Add or FluentMappings.Subtract:
                return FluentMappings.Begin(name, argument);

            case "To" or "With":
                return _mappings.Complete(pending, ProjectionPaths.Read(argument), location);

            case "ToEventSourceId":
                return _mappings.Complete(pending, ProjectionExpressions.EventSourceId, location);

            case "ToValue":
                return _mappings.Complete(pending, ProjectionExpressions.Value(semanticModel.GetConstantValue(argument!).Value), location);

            case "ToEventContextProperty":
                return _mappings.Complete(pending, Context(argument), location);

            case "Increment" or "Decrement" or "Count":
                _mappings.Counter(name, argument);

                return null;

            default:
                return ReadKey(call, name, argument, pending, semanticModel, location);
        }
    }

    /// <summary>
    /// Reads a call that says how the block is keyed rather than what it maps.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <param name="name">The name of the call.</param>
    /// <param name="argument">The first argument of the call.</param>
    /// <param name="pending">The read model property waiting for a value, if there is one.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <returns>The read model property still waiting for a value, if there is one.</returns>
    string? ReadKey(
        InvocationExpressionSyntax call,
        string name,
        ExpressionSyntax? argument,
        string? pending,
        SemanticModel semanticModel,
        string location)
    {
        switch (name)
        {
            case "UsingKey":
                Key = ProjectionPaths.Read(argument);
                break;

            case "UsingKeyFromContext":
                Key = Context(argument);
                break;

            case "UsingConstantKey":
                Key = ProjectionExpressions.Value(semanticModel.GetConstantValue(argument!).Value);
                break;

            case "UsingParentKey":
                ParentKey = ProjectionPaths.Read(argument);
                break;

            case "UsingCompositeKey":
                Key = FluentCompositeKeys.Read(call, semanticModel);
                break;

            case "On":
                On = ProjectionPaths.ReadDeclared(argument);
                break;

            case "ExcludeChildProjections":
                ExcludesChildren = true;
                break;

            default:
                _mappings.Report(name, location);

                return pending;
        }

        return null;
    }
}
