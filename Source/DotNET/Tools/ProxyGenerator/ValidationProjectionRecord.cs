// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Allowlisted metadata about one existing validation extraction decision.</summary>
/// <param name="Endpoint">The command or query identity.</param>
/// <param name="Member">The camelCase owning member, or null for a type-level record.</param>
/// <param name="RuleKind">The coarse rule family.</param>
/// <param name="Source">The rule source.</param>
/// <param name="Outcome">Whether extraction projected or omitted the rule.</param>
/// <param name="Reason">A stable reason, never a validator value.</param>
/// <param name="Severity">The statically known severity, if any.</param>
internal sealed record ValidationProjectionRecord(ValidationProjectionEndpoint Endpoint, string? Member, ValidationRuleKind RuleKind, ValidationProjectionSource Source, string Outcome, string Reason, int? Severity = null);
