// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// Defines a system that is told about audiences that are granted but not declared, so an operator can see them.
/// </summary>
public interface IUndeclaredAudienceObserver
{
    /// <summary>
    /// Called when a resolved subject holds grants for audiences the model does not declare.
    /// </summary>
    /// <param name="subject">The subject the grants were resolved for.</param>
    /// <param name="undeclared">The grants that are granted but not declared.</param>
    void Observe(AudienceSubject subject, IReadOnlyCollection<AudienceGrant> undeclared);
}
