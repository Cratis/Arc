// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Narrows the model of a whole application down to the part one document describes.
/// </summary>
/// <remarks>
/// Only the slices are narrowed. The concepts, the shapes and the policies are what the whole application means by
/// a name, so a document that redefined a subset of them would describe an application nobody wrote - and one that
/// left them out would refer to names it never introduces.
/// </remarks>
public static class ScopedApplicationModel
{
    /// <summary>
    /// Gets the model one document is generated from.
    /// </summary>
    /// <param name="model">The model of the whole application.</param>
    /// <param name="scope">The scope of the document.</param>
    /// <returns>The scoped <see cref="ApplicationModel"/>.</returns>
    /// <remarks>
    /// A slice outside the scope that declares an event or a read model a slice inside it refers to becomes an import. The
    /// reference is real and the document has to compile on its own, so it states the dependency outright in
    /// exactly the way the language already has for an event declared elsewhere.
    /// </remarks>
    public static ApplicationModel For(ApplicationModel model, DocumentScope scope) => For(model, scope, new ScreenplayOptions());

    /// <summary>
    /// Gets the scoped model with imports chosen from the declarations its emission actually retains.
    /// </summary>
    /// <param name="model">The whole application.</param>
    /// <param name="scope">The document scope.</param>
    /// <param name="options">The options used to emit this document.</param>
    /// <returns>The scoped model.</returns>
    public static ApplicationModel For(ApplicationModel model, DocumentScope scope, ScreenplayOptions options)
    {
        var slices = scope.Kind == EmbeddedDocumentKind.Assembly
            ? [.. model.Slices]
            : model.Slices.Where(_ => Namespaces.IsWithin(_.Namespace, scope.Namespace)).ToList();

        return model with
        {
            Module = scope.ModuleName,
            Slices = slices,
            Imports = ImportsFor(model, slices, options)
        };
    }

    /// <summary>
    /// Gets the fully qualified name of every event the scoped document refers to without declaring it.
    /// </summary>
    /// <param name="model">The model of the whole application.</param>
    /// <param name="slices">The slices within the scope.</param>
    /// <param name="options">The options used to emit the scoped document.</param>
    /// <returns>The imports, ordered.</returns>
    /// <remarks>
    /// What the whole application imports is imported by every document of it, because an event outside the
    /// assembly is outside every part of it. What one part refers to and another part declares is only an import
    /// once the parts are separate documents, which is why it is resolved here rather than during analysis.
    /// </remarks>
    static IReadOnlyList<string> ImportsFor(ApplicationModel model, IReadOnlyList<SliceModel> slices, ScreenplayOptions options)
    {
        // Use the same ReadModelDeclarations and authoring admission as emission, not a projection-count heuristic.
        // Imports do not decide declarations, so this syntax-only pass cannot change their ownership.
        var emitted = options.AuthoringOnlyConstructs
            ? new ApplicationSyntaxBuilder(new ScreenplayNaming(), new ScreenplayDiagnostics())
                .Build(model with { Slices = slices, Imports = [] }, options.WithDefaults(model.Domain))
                .Modules.SelectMany(module => module.Features).SelectMany(SlicesIn).ToList()
            : null;
        var within = slices.Select(_ => _.Namespace).ToHashSet(StringComparer.Ordinal);
        var declared = slices.SelectMany(_ => _.Events).Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);
        var elsewhere = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var slice in model.Slices
            .Where(_ => !within.Contains(_.Namespace))
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ThenBy(_ => _.Name, StringComparer.Ordinal))
        {
            foreach (var @event in slice.Events.Select(_ => _.Name).Where(_ => !declared.Contains(_)))
            {
                elsewhere.TryAdd(@event, $"{slice.Namespace}{Namespaces.Separator}{@event}");
            }
        }

        return
        [
            .. slices
                .SelectMany(ExternalEvents.ReferredToBy)
                .Where(elsewhere.ContainsKey)
                .Select(_ => elsewhere[_])
                .Concat(ReadModelsDeclaredElsewhere(model, slices, within, emitted))
                .Concat(model.Imports)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>
    /// Gets the fully qualified name of every read model the scoped document refers to that a slice outside it declares.
    /// </summary>
    /// <param name="model">The model of the whole application.</param>
    /// <param name="slices">The slices within the scope.</param>
    /// <param name="within">The namespaces of the slices within the scope.</param>
    /// <param name="emitted">The actual emitted slices in authoring-only mode.</param>
    /// <returns>The imports.</returns>
    /// <remarks>
    /// A read model is declared once, in the slice that owns it, so a projection or a query in one part of the
    /// application can name a read model another part declares. Once the parts are separate documents that is a
    /// dependency on a declaration elsewhere, and it is stated the same way an event declared elsewhere is.
    /// </remarks>
    static IEnumerable<string> ReadModelsDeclaredElsewhere(ApplicationModel model, IReadOnlyList<SliceModel> slices, HashSet<string> within, IReadOnlyList<SliceSyntax>? emitted)
    {
        var naming = new ScreenplayNaming();
        var referencedReads = emitted?.SelectMany(slice => slice.Commands).SelectMany(command => command.Reads ?? []).Select(read => read.ReadModel).ToHashSet(StringComparer.Ordinal);
        var reads = slices.SelectMany(_ => _.Commands).SelectMany(_ => _.Authoring?.Reads ?? [])
            .Where(read => referencedReads?.Contains(naming.ToDeclarationName(read.Name)) != false).ToList();
        var declared = emitted is null
            ? slices.SelectMany(_ => _.ReadModels).Select(_ => _.Name).ToHashSet(StringComparer.Ordinal)
            : emitted.SelectMany(slice => slice.ReadModels ?? []).Select(readModel => readModel.Name).ToHashSet(StringComparer.Ordinal);
        var elsewhere = model.Slices
            .Where(_ => !within.Contains(_.Namespace))
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ThenBy(_ => _.Name, StringComparer.Ordinal)
            .SelectMany(slice => slice.ReadModels
                .Where(_ => !declared.Contains(emitted is null ? _.Name : naming.ToDeclarationName(_.Name)))
                .Select(readModel => (ReadModel: readModel, Import: $"{slice.Namespace}{Namespaces.Separator}{readModel.Name}")))
            .ToList();
        var references = slices
            .SelectMany(slice => slice.Projections.Select(_ => (Name: _.ReadModel, FullName: (string?)null))
                .Concat(slice.Queries.Select(_ => (_.ReturnType.Name, _.ReturnTypeFullName))))
            .Concat(reads.Select(_ => (_.Name, _.FullName)));

        return references
            .SelectMany(reference => elsewhere.Where(_ => Refers(reference, _.ReadModel)).Select(_ => _.Import));
    }

    static IEnumerable<SliceSyntax> SlicesIn(FeatureSyntax feature) => feature.Slices.Concat(feature.Features.SelectMany(SlicesIn));

    /// <summary>
    /// Determines whether a reference names a read model, by full name when both are known.
    /// </summary>
    /// <param name="reference">The simple and, when known, full name the reference is written with.</param>
    /// <param name="readModel">The read model.</param>
    /// <returns>True when it does.</returns>
    static bool Refers((string Name, string? FullName) reference, ReadModelModel readModel) =>
        reference.FullName is not null && readModel.FullName is not null
            ? reference.FullName == readModel.FullName
            : reference.Name == readModel.Name;
}
