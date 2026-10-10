// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Concepts;
using Cratis.Arc.Screenplay.Emission.Constraints;
using Cratis.Arc.Screenplay.Emission.Events;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Emission.Projections;
using Cratis.Arc.Screenplay.Emission.Queries;
using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Emission.Screens;
using Cratis.Arc.Screenplay.Emission.Slices;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission;

/// <summary>
/// Builds the Screenplay document describing an application model.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Everything the document contains is ordered explicitly, never by the order the model happened to arrive in. That
/// is what makes the same model produce byte identical output every time, which in turn is what makes a generated
/// document something you can commit, diff and review.
/// </remarks>
public class ApplicationSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    readonly AuthorizeSyntaxBuilder _authorize = new();
    readonly ValidationSyntaxBuilder _validations = new(naming, diagnostics);
    readonly TypeReferenceConverter _types = new(naming);
    readonly NameAvailability _names = new(naming, diagnostics);

    /// <summary>
    /// Builds the document.
    /// </summary>
    /// <param name="model">The model to build from.</param>
    /// <param name="options">The options to build with, already resolved.</param>
    /// <returns>The <see cref="ApplicationSyntax"/>.</returns>
    /// <remarks>
    /// The options arrive resolved rather than being resolved here. What a name falls back to depends on how the
    /// document was asked for - the assembly being analyzed when a generation asked for it, the domain of the model
    /// when a host emitted one it already had - so resolving where neither of those is known meant resolving a
    /// second time against a different answer and letting one of them quietly win.
    /// </remarks>
    public ApplicationSyntax Build(ApplicationModel model, ScreenplayOptions options)
    {
        var eventRoutes = options.AuthoringOnlyConstructs || options.MaximumExecutableModelVersion is not { } routeCap || routeCap.IsAtLeast(SemanticVersion.V8);
        if (options.AuthoringOnlyConstructs)
        {
            if (options.MaximumExecutableModelVersion is { } cap && !cap.IsAtLeast(SemanticVersion.V7))
            {
                model = AuthoringDeclarations.RemoveOrphans(model, new ExecutableCommandValues(diagnostics).Apply(model, cap, authoringOnlyConstructs: true));
            }
        }
        else
        {
            if (eventRoutes)
            {
                model = AuthoringDeclarations.Resolve(model, diagnostics, authoringOnlyConstructs: false);
            }

            model = AuthoringDeclarations.RemoveOrphans(model, new ExecutableCommandValues(diagnostics).Apply(model, options.MaximumExecutableModelVersion));
        }

        if (options.AuthoringOnlyConstructs)
        {
            model = AuthoringDeclarations.Resolve(model, diagnostics, options.AuthoringOnlyConstructs);
        }

        model = ProtectedIdentityAnnotations.Apply(model, diagnostics);
        model = new ExecutableValidationRules(diagnostics).Apply(model);

        var domain = ToName(model.Domain, options.Domain);
        var concepts = new ConceptSyntaxBuilder(naming, _validations, diagnostics, _names).Build(model.Concepts).ToList();
        var declaredTypes = new TypeSyntaxBuilder(naming, _types, diagnostics).Build(model.Types, concepts, model.Domain).ToList();
        var routedOccurrences = new List<SpecificationStateModel>();
        var modules = BuildModules(model, options, domain, [.. concepts.Select(_ => _.Name), .. declaredTypes.Select(_ => _.Name)], routedOccurrences).ToList();
        var policies = new PolicySyntaxBuilder(naming).Build(model.Policies, _authorize.Referenced);

        return new(
            [.. BuildImports(model)],
            [.. concepts],
            [.. policies],
            [.. modules],
            SourceLocation.Start,
            new DomainSyntax(domain, SourceLocation.Start),
            Types: [.. declaredTypes])
        {
            Systems = options.AuthoringOnlyConstructs ? model.Slices.SelectMany(slice => slice.Commands).SelectMany(command => command.Authoring?.Operations ?? [])
                .Select(operation => operation.System).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)
                .Select(system => new SystemSyntax(system, null, SourceLocation.Start)).ToList() : [],
            EventSources = eventRoutes ? BuildEventSources(model, routedOccurrences) : []
        };
    }

    /// <summary>
    /// Resolves specification routes without changing declarations recovered from commands.
    /// </summary>
    /// <param name="model">The application declaring command routes and event producers.</param>
    /// <param name="routedOccurrences">The specification occurrences whose routes are being admitted.</param>
    /// <returns>The source and stream declarations implied by these occurrences.</returns>
    internal static IEnumerable<CommandRouteModel> SpecificationRoutes(ApplicationModel model, IEnumerable<SpecificationStateModel> routedOccurrences)
    {
        var commands = model.Slices.SelectMany(slice => slice.Commands).ToList();
        foreach (var state in routedOccurrences)
        {
            if (state.Route is not { Source: not null, Stream: not null } route)
            {
                continue;
            }

            var declarations = commands.Select(command => command.Authoring?.Route).OfType<CommandRouteModel>().Where(candidate => candidate.Source == route.Source).ToList();
            var existing = declarations.Find(candidate => candidate.Stream == route.Stream);
            var identifiers = commands.Where(command => command.Produces.Any(production => production.EventName == state.Name))
                .Select(command => command.Properties.Concat(command.Authoring?.Generated ?? []).SingleOrDefault(property => property.Name == (command.Authoring?.Identifier ?? command.Identifier))?.Type)
                .OfType<TypeReferenceModel>().Distinct().ToList();
            yield return new(
                route.Source,
                route.Stream,
                declarations.Select(candidate => candidate.IdentifierType).OfType<TypeReferenceModel>().FirstOrDefault() ?? (identifiers is [var identifier] ? identifier : new("String", false, false)),
                existing is not null ? existing.StreamIdType : route.StreamId is not null ? new("String", false, false) : null,
                null)
            {
                StreamIdParts = existing?.StreamIdParts ?? route.StreamIdParts.Select(part => new CommandStreamIdPartModel(part.Property, new("String", false, false), part.Source)).ToList()
            };
        }
    }

    /// <summary>
    /// Builds the imports naming every event the application refers to without declaring it.
    /// </summary>
    /// <param name="model">The model to build from.</param>
    /// <returns>The imports, ordered.</returns>
    /// <remarks>
    /// The Screenplay compiler reads the last segment of an import as the name of an event that is known, so the
    /// segment naming the event is written exactly as every reference to it is written - through the same conversion
    /// - or the document would import one name and refer to another.
    /// </remarks>
    IEnumerable<ImportSyntax> BuildImports(ApplicationModel model) =>
        model.Imports
            .Select(ToQualifiedName)
            .Where(_ => _.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new ImportSyntax(_, SourceLocation.Start));

    /// <summary>
    /// Sanitizes every segment of a dotted name.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <returns>The sanitized name.</returns>
    string ToQualifiedName(string name) =>
        string.Join('.', name.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(naming.ToDeclarationName));

    /// <summary>
    /// Builds the modules holding every slice that declares something.
    /// </summary>
    /// <param name="model">The model to build from.</param>
    /// <param name="options">The options to build with, already resolved.</param>
    /// <param name="domain">The name of the domain, which a slice with no namespace left is gathered under.</param>
    /// <param name="declared">The names of the concepts and types the document declares.</param>
    /// <param name="routedOccurrences">The routed occurrences from specifications retained in the document.</param>
    /// <returns>The modules.</returns>
    IEnumerable<ModuleSyntax> BuildModules(ApplicationModel model, ScreenplayOptions options, string domain, IReadOnlyList<string> declared, ICollection<SpecificationStateModel> routedOccurrences)
    {
        var declaredReadModels = new ReadModelDeclarations(naming, _types, declared, diagnostics).Of(model.Slices);
        var originalSlices = model.Slices;
        model = ReadModelReferences.Apply(model, declaredReadModels, diagnostics);
        var omittedReferenceSlices = originalSlices.Zip(model.Slices)
            .Where(pair => pair.First.Queries.Count() != pair.Second.Queries.Count() || pair.First.Projections.Count() != pair.Second.Projections.Count() || pair.First.Specifications.Count() != pair.Second.Specifications.Count())
            .Select(pair => (pair.Second.Namespace, pair.Second.Name)).ToHashSet();
        var sliceBuilder = CreateSliceBuilder(new InlineEvents(model, naming), model, options, declaredReadModels, routedOccurrences);
        if (options.AuthoringOnlyConstructs)
        {
            sliceBuilder.AuthoringReadModels = model.Slices.SelectMany(slice => slice.Commands).SelectMany(command => command.Authoring?.Reads ?? []).ToList();
        }
        var placed = new List<PlacedSlice>();
        var segmentsToSkip = options.SegmentsToSkip ?? 0;

        foreach (var slice in model.Slices
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ThenBy(_ => _.Name, StringComparer.Ordinal))
        {
            var built = sliceBuilder.Build(slice);
            if (SliceContent.IsEmpty(built))
            {
                if (!slice.OmittedReadModels.Any() && slice.ReadModels.All(declaredReadModels.Contains) && !omittedReferenceSlices.Contains((slice.Namespace, slice.Name)))
                {
                    diagnostics.Warning(
                        ScreenplayDiagnosticCodes.EmptySlice,
                        $"The slice '{slice.Name}' declares nothing that can be expressed and was left out",
                        slice.Namespace);
                }
                continue;
            }

            placed.Add(new(slice.Namespace, built));
        }

        var builder = new SliceTreeBuilder(naming);

        return options.ModulesFromNamespaceRoots
            ? builder.BuildPerNamespaceRoot(placed, domain, segmentsToSkip)
            : builder.Build(placed, ToName(model.Module, options.Module), segmentsToSkip);
    }

    /// <summary>
    /// Composes the builder that turns one slice into its declaration.
    /// </summary>
    /// <param name="inlineEvents">The inline eligibility decisions shared by declaration and production emission.</param>
    /// <param name="model">The full application used to type specification destinations.</param>
    /// <param name="options">The construct admission options.</param>
    /// <param name="declaredReadModels">The read models the document declares.</param>
    /// <param name="routedOccurrences">The routed occurrences from specifications retained in the document.</param>
    /// <returns>The <see cref="SliceSyntaxBuilder"/>.</returns>
    SliceSyntaxBuilder CreateSliceBuilder(InlineEvents inlineEvents, ApplicationModel model, ScreenplayOptions options, IReadOnlySet<ReadModelModel> declaredReadModels, ICollection<SpecificationStateModel> routedOccurrences) =>
        new(
            naming,
            _types,
            new CommandSyntaxBuilder(
                naming,
                _types,
                _authorize,
                _validations,
                new ProducesSyntaxBuilder(naming, _names)
                {
                    InlineEvents = inlineEvents,
                    Events = new EventSyntaxBuilder(naming, _types, _names),
                    Diagnostics = diagnostics
                },
                new ConcurrencySyntaxBuilder(naming, diagnostics) { ExecutableRoutes = options.MaximumExecutableModelVersion is not { } routeCap || routeCap.IsAtLeast(SemanticVersion.V8) },
                _names)
            {
                AuthoringOnlyConstructs = options.AuthoringOnlyConstructs
            },
            new EventSyntaxBuilder(naming, _types, _names),
            new QuerySyntaxBuilder(naming, _types, _authorize) { AuthoringOnlyConstructs = options.AuthoringOnlyConstructs, Diagnostics = diagnostics },
            new ConstraintSyntaxBuilder(naming),
            new ReactorSyntaxBuilder(naming, diagnostics) { Application = model },
            new ProjectionSyntaxBuilder(naming, diagnostics, _names),
            new ScreenSyntaxBuilder(naming, _types),
            new SpecificationSyntaxBuilder(naming) { Application = model, Diagnostics = diagnostics, AuthoringOnlyConstructs = options.AuthoringOnlyConstructs, ExecutableRoutes = options.MaximumExecutableModelVersion is not { } cap || cap.IsAtLeast(SemanticVersion.V8), RoutedOccurrences = routedOccurrences })
        {
            InlineEvents = inlineEvents,
            DeclaredReadModels = declaredReadModels
        };

    List<EventSourceSyntax> BuildEventSources(ApplicationModel model, IEnumerable<SpecificationStateModel> routedOccurrences) => model.Slices.SelectMany(slice => slice.Commands)
        .Select(command => command.Authoring?.Route).OfType<CommandRouteModel>().Concat(SpecificationRoutes(model, routedOccurrences))
        .GroupBy(route => route.Source, StringComparer.Ordinal)
        .OrderBy(group => group.Key, StringComparer.Ordinal).Select(source => new EventSourceSyntax(source.Key, SourceLocation.Start)
        {
            Identifier = source.Select(route => route.IdentifierType).OfType<TypeReferenceModel>().FirstOrDefault() is { } identifier ? _types.Convert(identifier) : null,
            Streams = source.Where(route => route.Stream is not null).GroupBy(route => route.Stream!, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(stream => new EventStreamSyntax(stream.Key, SourceLocation.Start)
                {
                    StreamId = stream.Select(route => route.StreamIdType).OfType<TypeReferenceModel>().FirstOrDefault() is { } id ? _types.Convert(id) : null,
                    StreamIdParts = stream.First().StreamIdParts.Select(part => new EventStreamIdPartSyntax(naming.ToPropertyName(part.Name), _types.Convert(part.Type), SourceLocation.Start)).ToList()
                }).ToList()
        }).ToList();

    /// <summary>
    /// Sanitizes a document level name, falling back when it yields nothing usable.
    /// </summary>
    /// <param name="value">The name to sanitize.</param>
    /// <param name="fallback">The name to fall back to.</param>
    /// <returns>The sanitized name.</returns>
    string ToName(string? value, string? fallback)
    {
        var name = naming.ToDeclarationName(value ?? string.Empty);

        return name.Length > 1 ? name : naming.ToDeclarationName(fallback ?? ScreenplayOptions.DefaultName);
    }
}
