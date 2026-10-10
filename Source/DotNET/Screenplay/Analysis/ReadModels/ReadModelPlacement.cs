// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
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
/// A read model is left out, and said so, whenever no declaration could be written that says what the application
/// holds: when its declaration name is shared with another read model, or with another type a query or a command
/// reads; when nothing in any slice refers to it and none of the slices is its namespace; when its name is one a concept
/// or a type is declared under; and when a value it holds cannot be typed faithfully (see
/// <see cref="DeclarableShapes"/>). Omitted shapes are retained as metadata so emission can withhold their references
/// instead of writing a document referring to an undeclared read model.
/// </remarks>
public static class ReadModelPlacement
{
    /// <summary>
    /// Places every read model of the application in the slice declaring it.
    /// </summary>
    /// <param name="slices">The slices of the application, joined across every project.</param>
    /// <param name="catalog">The read models the application declares.</param>
    /// <param name="types">The <see cref="TypeRegistry"/> a placed read model registers what it holds with.</param>
    /// <param name="naming">The <see cref="IScreenplayNaming"/> turning a simple name into the declaration name.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The slices, each carrying the read models it declares.</returns>
    public static IReadOnlyList<SliceModel> Place(
        IReadOnlyList<SliceModel> slices,
        ReadModelCatalog catalog,
        TypeRegistry types,
        IScreenplayNaming naming,
        ScreenplayDiagnostics diagnostics)
    {
        var ordered = slices
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ThenBy(_ => _.Name, StringComparer.Ordinal)
            .ToList();
        var candidates = catalog.All.ToList();
        var byName = candidates
            .GroupBy(_ => naming.ToDeclarationName(_.Shape.Name), StringComparer.Ordinal)
            .ToDictionary(_ => _.Key, _ => _.ToList(), StringComparer.Ordinal);
        var readElsewhere = ReadTypes(ordered).ToList();
        var shapes = new DeclarableShapes(types, byName.Keys.ToHashSet(StringComparer.Ordinal), naming);
        var properties = new PropertyReader(types);
        var placed = new Dictionary<SliceModel, List<ReadModelModel>>(ReferenceEqualityComparer.Instance);

        foreach (var (type, shape) in candidates)
        {
            var name = naming.ToDeclarationName(shape.Name);
            var location = shape.FullName ?? shape.Name;
            if (byName[name] is { Count: > 1 } sharing)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UndeclarableReadModel,
                    $"The read model '{location}' is declared as '{name}', as are {string.Join(", ", sharing.Where(_ => _.Shape != shape).Select(_ => $"'{_.Shape.FullName}'"))}, and a document refers to a read model by that name only, so none of them is declared",
                    location);

                continue;
            }

            if (readElsewhere.Find(_ => naming.ToDeclarationName(_.Name) == name && !string.Equals(_.FullName, shape.FullName, StringComparison.Ordinal)) is { FullName: { } other })
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UndeclarableReadModel,
                    $"The read model '{location}' is declared as '{name}', and '{other}' is read under the same name by a query or a command, so the read model is not declared",
                    location);

                continue;
            }

            var slice = SliceOf(shape, ordered);
            if (slice is null)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UndeclarableReadModel,
                    $"The read model '{shape.Name}' is neither referred to by any slice nor written in one, so there is no slice to declare it in",
                    shape.Namespace);

                continue;
            }

            if (types.Names.Any(_ => naming.ToDeclarationName(_) == name))
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UndeclarableReadModel,
                    $"The read model '{shape.Name}' is not declared, because '{name}' is a name the document already uses for a concept or a type",
                    slice.Namespace);

                continue;
            }

            if (shapes.FirstUndeclarable(type) is { } undeclarable)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UndeclarableReadModel,
                    $"The read model '{shape.Name}' holds '{undeclarable.Property}' {undeclarable.Reason}, so the read model is not declared",
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

        var declaredNames = placed.Values.SelectMany(readModels => readModels).Select(readModel => readModel.FullName).ToHashSet(StringComparer.Ordinal);
        var omitted = candidates.Where(candidate => !declaredNames.Contains(candidate.Shape.FullName))
            .Select(candidate => (Slice: SliceOf(candidate.Shape, ordered), candidate.Shape)).ToList();

        return [.. slices.Select(slice => slice with
        {
            ReadModels = placed.GetValueOrDefault(slice) ?? [],
            OmittedReadModels = omitted.Where(candidate => ReferenceEquals(candidate.Slice, slice)).Select(candidate => candidate.Shape).ToList()
        })];
    }

    /// <summary>
    /// Gets every type a query answers with or a command reads, whose full name is known.
    /// </summary>
    /// <param name="slices">The slices.</param>
    /// <returns>The simple and full name of each type.</returns>
    static IEnumerable<(string Name, string FullName)> ReadTypes(IEnumerable<SliceModel> slices) =>
        slices.SelectMany(slice => slice.Queries
            .Where(_ => _.ReturnTypeFullName is not null)
            .Select(_ => (_.ReturnType.Name, _.ReturnTypeFullName!))
            .Concat(slice.Commands
                .SelectMany(_ => _.Authoring?.Reads ?? [])
                .Where(_ => _.FullName is not null)
                .Select(_ => (_.Name, _.FullName!))));

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
        slices.Find(slice => slice.Commands.Any(command => command.Authoring?.Reads.Any(read => Reads(read, readModel)) == true));

    /// <summary>
    /// Determines whether a query answers with a read model, by full name when both are known.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="readModel">The read model.</param>
    /// <returns>True when it does.</returns>
    static bool Answers(QueryModel query, ReadModelModel readModel) =>
        query.ReturnTypeFullName is not null && readModel.FullName is not null
            ? query.ReturnTypeFullName == readModel.FullName
            : query.ReturnType.Name == readModel.Name;

    /// <summary>
    /// Determines whether a command reads a read model, by full name when both are known.
    /// </summary>
    /// <param name="read">The read.</param>
    /// <param name="readModel">The read model.</param>
    /// <returns>True when it does.</returns>
    static bool Reads(CommandReadModel read, ReadModelModel readModel) =>
        read.FullName is not null && readModel.FullName is not null
            ? read.FullName == readModel.FullName
            : read.Name == readModel.Name;
}
