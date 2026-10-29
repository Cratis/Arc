// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Reads only a held slice's single awaited execution and its own unnamed rejection.
/// </summary>
/// <param name="models">The semantic models of the analyzed source.</param>
/// <param name="held">The constructions and writes of held values.</param>
/// <param name="values">The reader for concrete command values.</param>
internal sealed class SliceSpecificationReader(SemanticModels models, HeldValues held, SpecificationValues values)
{
    /// <summary>
    /// Gets the command type of a held slice.
    /// </summary>
    /// <param name="type">The specification type to inspect.</param>
    /// <returns>The held slice's command type, or null when no slice is held.</returns>
    internal static ITypeSymbol? CommandOf(INamedTypeSymbol type) => SliceConstructionProof.CommandOf(type);

    /// <summary>
    /// Reads a bounded slice rejection or records why it cannot be stated faithfully.
    /// </summary>
    /// <param name="type">The specification containing the action and assertions.</param>
    /// <param name="draft">The specification being recovered.</param>
    /// <param name="name">The recovered specification name.</param>
    /// <param name="location">The source location used in diagnostics.</param>
    internal void Read(INamedTypeSymbol type, SpecificationDraft draft, string name, string location)
    {
        var steps = SpecificationMembers.StepsOf(type);
        if (SliceActionProof.TryRead(steps, models, held) is not { } action ||
            SliceConstructionProof.TryRead(steps, action, held) is not { } construction)
        {
            draft.CannotRead("the held slice needs one stable construction and one unconditional awaited Run");
            return;
        }

        if (!action.HasStableOutcome(steps, held) || !construction.HasOnlyConstructionSetup(steps, action, models) ||
            SliceRejectionProof.TryRead(type, action, models) is not { } rejection ||
            !SliceReferenceProof.IsClosed(steps, models, construction, action, rejection))
        {
            draft.CannotRead("the slice has setup, writes, or assertions beyond its own unnamed rejection");
            return;
        }

        draft.EventSources.ReadCommand(construction.Command, construction.CommandCreation, construction.Model, models, null, action.Model.Compilation);
        draft.SetWhen(
            new SpecificationStateModel(
                construction.Command.Name,
                SpecificationStateKind.Command,
                values.Read(construction.CommandCreation, construction.Model, construction.Command, name, location, draft)),
            construction.Command,
            action.Run.GetLocation());
        draft.AddError(string.Empty, rejection.Evidence);
    }
}
