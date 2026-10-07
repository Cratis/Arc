// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Decides which of the read models placed in the slices of a document are declared by it.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="types">The shared converter for property types.</param>
/// <param name="declared">The names of the concepts and types the document declares.</param>
/// <param name="diagnostics">The diagnostics a read model that cannot be declared is reported to.</param>
/// <remarks>
/// A placed read model is declared only when the document can say what it holds: its declaration name must be its
/// own - not shared with another read model, nor one a concept or a type is declared under - and every property has
/// to be typed by something the document declares. Analysis already places only read models that hold, so this is
/// asked of the whole document once, as it is written, and whatever is left out here is left out of every slice -
/// which is also what an authoring-only <c>reads</c> needs to know, since it declares a read model no slice does.
/// </remarks>
public class ReadModelDeclarations(
    IScreenplayNaming naming,
    TypeReferenceConverter types,
    IReadOnlyCollection<string> declared,
    ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Gets the read models a document declares.
    /// </summary>
    /// <param name="slices">The slices of the document.</param>
    /// <returns>The read models.</returns>
    public IReadOnlySet<ReadModelModel> Of(IEnumerable<SliceModel> slices)
    {
        var taken = declared.ToHashSet(StringComparer.Ordinal);
        var known = ConceptSyntax.PrimitiveTypes.Concat(declared).ToHashSet(StringComparer.Ordinal);
        var placed = slices
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ThenBy(_ => _.Name, StringComparer.Ordinal)
            .SelectMany(slice => slice.ReadModels.Select(readModel => (Slice: slice, ReadModel: readModel)))
            .ToList();
        var shared = placed
            .GroupBy(_ => naming.ToDeclarationName(_.ReadModel.Name), StringComparer.Ordinal)
            .Where(_ => _.Count() > 1)
            .Select(_ => _.Key)
            .ToHashSet(StringComparer.Ordinal);

        return placed
            .Where(_ => IsDeclarable(_.ReadModel, _.Slice.Namespace, taken, known, shared))
            .Select(_ => _.ReadModel)
            .ToHashSet();
    }

    /// <summary>
    /// Determines whether a placed read model can be declared, reporting it when it cannot.
    /// </summary>
    /// <param name="readModel">The read model to check.</param>
    /// <param name="location">Where the read model is placed, for use in diagnostics.</param>
    /// <param name="taken">The names the document's concepts and types use.</param>
    /// <param name="known">The names a property can be typed by.</param>
    /// <param name="shared">The declaration names more than one placed read model would be declared under.</param>
    /// <returns>True when every name the declaration writes resolves to what the application holds.</returns>
    bool IsDeclarable(ReadModelModel readModel, string location, HashSet<string> taken, HashSet<string> known, HashSet<string> shared)
    {
        var name = naming.ToDeclarationName(readModel.Name);
        if (shared.Contains(name))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UndeclarableReadModel,
                $"The read model '{readModel.Name}' is not declared, because another read model is declared as '{name}' too",
                location);

            return false;
        }

        if (name.Length <= 1 || taken.Contains(name))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UndeclarableReadModel,
                $"The read model '{readModel.Name}' is not declared, because '{name}' is a name the document already uses for a concept or a type",
                location);

            return false;
        }

        var unknown = readModel.Properties.FirstOrDefault(property => !known.Contains(types.Convert(property.Type).Name));
        if (unknown is not null)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UndeclarableReadModel,
                $"The read model '{readModel.Name}' is not declared, because '{unknown.Name}' is typed by '{unknown.Type.Name}', which the document does not declare",
                location);

            return false;
        }

        return true;
    }
}
