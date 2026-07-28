// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a condition comparing a property against a value.
/// </summary>
/// <param name="Left">The dotted path of the property being compared, in its original casing.</param>
/// <param name="Operator">The comparison being made.</param>
/// <param name="Right">What the property is compared against.</param>
public record ComparisonCondition(string Left, ComparisonKind Operator, MappingSourceModel Right) : ConditionModel;
