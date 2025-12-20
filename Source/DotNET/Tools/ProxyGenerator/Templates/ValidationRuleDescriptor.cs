// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Represents a validation rule for a property.
/// </summary>
/// <param name="RuleName">The name of the validation rule (e.g., notEmpty, minLength).</param>
/// <param name="Arguments">Arguments for the rule (e.g., minLength value).</param>
/// <param name="ErrorMessage">Optional custom error message.</param>
public record ValidationRuleDescriptor(
    string RuleName,
#pragma warning disable CA1819 // Properties should not return arrays. We use this directly in the .hbs files assuming its an array with `.length` property.
    object[] Arguments,
#pragma warning restore CA1819 // Properties should not return arrays
    string? ErrorMessage);
