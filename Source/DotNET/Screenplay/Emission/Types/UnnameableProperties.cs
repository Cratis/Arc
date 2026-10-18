// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Types;

/// <summary>
/// Omits unnameable properties without claiming dependent behavior over a different schema.
/// </summary>
/// <param name="diagnostics">Where property omissions and dependent constructs are reported.</param>
internal sealed class UnnameableProperties(ScreenplayDiagnostics diagnostics)
{
    IReadOnlySet<string> _empty = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Removes properties and withholds behavior requiring their complete schema.
    /// </summary>
    /// <param name="model">The original application.</param>
    /// <returns>The representable partial application.</returns>
    internal ApplicationModel Apply(ApplicationModel model)
    {
        _empty = EmptyShapes.In(model);
        var incomplete = new IncompleteShapes(model);
        var slices = model.Slices.Select(slice => Declarations(slice, incomplete)).ToList();
        var commands = model.Slices.SelectMany(slice => slice.Commands).Select(command => command.Name)
            .Except(slices.SelectMany(slice => slice.Commands).Select(command => command.Name), StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var queries = model.Slices.SelectMany(slice => slice.Queries).Select(query => query.Name)
            .Except(slices.SelectMany(slice => slice.Queries).Select(query => query.Name), StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        slices = slices.ConvertAll(slice => Reactors(slice, incomplete, commands));
        var reactors = model.Slices.SelectMany(slice => slice.Reactors).Select(reactor => reactor.Name)
            .Except(slices.SelectMany(slice => slice.Reactors).Select(reactor => reactor.Name), StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        return model with
        {
            Types = model.Types.Select(type => type with { Properties = Properties(type.Properties, type.Name) }).Where(type => !_empty.Contains(type.Name)).ToList(),
            Slices = slices.ConvertAll(slice => Dependents(slice, incomplete, commands, queries, reactors))
        };
    }

    static bool HasAuthoringBehavior(CommandAuthoringModel? authoring) => authoring is not null &&
        (authoring.Response is not null || authoring.ResponseFields.Count > 0 || authoring.Generated.Count > 0 ||
        authoring.Reads.Count > 0 || authoring.Requirements.Count > 0 || authoring.Operations.Count > 0 || authoring.Route is not null);

    static bool ConstraintAffected(ConstraintModel constraint, IncompleteShapes incomplete) =>
        constraint switch
        {
            UniquePropertyConstraintModel property => incomplete.Contains(property.EventName) || constraint.AdditionalRules.Any(rule => ConstraintAffected(rule, incomplete)),
            UniqueEventConstraintModel @event => incomplete.Contains(@event.EventName) || constraint.AdditionalRules.Any(rule => ConstraintAffected(rule, incomplete)),
            _ => constraint.AdditionalRules.Any(rule => ConstraintAffected(rule, incomplete))
        } || constraint.ReleasedBy.Any(incomplete.Contains);

    SliceModel Declarations(SliceModel slice, IncompleteShapes incomplete)
    {
        var commands = slice.Commands.Select(command =>
        {
            var properties = Properties(command.Properties, command.Name);
            var depends = incomplete.Contains(command.Name) || command.Produces.Any(production => incomplete.Contains(production.EventName)) ||
                command.Authoring?.Reads.Any(read => incomplete.Contains(read.Name)) == true ||
                command.Authoring?.Operations.Any(operation => incomplete.Affects(operation.Inputs)) == true;

            // Do not remove validations, provisioning guards or mappings and silently call the command equivalent.
            var declarative = command.Produces.Any() || command.Validations.Any() || HasAuthoringBehavior(command.Authoring) ||
                command.Concurrency is not null || command.Identifier is not null || command.EventSource is not null;

            return Keep(!depends || !declarative, $"Command '{command.Name}'", slice.Namespace)
                ? command with { Properties = properties } : null;
        }).OfType<CommandModel>().ToList();
        var queries = slice.Queries.Where(query => Keep(
            !_empty.Contains(query.ReturnType.Name) && !incomplete.Affects(query.Filters) && (query.By is null || !incomplete.Affects([query.By])),
            $"Query '{query.Name}'",
            slice.Namespace)).ToList();

        return slice with
        {
            Commands = commands,
            Events = slice.Events.Select(@event => @event with { Properties = Properties(@event.Properties, @event.Name) }).ToList(),
            ReadModels = slice.ReadModels.Select(readModel => readModel with { Properties = Properties(readModel.Properties, readModel.Name) }).Where(readModel => !_empty.Contains(readModel.Name)).ToList(),
            Queries = queries,
            Projections = slice.Projections.Where(projection => Keep(
                !incomplete.Contains(projection.ReadModel) && !ProjectionEvents.In(projection).Any(incomplete.Contains),
                $"Projection '{projection.Identifier}'",
                slice.Namespace)).ToList(),
            Constraints = slice.Constraints.Where(constraint => Keep(
                !ConstraintAffected(constraint, incomplete),
                $"Constraint '{constraint.Name}'",
                slice.Namespace)).ToList()
        };
    }

    SliceModel Reactors(SliceModel slice, IncompleteShapes incomplete, HashSet<string> commands)
    {
        var reactors = slice.Reactors.Where(reactor => Keep(
            !reactor.ObservedEvents.Any(incomplete.Contains) && !reactor.Reactions.Any(reaction => reaction.Produces.Any(production => incomplete.Contains(production.EventName)) || reaction.Invokes.Any(invocation => commands.Contains(invocation.CommandName) || incomplete.Contains(invocation.CommandName))),
            $"Reactor '{reactor.Name}'",
            slice.Namespace)).ToList();

        return slice with { Reactors = reactors };
    }

    SliceModel Dependents(SliceModel slice, IncompleteShapes incomplete, HashSet<string> commands, HashSet<string> queries, HashSet<string> reactors) =>
        slice with
        {
            Screens = slice.Screens.Select(screen => Screen(screen, incomplete, commands, queries, slice.Namespace)).ToList(),
            Specifications = slice.Specifications.Where(specification => KeepScenario(specification, incomplete, commands, queries, reactors, slice.Namespace)).ToList()
        };

    ScreenModel Screen(ScreenModel screen, IncompleteShapes incomplete, HashSet<string> commands, HashSet<string> queries, string location)
    {
        bool Action(string name) => Keep(!commands.Contains(name) && !incomplete.Contains(name), $"Screen '{screen.Name}' action '{name}'", location);
        var actions = screen.Actions.Where(Action).ToList();

        return screen with
        {
            Data = screen.Data.Where(data => Keep(!incomplete.Contains(data.Type.Name) && !queries.Contains(data.Query), $"Screen '{screen.Name}' data '{data.Query}'", location)).ToList(),
            Tables = screen.Tables.Where(table => Keep(!incomplete.Contains(table.ReadModel), $"Screen '{screen.Name}' table '{table.ReadModel}'", location)).ToList(),
            Actions = actions,
            Toolbars = screen.Toolbars.Select(toolbar => toolbar with { Items = toolbar.Items.Where(item => item.Command.Length == 0 || Action(item.Command)).ToList() }).Where(toolbar => toolbar.Items.Any()).ToList(),
            AfterSuccess = screen.AfterSuccess.Where(pair => actions.Contains(pair.Key, StringComparer.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
        };
    }

    bool KeepScenario(SpecificationModel specification, IncompleteShapes incomplete, HashSet<string> commands, HashSet<string> queries, HashSet<string> reactors, string location)
    {
        var states = specification.Given.Concat(specification.Then).Concat(specification.When is { } issued ? [issued] : []);
        if (!states.Any(state => incomplete.Contains(state.Name) || commands.Contains(state.Name) || queries.Contains(state.Name)) &&
            (specification.Reactor is null || !reactors.Contains(specification.Reactor)))
        {
            return true;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnreadableSpecification,
            $"The scenario '{specification.Name}' was left out because it depends on a schema with omitted unnameable properties or behavior withheld for that schema; its fixtures and assertions were not weakened",
            location);

        return false;
    }

    IReadOnlyList<PropertyModel> Properties(IEnumerable<PropertyModel> properties, string owner) => TypeReferenceConverter.Properties(
        properties.Select(property => _empty.Contains(property.Type.Name)
            ? property with { Type = property.Type with { UnnameableType = $"{property.Type.Name} (its shape has no representable properties)" } }
            : property),
        owner,
        diagnostics);

    bool Keep(bool keep, string construct, string location)
    {
        if (!keep)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnmappableTypeReference,
                $"{construct} was left out because it depends on a schema with omitted unnameable properties; its behavior was not restated over a partial schema",
                location);
        }

        return keep;
    }
}
