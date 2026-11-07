// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A test-only proxy base that implements the internal transport guarantee without granting
/// the dynamic proxy assembly access to Arc.Core internals.
/// </summary>
public class CredentialHeaderPreservationForSpecs : ICanPreserveCallerCredentialHeaders
{
    /// <inheritdoc/>
    public bool PreservesDuplicateCredentialHeaders => PreserveHeaders();

    /// <summary>Supplies the transport guarantee configured by a spec.</summary>
    /// <returns>Whether separate credential header lines are preserved.</returns>
    public virtual bool PreserveHeaders() => false;
}
