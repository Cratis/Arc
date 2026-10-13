// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Builds declarations only from the shared route-identity resolution.
/// </summary>
/// <param name="naming">The naming convention for stream-id parts.</param>
internal class EventSourceSyntaxBuilder(IScreenplayNaming naming)
{
    readonly TypeReferenceConverter _types = new(naming);

    /// <summary>
    /// Builds unique source declarations required by admitted behaviors.
    /// </summary>
    /// <param name="model">The application.</param>
    /// <param name="occurrences">The admitted specification occurrences.</param>
    /// <param name="metadataEnabled">Whether observer declarations are enabled.</param>
    /// <returns>The unique source declarations.</returns>
    internal List<EventSourceSyntax> Build(ApplicationModel model, IEnumerable<SpecificationStateModel> occurrences, bool metadataEnabled)
    {
        var routes = model.Slices.SelectMany(slice => slice.Commands).Select(command => command.Authoring?.Route).OfType<CommandRouteModel>();
        var declarations = EventSourceDeclarations.Resolve(routes.Concat(SpecificationRouteDeclarations.Read(model, occurrences)), EventSourceDeclarations.ObservedRoutes(model, metadataEnabled));

        return declarations.Sources.OrderBy(source => source.Identity.Source, StringComparer.Ordinal).Select(BuildSource).ToList();
    }

    static string? DifferentName(string stored, string model) => stored != model ? stored : null;

    EventSourceSyntax BuildSource(EventSourceDeclaration source) => new(source.Identity.Source, SourceLocation.Start)
    {
        Id = DifferentName(source.Identity.StoredSource, source.Identity.Source),
        Identifier = source.IdentifierType is { } identifier ? _types.Convert(identifier) : null,
        Streams = source.Streams.OrderBy(stream => stream.Identity.Stream, StringComparer.Ordinal).Select(BuildStream).ToList()
    };

    EventStreamSyntax BuildStream(CommandRouteModel stream) => new(stream.Identity.Stream!, SourceLocation.Start)
    {
        Id = DifferentName(stream.Identity.StoredStream!, stream.Identity.Stream!),
        StreamId = stream.StreamIdType is { } id ? _types.Convert(id) : null,
        StreamIdParts = stream.StreamIdParts.Select(part => new EventStreamIdPartSyntax(naming.ToPropertyName(part.Name), _types.Convert(part.Type), SourceLocation.Start)).ToList()
    };
}
