// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Reserves the document's complete declaration namespace before unused concepts can contribute names.
/// </summary>
static class DeclarationNames
{
    /// <summary>
    /// Enumerates declarations, including those derived from authoring, routing and availability metadata.
    /// </summary>
    /// <param name="model">The completed application before unused declarations are added.</param>
    /// <param name="segmentsToSkip">The namespace prefix omitted from the emitted feature tree.</param>
    /// <returns>The names already owned by the document.</returns>
    public static IEnumerable<string> In(ApplicationModel model, int segmentsToSkip) =>
    [
        model.Domain,
        model.Module,
        .. model.Concepts.Select(concept => concept.Name),
        .. model.Types.Select(type => type.Name),
        .. model.Policies.Select(policy => PolicyNames.For(policy.Name)),
        .. model.ContextValues.SelectMany(value => value.Scope.Append(value.Name)),
        .. model.Imports.Select(LastSegment),
        .. model.Slices.SelectMany(slice => InSlice(slice, segmentsToSkip)),
        .. Routes(model).SelectMany(route => new[] { route.Source, route.Stream }.OfType<string>())
    ];

    static IEnumerable<string> InSlice(SliceModel slice, int segmentsToSkip) =>
    [
        slice.Name,
        .. FeatureNames(slice.Namespace, segmentsToSkip),
        .. slice.Events.Select(@event => @event.Name),
        .. slice.Commands.Select(command => command.Name),
        .. slice.Queries.Select(query => query.Name),
        .. slice.ReadModels.Select(readModel => readModel.Name),
        .. slice.Projections.Select(ProjectionName),
        .. slice.Reactors.Select(reactor => reactor.Name),
        .. slice.Constraints.Select(constraint => constraint.Name),
        .. slice.Screens.Select(screen => screen.Name),
        .. slice.Specifications.Select(specification => specification.Name),
        .. slice.Commands.SelectMany(command => AuthoringNames(command.Authoring)),
        .. AvailabilityNames(slice),
        .. AuthorizationNames(slice)
    ];

    static string[] FeatureNames(string @namespace, int segmentsToSkip)
    {
        var segments = @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Skip(segmentsToSkip).ToArray();
        return segments.Length > 1 ? segments[..^1] : segments;
    }

    static IEnumerable<string> AuthoringNames(CommandAuthoringModel? authoring) =>
        authoring is null ? [] :
        [
            .. authoring.Reads.Select(read => read.Name),
            .. authoring.Operations.SelectMany(operation => new[] { operation.Name, operation.System }),
            .. authoring.Operations.SelectMany(operation => operation.ExecuteInvocations.Concat(operation.CompensateInvocations))
                .SelectMany(invocation => new[] { invocation.System, invocation.Ability })
        ];

    static IEnumerable<string> AvailabilityNames(SliceModel slice) =>
        slice.Commands.Select(command => command.Availability).Concat(slice.Queries.Select(query => query.Availability))
            .OfType<AvailabilityModel>().SelectMany(availability => availability.Gates)
            .SelectMany(gate => gate.Audiences.Append(gate.Availability).OfType<string>());

    static IEnumerable<string> AuthorizationNames(SliceModel slice) =>
        slice.Commands.Select(command => command.Authorization).Concat(slice.Queries.Select(query => query.Authorization))
            .OfType<AuthorizationModel>().Where(authorization => authorization.RequiresAuthentication)
            .SelectMany(authorization => authorization.Roles.Concat(authorization.Policies)
                .Select(PolicyNames.For).Where(name => name.Length > 0).DefaultIfEmpty(AuthorizeSyntaxBuilder.AuthenticatedPolicy));

    static IEnumerable<CommandRouteModel> Routes(ApplicationModel model)
    {
        var commands = model.Slices.SelectMany(slice => slice.Commands).SelectMany(ProductionRouteAdmission.Routes);
        var occurrences = model.Slices.SelectMany(slice => slice.Specifications)
            .SelectMany(specification => specification.Given.Concat(specification.Then).Concat(specification.When is { } action ? [action] : []));
        return commands.Concat(SpecificationRouteDeclarations.Read(model, occurrences)).Concat(EventSourceDeclarations.ObservedRoutes(model));
    }

    static string ProjectionName(ProjectionModel projection)
    {
        var name = new ScreenplayNaming().ToDeclarationName(LastSegment(projection.Identifier));
        return name.Length <= 1 ? projection.ReadModel : name;
    }

    static string LastSegment(string name) => name[(name.LastIndexOf('.') + 1)..];
}
