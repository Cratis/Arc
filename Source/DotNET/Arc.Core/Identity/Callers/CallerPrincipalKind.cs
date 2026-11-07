// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// The authenticated principal category, independent of credential type.
/// </summary>
public enum CallerPrincipalKind
{
    /// <summary>A person acting directly.</summary>
    Person = 0,

    /// <summary>An actor with a checked delegation from a person.</summary>
    DelegatedActor = 1,

    /// <summary>An organization service, never a person.</summary>
    OrganizationAutomation = 2,

    /// <summary>A separately verified cluster workload.</summary>
    ClusterWorkload = 3,

    /// <summary>A separately verified runner.</summary>
    Runner = 4
}
