// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using FluentValidation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Extensions for working with <see cref="IRuleBuilderInitial{T, TProperty}"/>.
/// </summary>
public static class RuleBuilderInitialExtensions
{
    /// <summary>
    /// Overrides the property name for the rule.
    /// </summary>
    /// <typeparam name="T">Type of object being validated.</typeparam>
    /// <typeparam name="TProperty">Property type.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <param name="propertyName">The property name to use.</param>
    /// <returns>The same rule builder for chaining.</returns>
    public static IRuleBuilderInitial<T, TProperty> OverridePropertyName<T, TProperty>(
        this IRuleBuilderInitial<T, TProperty> ruleBuilder,
        string propertyName)
    {
        // Use reflection to access the internal Rule property
        var ruleProperty = ruleBuilder.GetType().GetProperty("Rule", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (ruleProperty != null)
        {
            var rule = ruleProperty.GetValue(ruleBuilder);
            if (rule != null)
            {
                var propertyNameProperty = rule.GetType().GetProperty("PropertyName", BindingFlags.Public | BindingFlags.Instance);
                if (propertyNameProperty?.CanWrite == true)
                {
                    propertyNameProperty.SetValue(rule, propertyName);
                }
            }
        }

        return ruleBuilder;
    }
}