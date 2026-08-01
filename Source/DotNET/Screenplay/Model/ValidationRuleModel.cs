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
/// <para>
/// An operand is a value the compiler held, except when the rule compares against another property of the same
/// command - an end date on or after the start date it was sent with - where it is a <see cref="PropertyPathSource"/>
/// naming that property and is written as the path rather than as text.
/// </para>
/// </remarks>
public record ValidationRuleModel(string Property, ValidationRuleKind Kind, object? Value, string? Message);
