// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.ReadModels;

/// <summary>
/// Decides which slice declares each read model.
/// </summary>
/// <remarks>
/// A read model is declared once in a whole document, because Screenplay refers to it by its simple name from
/// anywhere. Which slice holds the declaration is decided in this order, the first slice in namespace order winning
/// within each step:
/// <list type="number">
/// <item><description>The slice declaring a keyed query onto it - a <c>by</c> query answering with one instance. The
/// executable model identifies an instance through the keyed query declared beside the read model, so placing it
/// anywhere else would leave an identity the application really has unstated.</description></item>
/// <item><description>The slice declaring what builds it - its projection or reducer.</description></item>
/// <item><description>The slice the read model type is written in.</description></item>
/// <item><description>The slice declaring any other query onto it.</description></item>
/// <item><description>The slice declaring a command that reads it.</description></item>
/// </list>
/// A read model nothing in any slice refers to, and none of whose namespaces is a slice, is left out and said so - as
/// is one holding a value the document has no type for, since declaring it would name a type nothing introduces.
/// </remarks>
public static class ReadModelPlacement
{
    /// <summary>
    /// Places every read model of the application in the slice declaring it.
    /// </summary>
    /// <param name="slices">The slices of the application, joined across every project.</param>
    /// <param name="catalog">The read models the application declares.</param>
    /// <param name="properties">The <see cref="PropertyReader"/> reading what a placed read model holds.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The slices, each carrying the read models it declares.</returns>
    public static IReadOnlyList<SliceModel> Place(
        IReadOnlyList<SliceModel> slices,
        ReadModelCatalog catalog,
        PropertyReader properties,
        ScreenplayDiagnostics diagnostics)
    {
        foreach (var ambiguous in catalog.Ambiguous)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UndeclarableReadModel,
                $"The read model '{ambiguous}' shares its simple name with another read model, and a document refers to a read model by its simple name only, so neither is declared",
                ambiguous);
        }

        var ordered = slices
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ThenBy(_ => _.Name, StringComparer.Ordinal)
            .ToList();
        var placed = new Dictionary<SliceModel, List<ReadModelModel>>(ReferenceEqualityComparer.Instance);

        foreach (var (type, shape) in catalog.Unambiguous)
        {
            var slice = SliceOf(shape, ordered);
            if (slice is null)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UndeclarableReadModel,
                    $"The read model '{shape.Name}' is neither referred to by any slice nor written in one, so there is no slice to declare it in",
                    shape.Namespace);

                continue;
            }

            if (DeclarableShapes.FirstUndeclarable(type) is { } undeclarable)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UndeclarableReadModel,
                    $"The read model '{shape.Name}' holds '{undeclarable.Property}' as '{undeclarable.Type}', which the document has no type for, so the read model is not declared",
                    slice.Namespace);

                continue;
            }

            if (!placed.TryGetValue(slice, out var declared))
            {
                declared = [];
                placed[slice] = declared;
            }

            declared.Add(shape with { Properties = [.. properties.Read(type)] });
        }

        return [.. slices.Select(slice => placed.TryGetValue(slice, out var declared) ? slice with { ReadModels = declared } : slice)];
    }

    /// <summary>
    /// Finds the slice that declares a read model.
    /// </summary>
    /// <param name="readModel">The read model to place.</param>
    /// <param name="slices">The slices, in namespace order.</param>
    /// <returns>The slice, or <see langword="null"/> when none refers to the read model or holds it.</returns>
    static SliceModel? SliceOf(ReadModelModel readModel, List<SliceModel> slices) =>
        slices.Find(slice => slice.Queries.Any(query => Answers(query, readModel) && query.By is not null && !query.ReturnType.IsCollection)) ??
        slices.Find(slice => slice.Projections.Any(projection => projection.ReadModel == readModel.Name)) ??
        slices.Find(slice => slice.Namespace == readModel.Namespace) ??
        slices.Find(slice => slice.Queries.Any(query => Answers(query, readModel))) ??
        slices.Find(slice => slice.Commands.Any(command => command.Authoring?.Reads.Any(read => read.Name == readModel.Name) == true));

    static bool Answers(QueryModel query, ReadModelModel readModel) => query.ReturnType.Name == readModel.Name;
}
