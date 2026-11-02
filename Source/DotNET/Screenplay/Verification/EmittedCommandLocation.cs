// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>Matches printed command locations to source analysis without guessing across scopes.</summary>
internal static class EmittedCommandLocation
{
    /// <summary>Finds the unique analyzed command containing a printed line.</summary>
    /// <param name="application">The parsed printed application.</param>
    /// <param name="line">The diagnostic line.</param>
    /// <param name="model">The model used for emission.</param>
    /// <param name="options">The resolved emission options.</param>
    /// <returns>The unique owning command and slice, or null.</returns>
    internal static (SliceModel Slice, CommandModel Command, CommandSyntax Printed)? Find(ApplicationSyntax application, int line, ApplicationModel model, ScreenplayOptions options)
    {
        var commands = application.Modules.SelectMany(module => Commands(module.Features, [module.Name])).ToArray();
        var boundaries = application.Modules.SelectMany(module => Boundaries(module.Features).Prepend(module.Location.Line)).Order().ToArray();
        var owners = commands.Where(placed => placed.Command.Location.Line <= line &&
            line < boundaries.FirstOrDefault(start => start > placed.Command.Location.Line, int.MaxValue)).ToArray();
        if (owners.Length != 1)
        {
            return null;
        }

        var owner = owners[0];
        var naming = new ScreenplayNaming();
        var tree = new SliceTreeBuilder(naming);
        var candidates = model.Slices.SelectMany(slice =>
        {
            var placed = new PlacedSlice(slice.Namespace, owner.Slice with { Name = SliceSyntaxBuilder.NameOf(slice, naming) });
            var modules = options.ModulesFromNamespaceRoots
                ? tree.BuildPerNamespaceRoot([placed], naming.ToDeclarationName(model.Domain), options.SegmentsToSkip ?? 0)
                : tree.Build([placed], naming.ToDeclarationName(model.Module), options.SegmentsToSkip ?? 0);
            var matches = modules.SelectMany(module => Commands(module.Features, [module.Name])).Any(command => command.Path.SequenceEqual(owner.Path));
            return matches ? slice.Commands.Where(command => naming.ToDeclarationName(command.Name) == owner.Command.Name).Select(command => (Slice: slice, Command: command, Printed: owner.Command)) : [];
        }).ToArray();

        return candidates.Length == 1 ? candidates[0] : null;
    }

    static IEnumerable<(string[] Path, SliceSyntax Slice, CommandSyntax Command)> Commands(IEnumerable<FeatureSyntax> features, string[] parents) =>
        features.SelectMany(feature => feature.Slices.SelectMany(slice => slice.Commands.Select(command =>
            (Path: (string[])[.. parents, feature.Name, slice.Name, command.Name], Slice: slice, Command: command)))
            .Concat(Commands(feature.Features, [.. parents, feature.Name])));

    static IEnumerable<int> Boundaries(IEnumerable<FeatureSyntax> features) => features.SelectMany(feature =>
        feature.Slices.SelectMany(slice => slice.Commands.Select(node => node.Location.Line)
            .Concat(slice.Events.Select(node => node.Location.Line))
            .Concat(slice.Queries.Select(node => node.Location.Line))
            .Concat(slice.Projections.Select(node => node.Location.Line))
            .Concat(slice.Reactions.Select(node => node.Location.Line))
            .Concat(slice.Screens.Select(node => node.Location.Line))
            .Concat(slice.Constraints.Select(node => node.Location.Line))
            .Concat(slice.Specifications.Select(node => node.Location.Line))
            .Concat((slice.ReadModels ?? []).Select(node => node.Location.Line))
            .Concat((slice.Reducers ?? []).Select(node => node.Location.Line))
            .Concat(slice.Operations.Select(node => node.Location.Line))
            .Prepend(slice.Location.Line))
            .Concat(Boundaries(feature.Features)).Prepend(feature.Location.Line));
}
