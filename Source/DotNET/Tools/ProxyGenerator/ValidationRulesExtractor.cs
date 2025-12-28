// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extracts validation rules from FluentValidation validators and DataAnnotations attributes.
/// </summary>
public static class ValidationRulesExtractor
{
    const string FluentValidationAbstractValidatorType = "FluentValidation.AbstractValidator`1";
    const string FluentValidationBaseValidatorType = "Cratis.Arc.Validation.BaseValidator`1";
    const string DiscoverableValidatorType = "Cratis.Arc.Validation.DiscoverableValidator`1";
    const string CommandValidatorType = "Cratis.Arc.Commands.CommandValidator`1";
    const string QueryValidatorType = "Cratis.Arc.Queries.QueryValidator`1";
    const string NotNullValidatorType = "FluentValidation.Validators.INotNullValidator";
    const string NotEmptyValidatorType = "FluentValidation.Validators.INotEmptyValidator";
    const string EmailValidatorType = "FluentValidation.Validators.IEmailValidator";
    const string LengthValidatorType = "FluentValidation.Validators.ILengthValidator";
    const string MinimumLengthValidatorType = "FluentValidation.Validators.IMinimumLengthValidator";
    const string MaximumLengthValidatorType = "FluentValidation.Validators.IMaximumLengthValidator";
    const string ExactLengthValidatorType = "FluentValidation.Validators.IExactLengthValidator";
    const string ComparisonValidatorType = "FluentValidation.Validators.IComparisonValidator";
    const string RegularExpressionValidatorType = "FluentValidation.Validators.IRegularExpressionValidator";

    /// <summary>
    /// Extract validation rules for a specific type using FluentValidation validators.
    /// </summary>
    /// <param name="assembly">Assembly to search for validators in.</param>
    /// <param name="type">The type to extract validation rules for.</param>
    /// <returns>Collection of property validation descriptors.</returns>
    public static IEnumerable<PropertyValidationDescriptor> ExtractValidationRules(Assembly assembly, Type type)
    {
        // Try FluentValidation first
        var fluentValidationRules = ExtractFluentValidationRules(assembly, type).ToList();

        // Then extract DataAnnotations
        var dataAnnotationsRules = ExtractDataAnnotationsRules(type).ToList();

        // Merge the rules - FluentValidation takes precedence
        return MergeValidationRules(fluentValidationRules, dataAnnotationsRules);
    }

    /// <summary>
    /// Extract validation rules from DataAnnotations attributes on properties.
    /// </summary>
    /// <param name="type">The type to extract validation rules for.</param>
    /// <returns>Collection of property validation descriptors.</returns>
    public static IEnumerable<PropertyValidationDescriptor> ExtractDataAnnotationsRules(Type type)
    {
        var propertyValidations = new List<PropertyValidationDescriptor>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var rules = ExtractDataAnnotationsFromMember(property);
            if (rules.Count > 0)
            {
                propertyValidations.Add(new PropertyValidationDescriptor(property.Name.ToCamelCase(), [.. rules]));
            }
        }

        return propertyValidations;
    }

    /// <summary>
    /// Extract validation rules from DataAnnotations attributes on a parameter.
    /// </summary>
    /// <param name="parameter">The parameter to extract validation rules for.</param>
    /// <returns>Collection of validation rule descriptors.</returns>
    public static IReadOnlyList<ValidationRuleDescriptor> ExtractDataAnnotationsFromParameter(ParameterInfo parameter)
    {
        var attributesData = parameter.GetCustomAttributesData();
        return ExtractDataAnnotationsFromAttributesData(attributesData);
    }

    static List<ValidationRuleDescriptor> ExtractDataAnnotationsFromMember(PropertyInfo property)
    {
        var attributesData = property.GetCustomAttributesData();
        return ExtractDataAnnotationsFromAttributesData(attributesData);
    }

    static List<ValidationRuleDescriptor> ExtractDataAnnotationsFromAttributesData(IList<CustomAttributeData> attributesData)
    {
        var rules = new List<ValidationRuleDescriptor>();

        foreach (var attributeData in attributesData)
        {
            var attributeType = attributeData.AttributeType;
            var attributeTypeName = attributeType.FullName;

            if (attributeTypeName == null)
            {
                continue;
            }

            switch (attributeTypeName)
            {
                case "System.ComponentModel.DataAnnotations.RequiredAttribute":
                    {
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("notEmpty", [], errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.StringLengthAttribute":
                    {
                        var maximumLength = GetConstructorArgument<int>(attributeData, 0);
                        var minimumLength = GetNamedArgument<int>(attributeData, "MinimumLength");
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(ExtractStringLengthRuleFromData(minimumLength, maximumLength, errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.MinLengthAttribute":
                    {
                        var length = GetConstructorArgument<int>(attributeData, 0);
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("minLength", [length], errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.MaxLengthAttribute":
                    {
                        var length = GetConstructorArgument<int>(attributeData, 0);
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("maxLength", [length], errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.RangeAttribute":
                    {
                        var minimum = GetConstructorArgument<object>(attributeData, 0);
                        var maximum = GetConstructorArgument<object>(attributeData, 1);
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.AddRange(ExtractRangeRulesFromData(minimum, maximum, errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.RegularExpressionAttribute":
                    {
                        var pattern = GetConstructorArgument<string>(attributeData, 0) ?? string.Empty;
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("matches", [pattern], errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.EmailAddressAttribute":
                    {
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("emailAddress", [], errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.PhoneAttribute":
                    {
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("phone", [], errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.UrlAttribute":
                    {
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("url", [], errorMessage));
                        break;
                    }
                case "System.ComponentModel.DataAnnotations.CreditCardAttribute":
                    {
                        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");
                        rules.Add(new ValidationRuleDescriptor("creditCard", [], errorMessage));
                        break;
                    }
            }
        }

        return rules;
    }

    static ValidationRuleDescriptor ExtractStringLengthRuleFromData(int minimumLength, int maximumLength, string? errorMessage)
    {
        if (minimumLength > 0 && maximumLength > 0)
        {
            return new ValidationRuleDescriptor("length", [minimumLength, maximumLength], errorMessage);
        }

        if (minimumLength > 0)
        {
            return new ValidationRuleDescriptor("minLength", [minimumLength], errorMessage);
        }

        return new ValidationRuleDescriptor("maxLength", [maximumLength], errorMessage);
    }

    static List<ValidationRuleDescriptor> ExtractRangeRulesFromData(object? minimum, object? maximum, string? errorMessage)
    {
        // For range, we need both min and max
        return
        [
            new ValidationRuleDescriptor("greaterThanOrEqual", [minimum ?? 0], errorMessage),
            new ValidationRuleDescriptor("lessThanOrEqual", [maximum ?? 0], errorMessage)
        ];
    }

    static T? GetConstructorArgument<T>(CustomAttributeData attributeData, int index)
    {
        if (attributeData.ConstructorArguments.Count <= index)
        {
            return default;
        }

        var value = attributeData.ConstructorArguments[index].Value;
        if (value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    static T? GetNamedArgument<T>(CustomAttributeData attributeData, string name)
    {
        var namedArgument = attributeData.NamedArguments.FirstOrDefault(arg => arg.MemberName == name);
        if (namedArgument.TypedValue.Value is T typedValue)
        {
            return typedValue;
        }

        return default;
    }

    static List<PropertyValidationDescriptor> MergeValidationRules(
        List<PropertyValidationDescriptor> fluentValidationRules,
        List<PropertyValidationDescriptor> dataAnnotationsRules)
    {
        var merged = new Dictionary<string, PropertyValidationDescriptor>();

        // Add FluentValidation rules first (they take precedence)
        foreach (var rule in fluentValidationRules)
        {
            merged[rule.PropertyName] = rule;
        }

        // Add DataAnnotations rules only if property doesn't already have FluentValidation rules
        foreach (var rule in dataAnnotationsRules)
        {
            if (!merged.ContainsKey(rule.PropertyName))
            {
                merged[rule.PropertyName] = rule;
            }
        }

        return [.. merged.Values];
    }

    static List<PropertyValidationDescriptor> ExtractFluentValidationRules(Assembly assembly, Type type)
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
                    propertyValidations.Add(new PropertyValidationDescriptor(propertyName, [.. rules]));
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

                // Check if it's a BaseValidator<T>, DiscoverableValidator<T>, CommandValidator<T>, QueryValidator<T>, or AbstractValidator<T>
                var baseType = t.BaseType;
                while (baseType != null)
                {
                    if (baseType.IsGenericType)
                    {
                        var genericTypeDef = baseType.GetGenericTypeDefinition();
                        var fullName = genericTypeDef.FullName;

                        if (fullName == FluentValidationAbstractValidatorType ||
                            fullName == FluentValidationBaseValidatorType ||
                            fullName == DiscoverableValidatorType ||
                            fullName == CommandValidatorType ||
                            fullName == QueryValidatorType)
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

        // Handle specific length validators first before the generic ILengthValidator
        if (interfaceNames.Contains(MinimumLengthValidatorType))
        {
            return ExtractMinimumLengthRule(validator, errorMessage);
        }

        if (interfaceNames.Contains(MaximumLengthValidatorType))
        {
            return ExtractMaximumLengthRule(validator, errorMessage);
        }

        if (interfaceNames.Contains(ExactLengthValidatorType))
        {
            return ExtractExactLengthRule(validator, errorMessage);
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
        var minProperty = validatorType.GetProperty("Min", BindingFlags.Public | BindingFlags.Instance);
        var maxProperty = validatorType.GetProperty("Max", BindingFlags.Public | BindingFlags.Instance);

        var min = minProperty?.GetValue(validator) as int? ?? -1;
        var max = maxProperty?.GetValue(validator) as int? ?? -1;

        // Check for both min and max being set (both non-negative and max is not -1)
        if (min >= 0 && max >= 0 && max != -1)
        {
            return new ValidationRuleDescriptor("length", [min, max], errorMessage);
        }

        // Check for minimum length only
        if (min >= 0 && (max == -1 || max == int.MaxValue))
        {
            return new ValidationRuleDescriptor("minLength", [min], errorMessage);
        }

        // Check for maximum length only
        if (max >= 0 && max != -1 && (min == -1 || min == 0))
        {
            return new ValidationRuleDescriptor("maxLength", [max], errorMessage);
        }

        return new ValidationRuleDescriptor("notEmpty", [], errorMessage);
    }

    static ValidationRuleDescriptor ExtractMinimumLengthRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var minProperty = validatorType.GetProperty("Min", BindingFlags.Public | BindingFlags.Instance);
        var min = minProperty?.GetValue(validator) as int? ?? 0;
        return new ValidationRuleDescriptor("minLength", [min], errorMessage);
    }

    static ValidationRuleDescriptor ExtractMaximumLengthRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var maxProperty = validatorType.GetProperty("Max", BindingFlags.Public | BindingFlags.Instance);
        var max = maxProperty?.GetValue(validator) as int? ?? 0;
        return new ValidationRuleDescriptor("maxLength", [max], errorMessage);
    }

    static ValidationRuleDescriptor ExtractExactLengthRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var lengthProperty = validatorType.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);
        var length = lengthProperty?.GetValue(validator) as int? ?? 0;
        return new ValidationRuleDescriptor("length", [length, length], errorMessage);
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
