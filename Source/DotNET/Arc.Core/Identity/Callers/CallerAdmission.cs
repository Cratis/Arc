// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Requires an explicit principal-kind set. A permission ceiling never overrides a kind mismatch.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal static class CallerAdmission
{
    /// <summary>Checks explicit kind admission, never permissions alone.</summary>
    /// <param name="caller">The verified caller, if any.</param>
    /// <param name="allowed">The explicitly admitted kinds.</param>
    /// <returns>Whether the caller's kind is admitted.</returns>
    internal static bool Allows(VerifiedCaller? caller, IReadOnlySet<CallerPrincipalKind> allowed) =>
        caller is not null && Enum.IsDefined(caller.PrincipalKind) && allowed.Contains(caller.PrincipalKind);
}
