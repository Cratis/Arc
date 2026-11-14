// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Allowlisted validation projection metadata captured during extraction.</summary>
/// <param name="Member">The camelCase input member.</param>
/// <param name="RuleKind">The coarse rule family.</param>
/// <param name="Source">The rule source.</param>
/// <param name="Outcome">Whether the rule was projected or omitted.</param>
/// <param name="Reason">A stable projection reason.</param>
/// <param name="Severity">The statically known Arc severity, if any.</param>
internal sealed record ValidationProjectionDetail(string? Member, ValidationRuleKind RuleKind, string Source, string Outcome, string Reason, int? Severity = null);
