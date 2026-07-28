// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a single declarative validation rule.
/// </summary>
/// <param name="Property">The dotted path of the property the rule applies to, in its original casing.</param>
/// <param name="Kind">The kind of rule.</param>
/// <param name="Value">The operand, or <see langword="null"/> when the rule takes none.</param>
/// <param name="Message">The message shown when the rule is broken, or <see langword="null"/> for the default.</param>
/// <remarks>
/// A message starting with <c>$strings.</c> is a localization key and is emitted unquoted.
/// </remarks>
public record ValidationRuleModel(string Property, ValidationRuleKind Kind, object? Value, string? Message);
