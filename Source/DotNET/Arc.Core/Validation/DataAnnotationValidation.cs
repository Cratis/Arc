// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Cratis.Arc.Validation;

/// <summary>
/// Evaluates data annotations against the values represented by concepts.
/// </summary>
internal static class DataAnnotationValidation
{
    /// <summary>
    /// Validates properties, then type attributes and object-level rules, without validating properties twice.
    /// </summary>
    /// <param name="instance">The object whose annotations are evaluated.</param>
    /// <returns>The failures reported by data annotations and object-level rules.</returns>
    internal static IReadOnlyList<SystemValidationResult> Validate(object instance)
    {
        var results = new List<SystemValidationResult>();
        var context = new ValidationContext(instance);
        var properties = TypeDescriptor.GetProperties(instance.GetType()).Cast<PropertyDescriptor>().ToArray();
        if (!properties.Any(property => property.PropertyType.IsConcept()))
        {
            Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
            return results;
        }

        // Match TryValidateObject's stages: property failures suppress type rules, and type failures suppress
        // IValidatableObject. TryValidateValue preserves Required's priority within each property's attributes.
        if (!ValidateProperties(instance, properties, results))
        {
            return results;
        }

        var typeAttributes = TypeDescriptor.GetAttributes(instance.GetType()).OfType<ValidationAttribute>();
        if (Validator.TryValidateValue(instance, context, results, typeAttributes) && instance is IValidatableObject validatable)
        {
            var objectResults = validatable.Validate(context);
            if (objectResults is not null)
            {
                results.AddRange(objectResults.Where(result => result is not null));
            }
        }

        return results;
    }

    /// <summary>
    /// Gets a concept's underlying value, leaving null and ordinary values unchanged.
    /// </summary>
    /// <param name="value">The property or parameter value to validate.</param>
    /// <returns>The underlying concept value, or the original value.</returns>
    internal static object? UnwrapConcept(object? value) =>
        value is not null && value.GetType().IsConcept() ? value.GetConceptValue() : value;

    static bool ValidateProperties(object instance, IEnumerable<PropertyDescriptor> properties, List<SystemValidationResult> results)
    {
        var isValid = true;
        foreach (var property in properties.Where(property => property.Attributes.OfType<ValidationAttribute>().Any()))
        {
            var context = new ValidationContext(instance) { MemberName = property.Name };
            isValid &= Validator.TryValidateValue(
                UnwrapConcept(property.GetValue(instance)),
                context,
                results,
                property.Attributes.OfType<ValidationAttribute>());
        }

        return isValid;
    }
}
