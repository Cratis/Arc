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
        if (options.AuthoringOnlyConstructs)
        {
            if (options.MaximumExecutableModelVersion is { } cap && !cap.IsAtLeast(SemanticVersion.V7))
            {
                model = AuthoringDeclarations.RemoveOrphans(model, new ExecutableCommandValues(diagnostics).Apply(model, cap, authoringOnlyConstructs: true));
            }

            model = AuthoringDeclarations.Resolve(model, diagnostics);
        }
        else
        {
            model = AuthoringDeclarations.RemoveOrphans(model, new ExecutableCommandValues(diagnostics).Apply(model, options.MaximumExecutableModelVersion));
        }

        model = new ExecutableValidationRules(diagnostics).Apply(model);

        var domain = ToName(model.Domain, options.Domain);
        var concepts = new ConceptSyntaxBuilder(naming, _validations, diagnostics, _names).Build(model.Concepts).ToList();
        var declaredTypes = new TypeSyntaxBuilder(naming, _types, diagnostics).Build(model.Types, concepts, model.Domain).ToList();
        var modules = BuildModules(model, options, domain, [.. concepts.Select(_ => _.Name), .. declaredTypes.Select(_ => _.Name)]);
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
            EventSources = options.AuthoringOnlyConstructs ? BuildEventSources(model) : []
        };
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
    /// <returns>The modules.</returns>
    IEnumerable<ModuleSyntax> BuildModules(ApplicationModel model, ScreenplayOptions options, string domain, IReadOnlyList<string> declared)
    {
        var sliceBuilder = CreateSliceBuilder(new InlineEvents(model, naming), model, options.AuthoringOnlyConstructs, declared);
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
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.EmptySlice,
                    $"The slice '{slice.Name}' declares nothing that can be expressed and was left out",
                    slice.Namespace);
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
    /// <param name="authoringOnlyConstructs">Whether optional authoring constructs are emitted.</param>
    /// <param name="declared">The names of the concepts and types the document declares.</param>
    /// <returns>The <see cref="SliceSyntaxBuilder"/>.</returns>
    SliceSyntaxBuilder CreateSliceBuilder(InlineEvents inlineEvents, ApplicationModel model, bool authoringOnlyConstructs, IReadOnlyList<string> declared) =>
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
                new ConcurrencySyntaxBuilder(naming, diagnostics),
                _names)
            {
                AuthoringOnlyConstructs = authoringOnlyConstructs
            },
            new EventSyntaxBuilder(naming, _types, _names),
            new QuerySyntaxBuilder(naming, _types, _authorize),
            new ConstraintSyntaxBuilder(naming),
            new ReactorSyntaxBuilder(naming, diagnostics),
            new ProjectionSyntaxBuilder(naming, diagnostics, _names),
            new ScreenSyntaxBuilder(naming, _types),
            new SpecificationSyntaxBuilder(naming) { Application = model, Diagnostics = diagnostics })
        {
            InlineEvents = inlineEvents,
            DeclaredReadModels = new ReadModelDeclarations(naming, _types, declared, diagnostics).Of(model.Slices)
        };

    List<EventSourceSyntax> BuildEventSources(ApplicationModel model) => model.Slices.SelectMany(slice => slice.Commands)
        .Select(command => command.Authoring?.Route).OfType<CommandRouteModel>().GroupBy(route => route.Source, StringComparer.Ordinal)
        .OrderBy(group => group.Key, StringComparer.Ordinal).Select(source => new EventSourceSyntax(source.Key, SourceLocation.Start)
        {
            Identifier = source.Select(route => route.IdentifierType).OfType<TypeReferenceModel>().FirstOrDefault() is { } identifier ? _types.Convert(identifier) : null,
            Streams = source.Where(route => route.Stream is not null).GroupBy(route => route.Stream!, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(stream => new EventStreamSyntax(stream.Key, SourceLocation.Start)
                {
                    StreamId = stream.Select(route => route.StreamIdType).OfType<TypeReferenceModel>().FirstOrDefault() is { } id ? _types.Convert(id) : null
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
