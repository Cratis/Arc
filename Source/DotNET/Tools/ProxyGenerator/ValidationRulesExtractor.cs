// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Validation;
using Cratis.Strings;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extracts validation rules from FluentValidation validators.
/// </summary>
public static class ValidationRulesExtractor
{
    /// <summary>
    /// Extract validation rules for a specific type.
    /// </summary>
    /// <param name="assembly">Assembly to search for validators in.</param>
    /// <param name="type">The type to extract validation rules for.</param>
    /// <returns>Collection of property validation descriptors.</returns>
    public static IEnumerable<PropertyValidationDescriptor> ExtractValidationRules(Assembly assembly, Type type)
    {
        var validatorType = FindValidatorForType(assembly, type);
        if (validatorType == null)
        {
            return [];
        }

        try
        {
            var validator = Activator.CreateInstance(validatorType);
            if (validator is IValidator fluentValidator)
            {
                var descriptor = fluentValidator.CreateDescriptor();
                var propertyValidations = new List<PropertyValidationDescriptor>();

                foreach (var member in descriptor.GetMembersWithValidators())
                {
                    var propertyName = member.Key.ToCamelCase();
                    var rules = new List<ValidationRuleDescriptor>();

                    foreach (var rule in member)
                    {
                        var ruleDescriptors = ExtractRulesFromPropertyRule(rule);
                        rules.AddRange(ruleDescriptors);
                    }

                    if (rules.Count > 0)
                    {
                        propertyValidations.Add(new PropertyValidationDescriptor(propertyName, rules));
                    }
                }

                return propertyValidations;
            }
        }
        catch
        {
            // If we can't create the validator or extract rules, just return empty
        }

        return [];
    }

    static Type? FindValidatorForType(Assembly assembly, Type type)
    {
        var baseValidatorType = typeof(BaseValidator<>).MakeGenericType(type);
        var abstractValidatorType = typeof(AbstractValidator<>).MakeGenericType(type);

        var validatorType = assembly.GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract &&
                !t.IsInterface &&
                (baseValidatorType.IsAssignableFrom(t) || abstractValidatorType.IsAssignableFrom(t)));

        return validatorType;
    }

    static List<ValidationRuleDescriptor> ExtractRulesFromPropertyRule((IPropertyValidator Validator, IRuleComponent Options) rule)
    {
        var ruleDescriptor = ExtractRuleFromValidator(rule.Validator, rule.Options);
        return ruleDescriptor != null ? [ruleDescriptor] : [];
    }

    static ValidationRuleDescriptor? ExtractRuleFromValidator(IPropertyValidator validator, IRuleComponent component)
    {
        var errorMessage = GetCustomErrorMessage(component);

        return validator switch
        {
            INotNullValidator => new ValidationRuleDescriptor("notNull", [], errorMessage),
            INotEmptyValidator => new ValidationRuleDescriptor("notEmpty", [], errorMessage),
            IEmailValidator => new ValidationRuleDescriptor("emailAddress", [], errorMessage),
            ILengthValidator lengthValidator => ExtractLengthRule(lengthValidator, errorMessage),
            IComparisonValidator comparisonValidator => ExtractComparisonRule(comparisonValidator, errorMessage),
            IRegularExpressionValidator regexValidator => ExtractRegexRule(regexValidator, errorMessage),
            _ => null
        };
    }

    static string? GetCustomErrorMessage(IRuleComponent component)
    {
        try
        {
            var errorMessageSource = component.GetType()
                .GetProperty("ErrorMessageSource", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(component);

            if (errorMessageSource != null)
            {
                return errorMessageSource.GetType()
                    .GetProperty("ErrorMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(errorMessageSource) as string;
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    static ValidationRuleDescriptor ExtractLengthRule(ILengthValidator validator, string? errorMessage)
    {
        var min = validator.Min;
        var max = validator.Max;

        if (min > 0 && max < int.MaxValue)
        {
            return new ValidationRuleDescriptor("length", [min, max], errorMessage);
        }

        if (min > 0)
        {
            return new ValidationRuleDescriptor("minLength", [min], errorMessage);
        }

        if (max < int.MaxValue)
        {
            return new ValidationRuleDescriptor("maxLength", [max], errorMessage);
        }

        return new ValidationRuleDescriptor("notEmpty", [], errorMessage);
    }

    static ValidationRuleDescriptor? ExtractComparisonRule(IComparisonValidator validator, string? errorMessage)
    {
        var valueToCompare = validator.ValueToCompare;
        if (valueToCompare == null)
        {
            return null;
        }

        return validator.Comparison switch
        {
            Comparison.GreaterThan => new ValidationRuleDescriptor("greaterThan", [valueToCompare], errorMessage),
            Comparison.GreaterThanOrEqual => new ValidationRuleDescriptor("greaterThanOrEqual", [valueToCompare], errorMessage),
            Comparison.LessThan => new ValidationRuleDescriptor("lessThan", [valueToCompare], errorMessage),
            Comparison.LessThanOrEqual => new ValidationRuleDescriptor("lessThanOrEqual", [valueToCompare], errorMessage),
            _ => null
        };
    }

    static ValidationRuleDescriptor ExtractRegexRule(IRegularExpressionValidator validator, string? errorMessage)
    {
        var pattern = validator.Expression;
        return new ValidationRuleDescriptor("matches", [pattern], errorMessage);
    }
}
