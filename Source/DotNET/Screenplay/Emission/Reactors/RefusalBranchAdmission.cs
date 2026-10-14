// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Reactors;

/// <summary>
/// Admits ordered selectors only when their runtime identities and reachability survive emission.
/// </summary>
/// <param name="naming">The emitted declaration naming.</param>
/// <param name="application">The declarations the invocation can reference.</param>
internal class RefusalBranchAdmission(IScreenplayNaming naming, ApplicationModel application)
{
    /// <summary>
    /// Checks every selector without dropping, reordering or sanitizing any branch.
    /// </summary>
    /// <param name="invocation">The invocation and its ordered branches.</param>
    /// <param name="command">The declared command being invoked.</param>
    /// <returns>Whether the whole branch set can be stated faithfully.</returns>
    internal bool Allows(InvocationModel invocation, CommandModel command)
    {
        var earlier = new List<InvocationRefusalModel>();
        foreach (var branch in invocation.OnRefused)
        {
            if (!ValidSelector(branch) || earlier.Exists(previous => Covers(previous, branch)) || !NamesApplicableConstraint(branch, command))
            {
                return false;
            }
            earlier.Add(branch);
        }

        return true;
    }

    static bool ValidSelector(InvocationRefusalModel branch) => branch.Selector switch
    {
        "constraint" => true,
        "any" or "validation" or "authorization" => branch.Constraint is null,
        _ => false
    };

    static bool Covers(InvocationRefusalModel previous, InvocationRefusalModel branch) => previous.Selector switch
    {
        "any" => branch.Selector == "any" || branch.Selector == "validation" || branch.Selector == "constraint",
        "constraint" => branch.Selector == "constraint" && (previous.Constraint is null || previous.Constraint == branch.Constraint),
        _ => previous.Selector == branch.Selector
    };

    static IEnumerable<string> Targets(ConstraintModel constraint) => PrimaryTarget(constraint).Concat(constraint.AdditionalRules.SelectMany(Targets));

    static IEnumerable<string> PrimaryTarget(ConstraintModel constraint) => constraint switch
    {
        UniqueEventConstraintModel unique => [unique.EventName],
        UniquePropertyConstraintModel unique => [unique.EventName],
        _ => []
    };

    bool NamesApplicableConstraint(InvocationRefusalModel branch, CommandModel command)
    {
        if (branch.Constraint is not { } name)
        {
            return true;
        }

        // A renamed constraint would match a different runtime name; duplicate names are not guessed across scopes.
        if (naming.ToDeclarationName(name) != name ||
            application.Slices.SelectMany(slice => slice.Constraints).Where(constraint => naming.ToDeclarationName(constraint.Name) == name).ToList() is not [var constraint] ||
            constraint.Name != name)
        {
            return false;
        }
        var produced = command.Produces.Select(production => naming.ToDeclarationName(production.EventName)).ToHashSet(StringComparer.Ordinal);

        return Targets(constraint).Select(naming.ToDeclarationName).Any(produced.Contains);
    }
}
