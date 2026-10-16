// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Represents the property a date rule validates.
/// </summary>
/// <param name="Subject">The selected property type.</param>
/// <param name="Property">The property path.</param>
/// <param name="ForEach">Whether the rule applies to collection elements.</param>
internal readonly record struct DateRuleTarget(ITypeSymbol? Subject, string Property, bool ForEach);
