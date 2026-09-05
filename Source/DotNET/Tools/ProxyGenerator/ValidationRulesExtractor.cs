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
    const string ConceptAsType = "Cratis.Concepts.ConceptAs`1";

    /// <summary>
    /// Extract validation rules for a specific type using FluentValidation validators.
    /// </summary>
    /// <param name="assembly">Assembly to search for validators in.</param>
    /// <param name="type">The type to extract validation rules for.</param>
    /// <returns>Collection of property validation descriptors.</returns>
    public static IEnumerable<PropertyValidationDescriptor> ExtractValidationRules(Assembly assembly, Type type)
    {
        // A FluentValidation rule only exists once its validator's constructor has run, which a metadata-only type
        // cannot do - see RuntimeValidatorAssemblies.
        var runtimeAssembly = RuntimeValidatorAssemblies.For(assembly) ?? assembly;
        var runtimeType = RuntimeValidatorAssemblies.For(type);

        // Try FluentValidation first
        var fluentValidationRules = ExtractFluentValidationRules(runtimeAssembly, runtimeType).ToList();

        // Then the rules contributed by the validators of any concept-typed properties
        var conceptRules = ExtractConceptRules(runtimeAssembly, runtimeType).ToList();

        // Then extract DataAnnotations
        var dataAnnotationsRules = ExtractDataAnnotationsRules(type).ToList();

        // Merge the rules - FluentValidation takes precedence
        return MergeValidationRules(fluentValidationRules, conceptRules, dataAnnotationsRules);
    }

    /// <summary>
    /// Extract the validation rules that <c>ConceptValidator&lt;T&gt;</c> validators contribute to a type's
    /// concept-typed properties, attributed to the property carrying the concept.
    /// </summary>
    /// <param name="assembly">Assembly to search for validators in.</param>
    /// <param name="type">The type whose properties to inspect.</param>
    /// <returns>Collection of property validation descriptors.</returns>
    /// <remarks>
    /// Whether a value is well formed is a property of its type, so a concept's validator already runs server-side
    /// wherever that concept appears. Projecting it here means declaring it once also validates in the browser,
    /// rather than the client silently enforcing less than the server.
    /// </remarks>
    public static IEnumerable<PropertyValidationDescriptor> ExtractConceptRules(Assembly assembly, Type type)
    {
        var propertyValidations = new List<PropertyValidationDescriptor>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var rules = ExtractRulesForConceptType(assembly, property.PropertyType);
            if (rules.Count > 0)
            {
                propertyValidations.Add(new PropertyValidationDescriptor(property.Name.ToCamelCase(), [.. rules]));
            }
        }

        return propertyValidations;
    }

    /// <summary>
    /// Extract the rules a concept's validator declares, flattened and detached from the concept's own member name.
    /// </summary>
    /// <param name="assembly">Assembly to search for validators in.</param>
    /// <param name="type">The type to extract rules for; anything that is not a concept yields nothing.</param>
    /// <returns>Collection of validation rule descriptors.</returns>
    /// <remarks>
    /// A <c>ConceptValidator&lt;T&gt;</c> declares its rules against the concept's <c>Value</c> member. The generated
    /// TypeScript erases a concept to its underlying primitive, so on the client the owning property <em>is</em> that
    /// value — the rules are re-attributed to the owner and the inner member name is dropped.
    /// Only concepts sitting directly on a property or parameter are projected: the client-side rule builder resolves
    /// a single property name, so it cannot express a rule against a concept nested deeper in the graph.
    /// </remarks>
    public static IReadOnlyList<ValidationRuleDescriptor> ExtractRulesForConceptType(Assembly assembly, Type type)
    {
        var runtimeAssembly = RuntimeValidatorAssemblies.For(assembly) ?? assembly;
        var runtimeType = RuntimeValidatorAssemblies.For(type);

        if (!IsConcept(runtimeType))
        {
            return [];
        }

        return [.. ExtractFluentValidationRules(runtimeAssembly, runtimeType).SelectMany(_ => _.Rules)];
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

    /// <summary>
    /// Merges rules from the three sources that can contribute them, in the order of precedence the command and query
    /// paths both follow.
    /// </summary>
    /// <param name="fluentValidationRules">Rules from an explicit validator for the model.</param>
    /// <param name="conceptRules">Rules contributed by the validators of concept-typed members; additive.</param>
    /// <param name="dataAnnotationsRules">Rules from DataAnnotations; applied only where nothing else contributed.</param>
    /// <returns>The merged collection of property validation descriptors.</returns>
    public static IEnumerable<PropertyValidationDescriptor> MergeValidationRules(
        IEnumerable<PropertyValidationDescriptor> fluentValidationRules,
        IEnumerable<PropertyValidationDescriptor> conceptRules,
        IEnumerable<PropertyValidationDescriptor> dataAnnotationsRules)
    {
        var merged = new Dictionary<string, PropertyValidationDescriptor>();

        // Add FluentValidation rules first (they take precedence)
        foreach (var rule in fluentValidationRules)
        {
            merged[rule.PropertyName] = rule;
        }

        // Concept rules are additive rather than overriding: server-side both the explicit validator and the
        // concept's own validator run, so the client has to apply both to agree with it. Identical rules are
        // collapsed — the same concept can be reached both through a parameters class and through the parameter
        // itself, and emitting it twice would show the user the same message twice.
        foreach (var rule in conceptRules)
        {
            merged[rule.PropertyName] = merged.TryGetValue(rule.PropertyName, out var existing)
                ? existing with { Rules = Distinct([.. existing.Rules, .. rule.Rules]) }
                : rule with { Rules = Distinct(rule.Rules) };
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

    /// <summary>
    /// Collapses rules that are indistinguishable to a client — same rule, same arguments, same message.
    /// </summary>
    /// <param name="rules">The rules to collapse.</param>
    /// <returns>The distinct rules, in their original order.</returns>
    static List<ValidationRuleDescriptor> Distinct(IEnumerable<ValidationRuleDescriptor> rules)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var distinct = new List<ValidationRuleDescriptor>();

        foreach (var rule in rules)
        {
            var key = $"{rule.RuleName}({string.Join(',', rule.Arguments.Select(_ => _?.ToString() ?? string.Empty))}):{rule.ErrorMessage}";
            if (seen.Add(key))
            {
                distinct.Add(rule);
            }
        }

        return distinct;
    }

    /// <summary>
    /// Check whether a type is a concept, by name rather than by type identity.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns><see langword="true"/> if the type derives from <c>ConceptAs&lt;T&gt;</c>; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// The types inspected here come from a load context of their own, so <c>typeof(ConceptAs&lt;&gt;)</c> as the
    /// generator sees it is a different type from the one the target project derives from. Every other type test in
    /// this class compares names for the same reason.
    /// </remarks>
    static bool IsConcept(Type type)
    {
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition().FullName == ConceptAsType)
            {
                return true;
            }
        }

        return false;
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
                        rules.Add(new ValidationRuleDescriptor("matches", [new RegularExpressionPattern(pattern)], errorMessage));
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

    static List<PropertyValidationDescriptor> ExtractFluentValidationRules(Assembly assembly, Type type)
    {
        var validatorType = FindValidatorForType(assembly, type);
        if (validatorType == null)
        {
            return [];
        }

        try
        {
            var validator = CreateValidatorInstance(validatorType);
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

    static object? CreateValidatorInstance(Type validatorType)
    {
        try
        {
            // First, try to create an instance without parameters (parameterless constructor)
            var parameterlessConstructor = validatorType.GetConstructor(Type.EmptyTypes);
            if (parameterlessConstructor != null)
            {
                return Activator.CreateInstance(validatorType);
            }

            // If no parameterless constructor, find the first constructor and create default values for its parameters
            var constructors = validatorType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
            {
                return null;
            }

            var constructor = constructors[0];
            var parameters = constructor.GetParameters();
            var parameterValues = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;

                // Create default value for parameter type
                if (parameterType.IsValueType)
                {
                    parameterValues[i] = Activator.CreateInstance(parameterType);
                }
                else
                {
                    parameterValues[i] = null;
                }
            }

            return Activator.CreateInstance(validatorType, parameterValues);
        }
        catch
        {
            return null;
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

            // A message declared lazily - .WithMessage(_ => Messages.Something) - is held as a factory instead, and
            // is deliberately left unprojected. A factory is deferred precisely because its value is not known yet,
            // so calling it here would answer for a different process, on a different machine, at a different time,
            // under ambient state this one cannot stand in for - the culture, the clock, a tenant, a feature flag.
            // A delegate is opaque, so there is no way to tell a factory that returns a constant from one that does
            // not, which leaves not calling it as the only guess-free move. The cost is small and the rule still
            // mirrors: the generated client rule falls back to its own default message, and any message the author
            // actually authored is resolved by the server, correctly, for the request that asked for it.

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
        // ExactLengthValidator has no Length property - it is a LengthValidator whose Min and Max both carry the
        // exact length.
        var validatorType = validator.GetType();
        var minProperty = validatorType.GetProperty("Min", BindingFlags.Public | BindingFlags.Instance);
        var length = minProperty?.GetValue(validator) as int? ?? 0;
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

        // The client-side comparison validators operate on numbers, so a non-numeric constant comparison cannot be
        // projected. In practice this is a "must be set" sentinel such as GreaterThan(DateOnly.MinValue), which the
        // server still enforces; emitting it here would only produce a client rule the browser cannot evaluate.
        if (!IsNumeric(valueToCompare))
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

    static bool IsNumeric(object value) => value
        is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    static ValidationRuleDescriptor ExtractRegexRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var expressionProperty = validatorType.GetProperty("Expression");
        var pattern = expressionProperty?.GetValue(validator)?.ToString() ?? string.Empty;

        return new ValidationRuleDescriptor("matches", [new RegularExpressionPattern(pattern)], errorMessage);
    }
}
