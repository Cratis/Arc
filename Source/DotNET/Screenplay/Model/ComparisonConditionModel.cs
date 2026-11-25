// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A comparison of a provided context value.
/// </summary>
/// <param name="Path">The path of the value beneath <c>$context</c>, for example <c>vessel.hasReported</c>.</param>
/// <param name="Operator">The name of the comparison the code makes: <c>IsTrue</c>, <c>Is</c>, <c>IsNot</c>, <c>In</c>, <c>NotIn</c>, <c>IsGreaterThan</c>, <c>IsAtLeast</c>, <c>IsLessThan</c> or <c>IsAtMost</c>.</param>
/// <param name="Operands">The operands: none for <c>IsTrue</c>, one for a comparison, any number for <c>In</c> and <c>NotIn</c>. Enumerations are their member names.</param>
public record ComparisonConditionModel(string Path, string Operator, IReadOnlyList<object> Operands) : AvailabilityConditionModel;
