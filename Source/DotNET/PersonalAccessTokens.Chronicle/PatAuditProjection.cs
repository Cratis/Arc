// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Arc.PersonalAccessTokens.Chronicle;

/// <summary>Builds owner-listing audit state. Chronicle is not verification or revocation authority.</summary>
public sealed class PatAuditProjection : IProjectionFor<PatAuditEntry>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<PatAuditEntry> builder) => builder
        .From<PatIssued>()
        .From<PatRevoked>();
}
