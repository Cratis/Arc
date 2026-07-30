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
        var domain = ToName(model.Domain, options.Domain);
        var modules = BuildModules(model, options, domain);
        var concepts = new ConceptSyntaxBuilder(naming, _validations, diagnostics, _names).Build(model.Concepts);
        var policies = new PolicySyntaxBuilder(naming).Build(model.Policies, _authorize.Referenced);

        return new(
            [.. BuildImports(model)],
            [.. concepts],
            [.. policies],
            [.. modules],
            SourceLocation.Start,
            new DomainSyntax(domain, SourceLocation.Start));
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
    /// <returns>The modules.</returns>
    IEnumerable<ModuleSyntax> BuildModules(ApplicationModel model, ScreenplayOptions options, string domain)
    {
        var sliceBuilder = CreateSliceBuilder();
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
    /// <returns>The <see cref="SliceSyntaxBuilder"/>.</returns>
    SliceSyntaxBuilder CreateSliceBuilder() =>
        new(
            naming,
            new CommandSyntaxBuilder(
                naming,
                _types,
                _authorize,
                _validations,
                new ProducesSyntaxBuilder(naming, _names),
                new ConcurrencySyntaxBuilder(naming, diagnostics),
                _names),
            new EventSyntaxBuilder(naming, _types, _names),
            new QuerySyntaxBuilder(naming, _types, _authorize),
            new ConstraintSyntaxBuilder(naming),
            new ReactorSyntaxBuilder(naming, diagnostics),
            new ProjectionSyntaxBuilder(naming, diagnostics, _names),
            new ScreenSyntaxBuilder(naming, _types),
            new SpecificationSyntaxBuilder(naming));

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
