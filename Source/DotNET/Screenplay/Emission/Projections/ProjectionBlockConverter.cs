// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Projections;

/// <summary>
/// Converts the scopes of a projection into Screenplay projection blocks.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="readModelName">The name of the read model, used when a composite key carries no type name.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <param name="location">Where the projection lives, for use in diagnostics.</param>
/// <param name="names">The <see cref="NameAvailability"/> deciding which properties a block can map onto.</param>
public class ProjectionBlockConverter(
    IScreenplayNaming naming,
    string readModelName,
    ScreenplayDiagnostics diagnostics,
    string location,
    NameAvailability names)
{
    readonly ProjectionMappingConverter _mappings = new(naming, diagnostics, location, readModelName, names);
    readonly ProjectionJoinConverter _joins = new(naming, new(naming, diagnostics, location, readModelName, names), diagnostics, location);

    /// <summary>
    /// Converts the model's auto map setting into the Screenplay one.
    /// </summary>
    /// <param name="autoMap">The model setting.</param>
    /// <returns>The Screenplay setting.</returns>
    public static AutoMapMode ToAutoMapMode(ProjectionAutoMapMode autoMap) => autoMap switch
    {
        ProjectionAutoMapMode.Disabled => AutoMapMode.Disabled,
        ProjectionAutoMapMode.Enabled => AutoMapMode.Enabled,
        _ => AutoMapMode.Inherit
    };

    /// <summary>
    /// Converts a scope into the blocks it declares.
    /// </summary>
    /// <param name="scope">The scope to convert.</param>
    /// <param name="subscribesToAllEvents">Whether the projection observes every event type in the system.</param>
    /// <returns>The blocks, in a deterministic order.</returns>
    public IEnumerable<ProjectionBlockSyntax> Convert(ProjectionScopeModel scope, bool subscribesToAllEvents = false)
    {
        var blocks = new List<ProjectionBlockSyntax>();

        AddEveryOrAll(blocks, scope.Every, subscribesToAllEvents);
        blocks.AddRange(scope.From.Select(ToFrom));
        blocks.AddRange(_joins.Convert(scope.Joins));
        blocks.AddRange(ToChildren(scope.Children));
        blocks.AddRange(ToNested(scope.Nested));
        blocks.AddRange(scope.RemovedWith.Select(ToRemoveWith));
        blocks.AddRange(scope.RemovedWithJoin.Select(ToRemoveViaJoin));

        return blocks;
    }

    /// <summary>
    /// Reduces a key back to a plain expression, which is all the removal blocks accept.
    /// </summary>
    /// <param name="key">The key to reduce.</param>
    /// <returns>The expression, or <see langword="null"/>.</returns>
    static ExpressionSyntax? ToExpression(KeySyntax? key) =>
        key is ExpressionKeySyntax expressionKey ? expressionKey.Expression : null;

    /// <summary>
    /// Adds the <c>every</c> or <c>all</c> block when the scope declares one.
    /// </summary>
    /// <param name="blocks">The blocks collected so far.</param>
    /// <param name="every">The block to add.</param>
    /// <param name="subscribesToAllEvents">Whether the projection observes every event type in the system.</param>
    void AddEveryOrAll(List<ProjectionBlockSyntax> blocks, ProjectionEveryModel? every, bool subscribesToAllEvents)
    {
        if (every is null)
        {
            return;
        }

        var mappings = _mappings.Convert(every.Properties, ReservedWords.None);
        var autoMap = ToAutoMapMode(every.AutoMap);

        blocks.Add(subscribesToAllEvents
            ? new AllSyntax(mappings, autoMap, SourceLocation.Start)
            : new EverySyntax(mappings, every.IncludeChildren, autoMap, SourceLocation.Start));
    }

    /// <summary>
    /// Converts a block observing specific event types.
    /// </summary>
    /// <param name="from">The block to convert.</param>
    /// <returns>The <see cref="FromSyntax"/>.</returns>
    FromSyntax ToFrom(ProjectionFromModel from) =>
        new(
            [.. from.EventTypes.Select(_ => new EventSpecSyntax(naming.ToDeclarationName(_), null, SourceLocation.Start))],
            ConvertKey(from.Key),
            ProjectionKeyConverter.ConvertParent(from.ParentKey),
            _mappings.Convert(from.Properties, ReservedWords.InFrom),
            SourceLocation.Start);

    /// <summary>
    /// Converts a key, reporting one that could not be expressed rather than silently falling back to the default.
    /// </summary>
    /// <param name="key">The key expression to convert.</param>
    /// <returns>The <see cref="KeySyntax"/>, or <see langword="null"/> when the default key applies.</returns>
    KeySyntax? ConvertKey(string? key)
    {
        var converted = ProjectionKeyConverter.Convert(key, readModelName);
        if (converted is null && !ProjectionKeyConverter.IsDefault(key))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionKey,
                $"The key '{key}' has no counterpart in the projection definition language, so the event source id is used instead",
                location);
        }

        return converted;
    }

    /// <summary>
    /// Converts the child collections of a scope, leaving out any that would produce an empty body.
    /// </summary>
    /// <param name="children">The child scopes to convert.</param>
    /// <returns>The <see cref="ChildrenSyntax"/> blocks.</returns>
    IEnumerable<ProjectionBlockSyntax> ToChildren(IEnumerable<ProjectionChildScopeModel> children)
    {
        foreach (var child in children)
        {
            var blocks = Convert(child.Scope).ToList();
            if (blocks.Count == 0 || !ProjectionExpressionConverter.TryConvert(child.IdentifiedBy, out var identifiedBy))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableProjectionScope,
                    $"The children of '{child.Property}' declare nothing expressible, or are identified by '{child.IdentifiedBy}' which has no counterpart in the projection definition language",
                    location);
                continue;
            }

            yield return new ChildrenSyntax(
                naming.ToPropertyName(child.Property),
                identifiedBy,
                ToAutoMapMode(child.AutoMap),
                blocks,
                SourceLocation.Start);
        }
    }

    /// <summary>
    /// Converts the nested objects of a scope, leaving out any without a from block since those do not compile.
    /// </summary>
    /// <param name="nested">The nested scopes to convert.</param>
    /// <returns>The <see cref="NestedSyntax"/> blocks.</returns>
    IEnumerable<ProjectionBlockSyntax> ToNested(IEnumerable<ProjectionChildScopeModel> nested)
    {
        foreach (var child in nested)
        {
            var blocks = Convert(child.Scope).ToList();
            if (!blocks.OfType<FromSyntax>().Any())
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableProjectionScope,
                    $"The nested object '{child.Property}' observes no event type and was left out",
                    location);
                continue;
            }

            yield return new NestedSyntax(
                naming.ToPropertyName(child.Property),
                ToAutoMapMode(child.AutoMap),
                blocks,
                SourceLocation.Start);
        }
    }

    /// <summary>
    /// Converts a removal block.
    /// </summary>
    /// <param name="remove">The block to convert.</param>
    /// <returns>The <see cref="RemoveWithSyntax"/>.</returns>
    RemoveWithSyntax ToRemoveWith(ProjectionRemoveModel remove) =>
        new(
            naming.ToDeclarationName(remove.EventType),
            ToExpression(ConvertKey(remove.Key)),
            ProjectionKeyConverter.ConvertParent(remove.ParentKey),
            SourceLocation.Start);

    /// <summary>
    /// Converts a removal block that goes through a join.
    /// </summary>
    /// <param name="remove">The block to convert.</param>
    /// <returns>The <see cref="RemoveViaJoinSyntax"/>.</returns>
    RemoveViaJoinSyntax ToRemoveViaJoin(ProjectionRemoveModel remove) =>
        new(
            naming.ToDeclarationName(remove.EventType),
            ToExpression(ConvertKey(remove.Key)),
            SourceLocation.Start);
}
