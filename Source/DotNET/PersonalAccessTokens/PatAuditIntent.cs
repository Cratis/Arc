// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>An immutable, secret-free audit delivery, not a credential authority.</summary>
/// <param name="Id">Stable identifier assigned in the authority transaction.</param>
/// <param name="Metadata">The snapshot captured in that transaction.</param>
/// <param name="OccurredAt">The authority's commit timestamp, not delivery time.</param>
/// <param name="IsRevocation">Whether the authority committed a revocation.</param>
internal sealed record PatAuditIntent(Guid Id, PatAuditMetadata Metadata, DateTimeOffset OccurredAt, bool IsRevocation);
