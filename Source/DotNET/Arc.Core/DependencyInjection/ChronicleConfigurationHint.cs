// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Detects failures that stem from Chronicle not being configured and provides a hint on how to fix them.
/// </summary>
/// <remarks>
/// Arc must not reference Chronicle types directly, so detection is done purely by namespace so that
/// the layering (Chronicle depends on Arc, never the other way around) is preserved.
/// </remarks>
static class ChronicleConfigurationHint
{
    /// <summary>
    /// The hint appended to error messages when a failure appears to be caused by Chronicle not being configured.
    /// </summary>
    public const string Text =
        "This usually means Chronicle has not been configured. Call WithChronicle() on the Arc builder " +
        "(for example inside AddCratisArc(...)), or use AddCratis() which wires Arc and Chronicle together.";

    const string ChronicleNamespacePrefix = "Cratis.Chronicle";

    /// <summary>
    /// Determines whether the Chronicle configuration hint applies to a failed dependency resolution.
    /// </summary>
    /// <param name="parameterType">The type of the dependency that failed to resolve.</param>
    /// <param name="failure">The underlying failure captured while resolving the dependency.</param>
    /// <returns>True if the failure looks like a missing Chronicle configuration, false otherwise.</returns>
    public static bool AppliesTo(Type parameterType, Exception failure) =>
        IsChronicleType(parameterType) || MentionsChronicle(failure);

    static bool IsChronicleType(Type type) =>
        type.Namespace?.StartsWith(ChronicleNamespacePrefix, StringComparison.Ordinal) == true;

    static bool MentionsChronicle(Exception? failure)
    {
        while (failure is not null)
        {
            if (failure.Message.Contains(ChronicleNamespacePrefix, StringComparison.Ordinal))
            {
                return true;
            }

            failure = failure.InnerException;
        }

        return false;
    }
}
