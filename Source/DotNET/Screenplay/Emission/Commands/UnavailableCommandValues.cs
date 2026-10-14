// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Withholds unavailable generated dependencies in every output mode, before version admission.
/// </summary>
/// <param name="diagnostics">The classified loss reports.</param>
internal class UnavailableCommandValues(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Removes whole dependent productions or optional mappings while retaining independent behavior.
    /// </summary>
    /// <param name="command">The analyzed command.</param>
    /// <param name="application">The event schemas used to prove requiredness.</param>
    /// <param name="location">The command's diagnostic location.</param>
    /// <returns>The faithfully representable command.</returns>
    internal CommandModel Apply(CommandModel command, ApplicationModel application, string location)
    {
        var productions = new List<ProducesModel>();
        foreach (var production in command.Produces)
        {
            if (production.UnavailableCondition is { } guard)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UnreadableCommandResponse,
                    $"Production '{production.EventName}' was left out because its condition depends on unavailable generated input '{guard.Name}' ({guard.Failure}); it cannot be stated unconditionally",
                    location);
                continue;
            }
            if (production.UnavailableDestination is { } destination)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult,
                    $"Production '{production.EventName}' was left out because its explicit destination depends on unavailable generated input '{destination.Name}' ({destination.Failure}); it was not redirected to command context",
                    location);
                continue;
            }

            var omitted = production.Mappings.Where(mapping => mapping.Source is UnavailableValueSource).ToList();
            var retained = CommandProductionPruning.WithoutMappings(production, omitted, application, diagnostics, location);
            if (retained is null)
            {
                continue;
            }
            foreach (var mapping in omitted)
            {
                var input = ((UnavailableValueSource)mapping.Source).Input;
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableCommandProduction,
                    $"The mapping of optional event property '{production.EventName}.{mapping.Property}' was left out because it depends on unavailable generated input '{input.Name}' ({input.Failure}); independent mappings and the production were retained",
                    location);
            }
            productions.Add(retained);
        }

        return command with
        {
            Produces = productions,
            HasNoFactBehavior = command.HasNoFactBehavior && command.Authoring?.UnavailableGenerated.Count is not > 0
        };
    }
}
