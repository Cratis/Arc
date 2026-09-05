// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the blocks of a fluent projection that observe or remove, as opposed to the ones that nest.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Each of these turns one call and the body it was given into one block, and none of them recurses. Keeping them
/// apart from the scope reader leaves that reader with only the two things that do recurse - a child collection and
/// a nested object.
/// </remarks>
public class FluentBlocks(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Gets the name of the event type a call names as its type argument.
    /// </summary>
    /// <param name="call">The call to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <returns>The name, or <see langword="null"/> when the call names none.</returns>
    public static string? EventTypeOf(InvocationExpressionSyntax call, SemanticModel semanticModel) =>
        InvocationChain.TypeArgumentOf(call) is { } argument ? semanticModel.GetTypeInfo(argument).Type?.Name : null;

    /// <summary>
    /// Reads a block observing an event type.
    /// </summary>
    /// <param name="call">The call declaring the block.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="scope">The scope being collected into.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    public void ReadFrom(InvocationExpressionSyntax call, SemanticModel semanticModel, FluentScope scope, string location)
    {
        var eventType = EventTypeOf(call, semanticModel);
        if (eventType is null)
        {
            return;
        }

        var block = ReadBlock(InvocationChain.ArgumentOf(call), semanticModel, location);

        scope.From.Add(new([eventType], block.Key, block.ParentKey, block.Properties));
    }

    /// <summary>
    /// Reads a block joining data from another event.
    /// </summary>
    /// <param name="call">The call declaring the block.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="scope">The scope being collected into.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <remarks>
    /// The fluent form names no property to hold the joined data, so the join is named after the first property it
    /// fills in - which is the closest the declaration comes to saying what the joined data is.
    /// </remarks>
    public void ReadJoin(InvocationExpressionSyntax call, SemanticModel semanticModel, FluentScope scope, string location)
    {
        var eventType = EventTypeOf(call, semanticModel);
        if (eventType is null)
        {
            return;
        }

        var block = ReadBlock(InvocationChain.ArgumentOf(call), semanticModel, location);
        var named = block.Properties.Keys.Order(StringComparer.Ordinal).FirstOrDefault() ?? block.On;

        scope.Joins.Add(new(named ?? string.Empty, eventType, block.On ?? string.Empty, block.Properties));
    }

    /// <summary>
    /// Reads a block applying to every observed event.
    /// </summary>
    /// <param name="call">The call declaring the block.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProjectionEveryModel"/>.</returns>
    public ProjectionEveryModel ReadEvery(InvocationExpressionSyntax call, SemanticModel semanticModel, string location)
    {
        var block = ReadBlock(InvocationChain.ArgumentOf(call), semanticModel, location);

        return new(block.Properties, !block.ExcludesChildren, ProjectionAutoMapMode.Inherit);
    }

    /// <summary>
    /// Reads a block removing an instance when an event occurs.
    /// </summary>
    /// <param name="call">The call declaring the block.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <param name="removals">The removals being collected into.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    public void Remove(
        InvocationExpressionSyntax call,
        SemanticModel semanticModel,
        IList<ProjectionRemoveModel> removals,
        string location)
    {
        var eventType = EventTypeOf(call, semanticModel);
        if (eventType is null)
        {
            return;
        }

        var block = ReadBlock(InvocationChain.ArgumentOf(call), semanticModel, location);

        removals.Add(new(eventType, block.Key, block.ParentKey));
    }

    /// <summary>
    /// Reads the mappings and keys of one block.
    /// </summary>
    /// <param name="body">The body of the block.</param>
    /// <param name="semanticModel">The semantic model of the tree the block lives in.</param>
    /// <param name="location">Where the projection lives, for use in diagnostics.</param>
    /// <returns>The <see cref="FluentBlockReader"/> holding what was read.</returns>
    FluentBlockReader ReadBlock(ExpressionSyntax? body, SemanticModel semanticModel, string location)
    {
        var block = new FluentBlockReader(diagnostics);
        if (body is not null)
        {
            block.Read(body, semanticModel, location);
        }

        return block;
    }
}
