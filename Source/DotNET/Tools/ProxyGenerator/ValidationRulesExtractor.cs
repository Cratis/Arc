// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extracts validation rules from FluentValidation validators.
/// </summary>
public static class ValidationRulesExtractor
{
    const string FluentValidationAbstractValidatorType = "FluentValidation.AbstractValidator`1";
    const string FluentValidationBaseValidatorType = "Cratis.Arc.Validation.BaseValidator`1";
    const string NotNullValidatorType = "FluentValidation.Validators.INotNullValidator";
    const string NotEmptyValidatorType = "FluentValidation.Validators.INotEmptyValidator";
    const string EmailValidatorType = "FluentValidation.Validators.IEmailValidator";
    const string LengthValidatorType = "FluentValidation.Validators.ILengthValidator";
    const string ComparisonValidatorType = "FluentValidation.Validators.IComparisonValidator";
    const string RegularExpressionValidatorType = "FluentValidation.Validators.IRegularExpressionValidator";

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
            if (validator == null)
            {
                return [];
            }

            // Call CreateDescriptor() method using reflection
            var createDescriptorMethod = validatorType.GetMethod("CreateDescriptor", BindingFlags.Public | BindingFlags.Instance);
            if (createDescriptorMethod == null)
            {
                return [];
            }

            var descriptor = createDescriptorMethod.Invoke(validator, null);
            if (descriptor == null)
            {
                return [];
            }

            var propertyValidations = new List<PropertyValidationDescriptor>();

            // Call GetMembersWithValidators() using reflection
            var getMembersMethod = descriptor.GetType().GetMethod("GetMembersWithValidators");
            if (getMembersMethod == null)
            {
                return [];
            }

            var members = getMembersMethod.Invoke(descriptor, null);
            if (members == null)
            {
                return [];
            }

            // Iterate through members
            foreach (var member in (System.Collections.IEnumerable)members)
            {
                var keyProperty = member.GetType().GetProperty("Key");
                var propertyName = keyProperty?.GetValue(member)?.ToString()?.ToCamelCase();

                if (string.IsNullOrEmpty(propertyName))
                {
                    continue;
                }

                var rules = new List<ValidationRuleDescriptor>();

                // Enumerate the validation rules for this member
                foreach (var rule in (System.Collections.IEnumerable)member)
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
        catch
        {
            // Silently fail if we can't extract rules
            return [];
        }
    }

    static Type? FindValidatorForType(Assembly assembly, Type type)
    {
        return assembly.GetTypes()
            .FirstOrDefault(t =>
            {
                if (t.IsAbstract || t.IsInterface)
                {
                    return false;
                }

                // Check if it's a BaseValidator<T> or AbstractValidator<T>
                var baseType = t.BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType)
                    {
                        var genericTypeDef = baseType.GetGenericTypeDefinition();
                        var fullName = genericTypeDef.FullName;

                        if (fullName == FluentValidationAbstractValidatorType || fullName == FluentValidationBaseValidatorType)
                        {
                            var genericArgs = baseType.GetGenericArguments();
                            if (genericArgs.Length == 1 && genericArgs[0] == type)
                            {
                                return true;
                            }
                        }
                    }
                    baseType = baseType.BaseType;
                }

                return false;
            });
    }

    static List<ValidationRuleDescriptor> ExtractRulesFromPropertyRule(object rule)
    {
        // rule is a tuple (IPropertyValidator Validator, IRuleComponent Options)
        // ValueTuple uses fields (Item1, Item2) not properties
        var validatorField = rule.GetType().GetField("Item1");
        var optionsField = rule.GetType().GetField("Item2");

        if (validatorField == null || optionsField == null)
        {
            return [];
        }

        var validator = validatorField.GetValue(rule);
        var options = optionsField.GetValue(rule);

        if (validator == null || options == null)
        {
            return [];
        }

        var ruleDescriptor = ExtractRuleFromValidator(validator, options);
        return ruleDescriptor != null ? [ruleDescriptor] : [];
    }

    static ValidationRuleDescriptor? ExtractRuleFromValidator(object validator, object component)
    {
        var validatorType = validator.GetType();
        var errorMessage = GetCustomErrorMessage(component);

        // Check validator type by interface
        var interfaces = validatorType.GetInterfaces();
        var interfaceNames = interfaces.Select(i => i.FullName ?? i.Name).ToHashSet();

        if (interfaceNames.Contains(NotNullValidatorType))
        {
            return new ValidationRuleDescriptor("notNull", [], errorMessage);
        }

        if (interfaceNames.Contains(NotEmptyValidatorType))
        {
            return new ValidationRuleDescriptor("notEmpty", [], errorMessage);
        }

        if (interfaceNames.Contains(EmailValidatorType))
        {
            return new ValidationRuleDescriptor("emailAddress", [], errorMessage);
        }

        if (interfaceNames.Contains(LengthValidatorType))
        {
            return ExtractLengthRule(validator, errorMessage);
        }

        if (interfaceNames.Contains(ComparisonValidatorType))
        {
            return ExtractComparisonRule(validator, errorMessage);
        }

        if (interfaceNames.Contains(RegularExpressionValidatorType))
        {
            return ExtractRegexRule(validator, errorMessage);
        }

        return null;
    }

    static string? GetCustomErrorMessage(object component)
    {
        try
        {
            // FluentValidation 12.x stores custom messages in the _errorMessage field on RuleComponent
            var errorMessageField = component.GetType()
                .GetField("_errorMessage", BindingFlags.NonPublic | BindingFlags.Instance);

            if (errorMessageField != null)
            {
                var errorMessage = errorMessageField.GetValue(component) as string;

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return errorMessage;
                }
            }

            // Fallback: Try FluentValidation 11.x approach with ErrorMessageSource property
            var errorMessageSource = component.GetType()
                .GetProperty("ErrorMessageSource", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(component);

            if (errorMessageSource != null)
            {
                var errorMessage = errorMessageSource.GetType()
                    .GetProperty("ErrorMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(errorMessageSource) as string;

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return errorMessage;
                }
            }
        }
        catch
        {
            // Ignore
        }

        return null;
    }

    static ValidationRuleDescriptor ExtractLengthRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var minProperty = validatorType.GetProperty("Min");
        var maxProperty = validatorType.GetProperty("Max");

        var min = minProperty?.GetValue(validator) as int? ?? 0;
        var max = maxProperty?.GetValue(validator) as int? ?? int.MaxValue;

        // Check for both min and max being set (both positive and less than MaxValue)
        if (min > 0 && max > 0 && max < int.MaxValue)
        {
            return new ValidationRuleDescriptor("length", [min, max], errorMessage);
        }

        // Check for minimum length only (max will be -1 or int.MaxValue)
        if (min > 0)
        {
            return new ValidationRuleDescriptor("minLength", [min], errorMessage);
        }

        // Check for maximum length only (max will be positive and less than MaxValue)
        if (max > 0 && max < int.MaxValue)
        {
            return new ValidationRuleDescriptor("maxLength", [max], errorMessage);
        }

        return new ValidationRuleDescriptor("notEmpty", [], errorMessage);
    }

    static ValidationRuleDescriptor? ExtractComparisonRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var valueToCompareProperty = validatorType.GetProperty("ValueToCompare");
        var comparisonProperty = validatorType.GetProperty("Comparison");

        var valueToCompare = valueToCompareProperty?.GetValue(validator);
        if (valueToCompare == null)
        {
            return null;
        }

        var comparison = comparisonProperty?.GetValue(validator);
        if (comparison == null)
        {
            return null;
        }

        // Comparison is an enum, get its string value
        var comparisonName = comparison.ToString();

        return comparisonName switch
        {
            "GreaterThan" => new ValidationRuleDescriptor("greaterThan", [valueToCompare], errorMessage),
            "GreaterThanOrEqual" => new ValidationRuleDescriptor("greaterThanOrEqual", [valueToCompare], errorMessage),
            "LessThan" => new ValidationRuleDescriptor("lessThan", [valueToCompare], errorMessage),
            "LessThanOrEqual" => new ValidationRuleDescriptor("lessThanOrEqual", [valueToCompare], errorMessage),
            _ => null
        };
    }

    static ValidationRuleDescriptor ExtractRegexRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var expressionProperty = validatorType.GetProperty("Expression");
        var pattern = expressionProperty?.GetValue(validator)?.ToString() ?? string.Empty;

        return new ValidationRuleDescriptor("matches", [pattern], errorMessage);
    }
}
