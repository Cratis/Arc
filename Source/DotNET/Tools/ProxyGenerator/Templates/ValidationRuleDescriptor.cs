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
    object[] Arguments,
    string? ErrorMessage);
