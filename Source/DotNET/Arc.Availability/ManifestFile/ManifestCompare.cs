// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Compares a context value with literals. An absent value is unknown, whatever the operator.
/// </summary>
/// <param name="Path">The context path, for example <c>arguments.vesselId</c>.</param>
/// <param name="Operator">One of 'isTrue', 'equal', 'notEqual', 'in', 'notIn', 'greaterThan', 'greaterThanOrEqual', 'lessThan' or 'lessThanOrEqual'.</param>
/// <param name="Operands">The literals: none for 'isTrue', one for a single comparison, any number for 'in' and 'notIn'.</param>
public record ManifestCompare(string Path, string Operator, IReadOnlyList<JsonElement> Operands) : ManifestCondition;
