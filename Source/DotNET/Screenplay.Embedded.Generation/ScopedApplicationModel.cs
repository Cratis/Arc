// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;

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
    public static ApplicationModel For(ApplicationModel model, DocumentScope scope)
    {
        var slices = scope.Kind == EmbeddedDocumentKind.Assembly
            ? [.. model.Slices]
            : model.Slices.Where(_ => Namespaces.IsWithin(_.Namespace, scope.Namespace)).ToList();

        return model with
        {
            Module = scope.ModuleName,
            Slices = slices,
            Imports = ImportsFor(model, slices)
        };
    }

    /// <summary>
    /// Gets the fully qualified name of every event the scoped document refers to without declaring it.
    /// </summary>
    /// <param name="model">The model of the whole application.</param>
    /// <param name="slices">The slices within the scope.</param>
    /// <returns>The imports, ordered.</returns>
    /// <remarks>
    /// What the whole application imports is imported by every document of it, because an event outside the
    /// assembly is outside every part of it. What one part refers to and another part declares is only an import
    /// once the parts are separate documents, which is why it is resolved here rather than during analysis.
    /// </remarks>
    static IReadOnlyList<string> ImportsFor(ApplicationModel model, IReadOnlyList<SliceModel> slices)
    {
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
                .Concat(ReadModelsDeclaredElsewhere(model, slices, within))
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
    /// <returns>The imports.</returns>
    /// <remarks>
    /// A read model is declared once, in the slice that owns it, so a projection or a query in one part of the
    /// application can name a read model another part declares. Once the parts are separate documents that is a
    /// dependency on a declaration elsewhere, and it is stated the same way an event declared elsewhere is.
    /// </remarks>
    static IEnumerable<string> ReadModelsDeclaredElsewhere(ApplicationModel model, IReadOnlyList<SliceModel> slices, HashSet<string> within)
    {
        var declared = slices.SelectMany(_ => _.ReadModels).Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);
        var elsewhere = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slice in model.Slices.Where(_ => !within.Contains(_.Namespace)))
        {
            foreach (var readModel in slice.ReadModels.Where(_ => !declared.Contains(_.Name)))
            {
                elsewhere.TryAdd(readModel.Name, $"{slice.Namespace}{Namespaces.Separator}{readModel.Name}");
            }
        }

        return slices
            .SelectMany(slice => slice.Projections.Select(_ => _.ReadModel)
                .Concat(slice.Queries.Select(_ => _.ReturnType.Name))
                .Concat(slice.Commands.SelectMany(_ => _.Authoring?.Reads ?? []).Select(_ => _.Name)))
            .Where(elsewhere.ContainsKey)
            .Select(_ => elsewhere[_]);
    }
}
