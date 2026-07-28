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
    /// <param name="options">The options to build with.</param>
    /// <returns>The <see cref="ApplicationSyntax"/>.</returns>
    public ApplicationSyntax Build(ApplicationModel model, ScreenplayOptions options)
    {
        var resolved = options.WithDefaults(model.Domain);
        var domain = ToName(model.Domain, resolved.Domain);
        var module = ToName(model.Module, resolved.Module);

        var modules = BuildModules(model, module, resolved.SegmentsToSkip ?? 0);
        var concepts = new ConceptSyntaxBuilder(naming, _validations, diagnostics, _names).Build(model.Concepts);
        var policies = new PolicySyntaxBuilder(naming).Build(model.Policies, _authorize.Referenced);

        return new(
            [],
            [.. concepts],
            [.. policies],
            [.. modules],
            SourceLocation.Start,
            new DomainSyntax(domain, SourceLocation.Start));
    }

    /// <summary>
    /// Builds the modules holding every slice that declares something.
    /// </summary>
    /// <param name="model">The model to build from.</param>
    /// <param name="module">The name of the module.</param>
    /// <param name="segmentsToSkip">The number of leading namespace segments to skip.</param>
    /// <returns>The modules.</returns>
    IEnumerable<ModuleSyntax> BuildModules(ApplicationModel model, string module, int segmentsToSkip)
    {
        var sliceBuilder = CreateSliceBuilder();
        var placed = new List<PlacedSlice>();

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

        return new SliceTreeBuilder(naming).Build(placed, module, segmentsToSkip);
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
            new ScreenSyntaxBuilder(naming, _types));

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
