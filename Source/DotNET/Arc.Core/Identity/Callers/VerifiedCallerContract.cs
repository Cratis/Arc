// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Supported local caller profiles; never accept an arbitrary incoming contract version.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal static class VerifiedCallerContract
{
    /// <summary>Checks exact local profile support.</summary>
    /// <param name="version">The declared profile.</param>
    /// <returns>True only for version 1.</returns>
    internal static bool Accepts(string? version) => string.Equals(version, "1", StringComparison.Ordinal);
}
