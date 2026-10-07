// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>Rejects ambiguous authoring declarations before any command references them.</summary>
public static class AuthoringDeclarations
{
    /// <summary>Retains only uniquely nameable operation systems and compatible source-owned streams.</summary>
    /// <param name="model">The whole application.</param>
    /// <param name="diagnostics">Where ambiguity is reported.</param>
    /// <returns>The model with ambiguous authoring intent omitted.</returns>
    public static ApplicationModel Resolve(ApplicationModel model, ScreenplayDiagnostics diagnostics)
    {
        var commands = model.Slices.SelectMany(slice => slice.Commands).ToArray();
        var systems = commands.SelectMany(command => command.Authoring?.Operations ?? []).GroupBy(operation => operation.System, StringComparer.Ordinal)
            .Where(group => group.Select(operation => operation.SystemTypeIdentity).Distinct(StringComparer.Ordinal).Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var routes = commands.Select(command => command.Authoring?.Route).OfType<CommandRouteModel>().ToArray();
        var conflictingSources = routes.GroupBy(route => route.Source, StringComparer.Ordinal)
            .Where(source => source.Select(route => route.IdentifierType).OfType<TypeReferenceModel>().Distinct().Count() > 1 ||
                source.GroupBy(route => route.Stream, StringComparer.Ordinal).Any(stream => stream.Select(route => route.StreamIdType).Distinct().Count() > 1))
            .Select(group => group.Key).ToHashSet(StringComparer.Ordinal);

        var reads = commands.SelectMany(command => command.Authoring?.Reads ?? []).ToArray();
        var projections = model.Slices.SelectMany(slice => slice.Projections.Select(projection => (projection.ReadModel, slice.Namespace))).ToArray();
        var readOwners = reads.GroupBy(read => read.Name, StringComparer.Ordinal)
            .Where(group => group.All(read => read.Properties.Count > 0 && read.Namespace == group.First().Namespace && read.Properties.SequenceEqual(group.First().Properties)) &&
                projections.Count(projection => projection.ReadModel == group.Key) == 1)
            .ToDictionary(group => group.Key, group => projections.Single(projection => projection.ReadModel == group.Key).Namespace, StringComparer.Ordinal);

        var resolved = model with
        {
            Slices = model.Slices.Select(slice => ResolveSlice(slice, systems, conflictingSources, readOwners, diagnostics)).ToList()
        };

        return RemoveOrphans(model, resolved);
    }

    /// <summary>Removes declarations used only by authoring intent that was withheld.</summary>
    /// <param name="original">The model before authoring admission.</param>
    /// <param name="resolved">The model after authoring admission.</param>
    /// <returns>The admitted model without orphan declarations.</returns>
    public static ApplicationModel RemoveOrphans(ApplicationModel original, ApplicationModel resolved)
    {
        var candidates = ReachableTypes(AuthoringTypes(original), original);
        var retained = ReachableTypes(RootTypes(resolved).Concat(resolved.Types.Where(type => !candidates.Contains(type.Name)).Select(type => type.Name)), resolved);
        candidates.ExceptWith(retained);

        return resolved with
        {
            Concepts = resolved.Concepts.Where(concept => !candidates.Contains(concept.Name)).ToList(),
            Types = resolved.Types.Where(type => !candidates.Contains(type.Name)).ToList()
        };
    }

    static HashSet<string> ReachableTypes(IEnumerable<string> roots, ApplicationModel model)
    {
        var reached = roots.ToHashSet(StringComparer.Ordinal);
        var pending = new Queue<string>(reached);
        while (pending.TryDequeue(out var name))
        {
            foreach (var property in model.Types.Where(type => type.Name == name).SelectMany(type => type.Properties))
            {
                if (reached.Add(property.Type.Name))
                {
                    pending.Enqueue(property.Type.Name);
                }
            }
        }

        return reached;
    }

    static IEnumerable<string> AuthoringTypes(ApplicationModel model) => model.Slices.SelectMany(slice => slice.Commands)
        .SelectMany(command => command.Authoring is { } authoring
            ? authoring.Generated.Select(property => property.Type.Name)
                .Concat(authoring.Reads.SelectMany(read => read.Properties).Select(property => property.Type.Name))
                .Concat(authoring.Operations.SelectMany(operation => operation.Inputs).Select(property => property.Type.Name))
                .Concat(authoring.Route is { } route ? new[] { route.IdentifierType?.Name, route.StreamIdType?.Name }.OfType<string>() : [])
            : []);

    static IEnumerable<string> RootTypes(ApplicationModel model) => AuthoringTypes(model)
        .Concat(model.Slices.SelectMany(slice => slice.Events).SelectMany(@event => @event.Properties).Select(property => property.Type.Name))
        .Concat(model.Slices.SelectMany(slice => slice.Commands).SelectMany(command => command.Properties).Select(property => property.Type.Name))
        .Concat(model.Slices.SelectMany(slice => slice.Queries).SelectMany(query => query.Filters.Concat(query.By is { } by ? [by] : [])
            .Select(property => property.Type.Name).Prepend(query.ReturnType.Name)))
        .Concat(model.Slices.SelectMany(slice => slice.Screens).SelectMany(screen => screen.Data).Select(data => data.Type.Name))
        .Concat(model.Slices.SelectMany(slice => slice.ReadModels).SelectMany(readModel => readModel.Properties).Select(property => property.Type.Name));

    static bool ReferencesRead(string path, HashSet<string> aliases) => aliases.Contains(path.Split('.')[0]);

    static bool ReferencesRead(ConditionModel condition, HashSet<string> aliases) => condition switch
    {
        ComparisonCondition comparison => ReferencesRead(comparison.Left, aliases) || (comparison.Right is PropertyPathSource path && ReferencesRead(path.Path, aliases)),
        LogicalCondition logical => ReferencesRead(logical.Left, aliases) || ReferencesRead(logical.Right, aliases),
        _ => false
    };

    static SliceModel ResolveSlice(SliceModel slice, HashSet<string> systems, HashSet<string> sources, Dictionary<string, string> readOwners, ScreenplayDiagnostics diagnostics)
    {
        var declared = slice.Events.Select(e => e.Name).Concat(slice.Commands.Select(command => command.Name)).ToHashSet(StringComparer.Ordinal);
        var duplicateOperations = slice.Commands.SelectMany(command => command.Authoring?.Operations ?? []).GroupBy(operation => operation.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var commands = slice.Commands.Select(command =>
        {
            if (command.Authoring is not { } authoring)
            {
                return command;
            }

            var unavailableReads = authoring.Reads.Where(read => !readOwners.ContainsKey(read.Name)).Select(read => read.Alias).ToHashSet(StringComparer.Ordinal);
            var operations = authoring.Operations.Where(operation =>
            {
                if (systems.Contains(operation.System) || declared.Contains(operation.Name) || duplicateOperations.Contains(operation.Name))
                {
                    diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandOperation, $"Command '{command.Name}': operation '{operation.Name}' or system '{operation.System}' has ambiguous ownership and was left out", slice.Namespace);
                    return false;
                }

                if (operation.Mappings.Any(mapping => mapping.Source is PropertyPathSource path && ReferencesRead(path.Path, unavailableReads)))
                {
                    diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandOperation, $"Command '{command.Name}': operation '{operation.Name}' depends on an unavailable read and was left out", slice.Namespace);
                    return false;
                }

                return true;
            }).ToList();
            var route = authoring.Route;
            if (route is not null && sources.Contains(route.Source))
            {
                diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandRoute, $"Command '{command.Name}': source '{route.Source}' has incompatible identity or stream-id types and its routes were left out", slice.Namespace);
                route = null;
            }

            if (unavailableReads.Count > 0)
            {
                diagnostics.Information(ScreenplayDiagnosticCodes.UnreadableCommandProvisioning, $"Command '{command.Name}': a read model has no unique readable shape and projection in this document; its dependency, requirements and mappings were left in code", slice.Namespace);
            }

            var keptReads = authoring.Reads.Where(read => readOwners.ContainsKey(read.Name)).Select(read => read with { Namespace = readOwners[read.Name] }).ToList();
            return command with
            {
                Produces = command.Produces.Select(production => production with
                {
                    Mappings = production.Mappings.Where(mapping => mapping.Source is not PropertyPathSource path || !ReferencesRead(path.Path, unavailableReads)).ToList()
                }).ToList(),
                Authoring = authoring with
                {
                    Operations = operations,
                    Route = route,
                    Reads = keptReads,
                    Requirements = authoring.Requirements.Where(requirement => !ReferencesRead(requirement.Condition, unavailableReads)).ToList()
                }
            };
        }).ToList();

        return slice with { Commands = commands };
    }
}
