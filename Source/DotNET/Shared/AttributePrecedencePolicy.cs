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
    /// Metadata name of the read-protection declaration.
    /// </summary>
    internal const string ReadProtectionName = "Cratis.Arc.Commands.ReadProtectionAttribute";

    /// <summary>
    /// Metadata name of the explicit lookup declaration.
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
    /// Identifies incompatible protected and advisory command declarations.
    /// </summary>
    internal const string ProtectionConflict = "CommandCannotBeBothProtectedAndUnprotected";

    /// <summary>
    /// Identifies an explicit rollout declaration conflicting with a command opt-out.
    /// </summary>
    internal const string ReadProtectionConflict = "ConflictingReadProtection";

    /// <summary>
    /// Identifies an invalid explicit lookup declaration.
    /// </summary>
    internal const string LookupConflict = "InvalidLookUpBy";

    /// <summary>
    /// Resolves the existing decision profile and the independent read-protection rollout mode.
    /// </summary>
    /// <param name="isProtected">Whether the command declares protected decisions.</param>
    /// <param name="isUnprotected">Whether the command declares advisory decisions.</param>
    /// <param name="commandReadProtection">The inherited command declaration.</param>
    /// <param name="assemblyReadProtection">The command assembly declaration.</param>
    /// <returns>The decision mode, read-protection mode and declaration conflict.</returns>
    internal static (int Mode, int ReadProtection, string? Conflict) ResolveCommand(
        bool isProtected,
        bool isUnprotected,
        int? commandReadProtection,
        int? assemblyReadProtection)
    {
        var mode = isProtected ? 1 : isUnprotected ? 2 : 0;
        var conflict = isProtected && isUnprotected
            ? ProtectionConflict
            : isUnprotected && commandReadProtection is 1 or 2 ? ReadProtectionConflict : null;
        var readProtection = isUnprotected ? 0 : commandReadProtection ?? assemblyReadProtection ?? 0;
        return (mode, readProtection, conflict);
    }

    /// <summary>
    /// Records parameter/model advisory declarations separately from method-body opt-outs.
    /// No reads or guards are executed by this policy.
    /// </summary>
    /// <param name="parameterUnprotected">Whether the injected parameter is advisory.</param>
    /// <param name="modelUnprotected">Whether the read-model type is advisory.</param>
    /// <param name="methodUnprotected">Whether reads in the method body are advisory.</param>
    /// <returns>The parameter's advisory status and the method-body declaration.</returns>
    internal static (bool Advisory, bool MethodBodyUnprotected) ResolveRead(
        bool parameterUnprotected,
        bool modelUnprotected,
        bool methodUnprotected) =>
        (parameterUnprotected || modelUnprotected, methodUnprotected);

    /// <summary>
    /// Validates the independent explicit lookup axis in every read-protection mode.
    /// </summary>
    /// <param name="property">The declared property, or null without a declaration.</param>
    /// <param name="isReadModel">Whether the parameter is a read model.</param>
    /// <param name="propertyExists">Tests for a command property.</param>
    /// <returns>The selected property and any declaration conflict.</returns>
    internal static (string? Property, string? Conflict) ResolveLookup(string? property, bool isReadModel, Func<string, bool> propertyExists) =>
        property is null ? (null, null) : isReadModel && propertyExists(property) ? (property, null) : (null, LookupConflict);
}
