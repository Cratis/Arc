// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Pure declaration resolution shared by reflection and Roslyn, independent of their lookup mechanisms.
/// Integer values mirror the public Arc enums without adding runtime dependencies to compiler components.
/// </summary>
internal static class AttributePrecedencePolicy
{
    /// <summary>
    /// Metadata name of the key guard declaration.
    /// </summary>
    internal const string KeyGuardName = "Cratis.Arc.Commands.KeyGuardAttribute";

    /// <summary>
    /// Metadata name of the consistency declaration.
    /// </summary>
    internal const string ConsistencyName = "Cratis.Arc.Commands.ConsistencyAttribute";

    /// <summary>
    /// Metadata name of the lookup declaration.
    /// </summary>
    internal const string LookUpByName = "Cratis.Arc.Commands.LookUpByAttribute";

    /// <summary>
    /// Metadata name of the advisory declaration.
    /// </summary>
    internal const string UnprotectedName = "Cratis.Arc.Chronicle.ReadModels.UnprotectedAttribute";

    /// <summary>
    /// Metadata name of the protected decision declaration.
    /// </summary>
    internal const string ProtectedName = "Cratis.Arc.Chronicle.ReadModels.ProtectedDecisionAttribute";

    /// <summary>
    /// Identifies conflicting protection declarations.
    /// </summary>
    internal const string ProtectionConflict = "CommandCannotBeBothProtectedAndUnprotected";

    /// <summary>
    /// Identifies a key guard declaration conflicting with a protection declaration.
    /// </summary>
    internal const string KeyGuardConflict = "CommandCannotCombineProtectedDecisionAndKeyGuard";

    /// <summary>
    /// Identifies conflicting declarations at one read-policy level.
    /// </summary>
    internal const string ReadConflict = "ConflictingReadPolicy";

    /// <summary>
    /// Identifies a lookup that cannot be resolved.
    /// </summary>
    internal const string LookupConflict = "InvalidLookUpBy";

    /// <summary>
    /// Resolves the command profile, preserving protection declarations over assembly rollout settings.
    /// </summary>
    /// <param name="isProtected">Whether the command declares protected decisions.</param>
    /// <param name="isUnprotected">Whether the command declares advisory decisions.</param>
    /// <param name="commandKeyGuard">The inherited command declaration.</param>
    /// <param name="assemblyKeyGuard">The command assembly declaration.</param>
    /// <param name="hostDefault">The host fallback, Off by default.</param>
    /// <returns>The decision mode, guard mode and declaration conflict.</returns>
    internal static (int Mode, int KeyGuard, string? Conflict) ResolveCommand(
        bool isProtected,
        bool isUnprotected,
        int? commandKeyGuard,
        int? assemblyKeyGuard,
        int hostDefault = 0)
    {
        var mode = isProtected ? 1 : isUnprotected ? 2 : 0;
        var conflict = isProtected && isUnprotected
            ? ProtectionConflict
            : (isProtected || isUnprotected) && commandKeyGuard is 1 or 2 ? KeyGuardConflict : null;
        var keyGuard = isProtected || isUnprotected ? 0 : commandKeyGuard ?? assemblyKeyGuard ?? hostDefault;
        return (mode, keyGuard, conflict);
    }

    /// <summary>
    /// Resolves the nearest read declaration only while the key guard is On.
    /// </summary>
    /// <param name="keyGuard">The resolved guard mode.</param>
    /// <param name="levels">Declarations ordered from nearest to farthest.</param>
    /// <returns>The read policy, whether it contributes a guard label, and any conflict.</returns>
    internal static (int Policy, bool Label, string? Conflict) ResolveRead(
        int keyGuard,
        IEnumerable<(bool Unprotected, int? Consistency)> levels)
    {
        if (keyGuard != 2)
        {
            return (0, false, null);
        }

        foreach (var level in levels.Where(_ => _.Unprotected || _.Consistency.HasValue))
        {
            if (level.Unprotected && level.Consistency.HasValue)
            {
                return (0, false, ReadConflict);
            }

            return level.Unprotected ? (2, false, null) : (level.Consistency!.Value, true, null);
        }

        return (1, true, null);
    }

    /// <summary>
    /// Validates the independent explicit lookup axis in every guard mode.
    /// </summary>
    /// <param name="property">The declared property, or null without a declaration.</param>
    /// <param name="isReadModel">Whether the parameter is a read model.</param>
    /// <param name="propertyExists">Tests for a command property.</param>
    /// <returns>The selected property and any declaration conflict.</returns>
    internal static (string? Property, string? Conflict) ResolveLookup(string? property, bool isReadModel, Func<string, bool> propertyExists) =>
        property is null ? (null, null) : isReadModel && propertyExists(property) ? (property, null) : (null, LookupConflict);
}
