// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Identifies the verified principal category, independently of its credential.
/// </summary>
public enum CapabilityPrincipalKind
{
    /// <summary>
    /// A person acting directly.
    /// </summary>
    Person = 0,

    /// <summary>
    /// An actor with a checked delegation from a person.
    /// </summary>
    DelegatedActor = 1,

    /// <summary>
    /// An organization service.
    /// </summary>
    OrganizationAutomation = 2,

    /// <summary>
    /// A verified cluster workload.
    /// </summary>
    ClusterWorkload = 3,

    /// <summary>
    /// A verified runner.
    /// </summary>
    Runner = 4
}
