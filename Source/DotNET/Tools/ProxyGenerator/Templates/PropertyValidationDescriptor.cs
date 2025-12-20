// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Templates;

/// <summary>
/// Represents validation rules for a property.
/// </summary>
/// <param name="PropertyName">The name of the property being validated.</param>
/// <param name="Rules">The validation rules for this property.</param>
public record PropertyValidationDescriptor(
    string PropertyName,
    ValidationRuleDescriptor[] Rules);
