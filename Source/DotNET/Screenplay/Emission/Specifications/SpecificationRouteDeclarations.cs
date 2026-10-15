// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Carries specification route identities into declaration admission without replacing stored names.
/// </summary>
internal static class SpecificationRouteDeclarations
{
    /// <summary>
    /// Carries a command's complete route identity into an expected event occurrence.
    /// </summary>
    /// <param name="command">The command's proven route.</param>
    /// <param name="streamId">The concrete stream id.</param>
    /// <param name="parts">The concrete composite stream-id parts.</param>
    /// <returns>The occurrence route with the command's model and stored names.</returns>
    internal static SpecificationEventRouteModel Inferred(CommandRouteModel command, LiteralSource? streamId, IReadOnlyList<PropertyMappingModel> parts) =>
        new(command.Identity.Source, command.Identity.Stream, streamId)
        {
            StoredSource = command.Identity.StoredSource,
            StoredStream = command.Identity.StoredStream,
            StreamIdParts = parts
        };

    /// <summary>
    /// Reads declarations required by routed specification occurrences.
    /// </summary>
    /// <param name="model">The application declaring commands and events.</param>
    /// <param name="occurrences">The concrete or command-inferred event occurrences.</param>
    /// <returns>The declarations, carrying their original stored-name identities.</returns>
    internal static IEnumerable<CommandRouteModel> Read(ApplicationModel model, IEnumerable<SpecificationStateModel> occurrences)
    {
        var commands = model.Slices.SelectMany(slice => slice.Commands).ToList();

        return occurrences.Where(state => state.Route?.Identity is { Stream: not null }).Select(state => Describe(state, commands));
    }

    static CommandRouteModel Describe(SpecificationStateModel state, List<CommandModel> commands)
    {
        var route = state.Route!;
        var identity = route.Identity!;
        var declarations = commands.SelectMany(Commands.ProductionRouteAdmission.Routes)
            .Where(candidate => candidate.Identity.StoredSource == identity.StoredSource).ToList();
        var existing = declarations.Find(candidate => candidate.Identity == identity);

        return new(
            identity.Source,
            identity.Stream,
            declarations.Select(candidate => candidate.IdentifierType).OfType<TypeReferenceModel>().FirstOrDefault() ?? IdentifierOf(state, commands),
            existing is not null ? existing.StreamIdType : route.StreamId is not null ? new("String", false, false) : null,
            null)
        {
            StoredSource = identity.StoredSource,
            StoredStream = identity.StoredStream,
            StreamIdParts = existing?.StreamIdParts ?? route.StreamIdParts.Select(part => new CommandStreamIdPartModel(part.Property, new("String", false, false), part.Source)).ToList()
        };
    }

    static TypeReferenceModel IdentifierOf(SpecificationStateModel state, List<CommandModel> commands)
    {
        var identifiers = commands.Where(command => command.Produces.Any(production => production.EventName == state.Name))
            .Select(command => command.Properties.Concat(command.Authoring?.Generated ?? [])
                .SingleOrDefault(property => property.Name == (command.Authoring?.Identifier ?? command.Identifier))?.Type)
            .OfType<TypeReferenceModel>().Distinct().ToList();

        return identifiers is [var identifier] ? identifier : new("String", false, false);
    }
}
