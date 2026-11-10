// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extracts validation rules from FluentValidation validators and DataAnnotations attributes.
/// </summary>
public static class ValidationRulesExtractor
{
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

    static readonly Dictionary<string, string> _simpleAttributeRules = new()
    {
        ["System.ComponentModel.DataAnnotations.RequiredAttribute"] = "notEmpty",
        ["System.ComponentModel.DataAnnotations.EmailAddressAttribute"] = "emailAddress",
        ["System.ComponentModel.DataAnnotations.PhoneAttribute"] = "phone",
        ["System.ComponentModel.DataAnnotations.UrlAttribute"] = "url",
        ["System.ComponentModel.DataAnnotations.CreditCardAttribute"] = "creditCard"
    };

    static readonly (string InterfaceName, Func<object, string?, ValidationRuleDescriptor?> Extract)[] _validatorRuleExtractors =
    [
        (NotNullValidatorType, (_, message) => new ValidationRuleDescriptor("notNull", [], message)),
        (NotEmptyValidatorType, (_, message) => new ValidationRuleDescriptor("notEmpty", [], message)),
        (EmailValidatorType, (_, message) => new ValidationRuleDescriptor("emailAddress", [], message)),

        // The specific length validators come before the generic ILengthValidator.
        (MinimumLengthValidatorType, ExtractMinimumLengthRule),
        (MaximumLengthValidatorType, ExtractMaximumLengthRule),
        (ExactLengthValidatorType, ExtractExactLengthRule),
        (LengthValidatorType, ExtractLengthRule),
        (ComparisonValidatorType, ExtractComparisonRule),
        (RegularExpressionValidatorType, ExtractRegexRule)
    ];

    /// <summary>
    /// Extract validation rules for a specific type using FluentValidation validators.
    /// </summary>
    /// <param name="assembly">Assembly being generated; concept validators may also come from their declaring assembly.</param>
    /// <param name="type">The type to extract validation rules for.</param>
    /// <param name="includeConceptRules">Whether to infer rules from concept-typed properties.</param>
    /// <param name="includeDataAnnotations">Whether to include DataAnnotations as fallback rules.</param>
    /// <returns>Collection of property validation descriptors.</returns>
    public static IEnumerable<PropertyValidationDescriptor> ExtractValidationRules(Assembly assembly, Type type, bool includeConceptRules = true, bool includeDataAnnotations = true)
    {
        // Discovery describes every artifact before excluded ones are filtered out. An excluded type is never
        // emitted, so its validator must not run - and must not fail generation when it cannot be constructed.
        if (type.IsExcluded())
        {
            return [];
        }

        // A FluentValidation rule only exists once its validator's constructor has run, which a metadata-only type
        // cannot do - see RuntimeValidatorAssemblies.
        var runtimeAssembly = RuntimeValidatorAssemblies.For(assembly) ?? assembly;
        var runtimeType = RuntimeValidatorAssemblies.For(type);

        // Try FluentValidation first
        var fluentValidationRules = ExtractFluentValidationRules(runtimeAssembly, runtimeType).ToList();

        // Then the rules contributed by the validators of any concept-typed properties
        var conceptRules = includeConceptRules ? ExtractConceptRules(assembly, type).ToList() : [];

        // Then extract DataAnnotations
        var dataAnnotationsRules = includeDataAnnotations ? ExtractDataAnnotationsRules(type).ToList() : [];

        // Merge the rules - FluentValidation takes precedence
        return MergeValidationRules(fluentValidationRules, conceptRules, dataAnnotationsRules);
    }

    /// <summary>
    /// Extract the validation rules that <c>ConceptValidator&lt;T&gt;</c> validators contribute to a type's
    /// concept-typed properties, attributed to the property carrying the concept.
    /// </summary>
    /// <param name="assembly">Assembly being generated; concept validators may also come from their declaring assembly.</param>
    /// <param name="type">The type whose properties to inspect.</param>
    /// <returns>Collection of property validation descriptors.</returns>
    /// <remarks>
    /// Whether a value is well formed is a property of its type, so a concept's validator already runs server-side
    /// wherever that concept appears. Projecting it here means declaring it once also validates in the browser,
    /// rather than the client silently enforcing less than the server. Nullable properties without a Required
    /// annotation omit inferred presence rules only: other client rules accept null and undefined while validating supplied values.
    /// </remarks>
    public static IEnumerable<PropertyValidationDescriptor> ExtractConceptRules(Assembly assembly, Type type)
    {
        var propertyValidations = new List<PropertyValidationDescriptor>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var isOptional = property.IsOptional() && !property.GetCustomAttributesData().Any(_ => _.AttributeType.FullName == typeof(RequiredAttribute).FullName);
            var rules = ExtractRulesForConceptType(assembly, property.PropertyType, isOptional);
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
    /// <param name="assembly">Assembly being generated; concept validators may also come from their declaring assembly.</param>
    /// <param name="type">The type to extract rules for; anything that is not a concept yields nothing.</param>
    /// <param name="isOptional">Whether the owning member is optional and must omit inferred presence rules.</param>
    /// <returns>Collection of validation rule descriptors.</returns>
    /// <remarks>
    /// A <c>ConceptValidator&lt;T&gt;</c> declares its rules against the concept's <c>Value</c> member. The generated
    /// TypeScript erases a concept to its underlying primitive, so on the client the owning property <em>is</em> that
    /// value — the rules are re-attributed to the owner and the inner member name is dropped.
    /// Only concepts sitting directly on a property or parameter are projected: the client-side rule builder resolves
    /// a single property name, so it cannot express a rule against a concept nested deeper in the graph.
    /// </remarks>
    public static IReadOnlyList<ValidationRuleDescriptor> ExtractRulesForConceptType(Assembly assembly, Type type, bool isOptional = false)
    {
        var runtimeAssembly = RuntimeValidatorAssemblies.For(assembly) ?? assembly;
        var runtimeType = RuntimeValidatorAssemblies.For(type);

        if (!IsConcept(runtimeType))
        {
            return [];
        }

        // Only these two client rules reject null/undefined; all other supported rules validate supplied values.
        return [.. ExtractFluentValidationRules(runtimeAssembly, runtimeType)
            .SelectMany(_ => _.Rules)
            .Where(_ => !isOptional || _.RuleName is not ("notEmpty" or "notNull"))];
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
            var key = $"{rule.RuleName}({string.Join(',', rule.Arguments.Select(_ => _?.ToString() ?? string.Empty))}):{rule.ErrorMessage}:{rule.Severity}";
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
            var attributeTypeName = attributeData.AttributeType.FullName;
            if (attributeTypeName is not null)
            {
                rules.AddRange(ExtractRulesFromAttribute(attributeTypeName, attributeData));
            }
        }

        return rules;
    }

    static List<ValidationRuleDescriptor> ExtractRulesFromAttribute(string attributeTypeName, CustomAttributeData attributeData)
    {
        var errorMessage = GetNamedArgument<string>(attributeData, "ErrorMessage");

        if (_simpleAttributeRules.TryGetValue(attributeTypeName, out var ruleName))
        {
            return [new ValidationRuleDescriptor(ruleName, [], errorMessage)];
        }

        return attributeTypeName switch
        {
            "System.ComponentModel.DataAnnotations.StringLengthAttribute" => [ExtractStringLengthRuleFromData(GetNamedArgument<int>(attributeData, "MinimumLength"), GetConstructorArgument<int>(attributeData, 0), errorMessage)],
            "System.ComponentModel.DataAnnotations.MinLengthAttribute" => [new ValidationRuleDescriptor("minLength", [GetConstructorArgument<int>(attributeData, 0)], errorMessage)],
            "System.ComponentModel.DataAnnotations.MaxLengthAttribute" => [new ValidationRuleDescriptor("maxLength", [GetConstructorArgument<int>(attributeData, 0)], errorMessage)],
            _ => ExtractValueRulesFromAttribute(attributeTypeName, attributeData, errorMessage)
        };
    }

    static List<ValidationRuleDescriptor> ExtractValueRulesFromAttribute(string attributeTypeName, CustomAttributeData attributeData, string? errorMessage) => attributeTypeName switch
    {
        "System.ComponentModel.DataAnnotations.RangeAttribute" => ExtractRangeRulesFromData(GetConstructorArgument<object>(attributeData, 0), GetConstructorArgument<object>(attributeData, 1), errorMessage),
        "System.ComponentModel.DataAnnotations.RegularExpressionAttribute" => [new ValidationRuleDescriptor("matches", [new RegularExpressionPattern(GetConstructorArgument<string>(attributeData, 0) ?? string.Empty)], errorMessage)],
        _ => []
    };

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
        var validatorType = ValidatorTypes.Find(assembly, type);
        if (validatorType == null)
        {
            return [];
        }

        try
        {
            var (descriptor, members, getRulesMethod) = DescribeValidator(validatorType);

            return [.. members
                .Cast<object>()
                .Select(_ => DescribeMember(descriptor, getRulesMethod, _))
                .OfType<PropertyValidationDescriptor>()];
        }
        catch (Exception exception) when (exception is not ValidationRulesCouldNotBeExtracted)
        {
            throw new ValidationRulesCouldNotBeExtracted(type, validatorType, exception);
        }
    }

    static PropertyValidationDescriptor? DescribeMember(object descriptor, MethodInfo getRulesMethod, object member)
    {
        var memberName = member.GetType().GetProperty("Key")?.GetValue(member)?.ToString();
        var propertyName = memberName?.ToCamelCase();

        if (string.IsNullOrEmpty(propertyName))
        {
            return null;
        }

        // Read the owning rules as well as their components: block-level When conditions live on the
        // owning rule, while chained When conditions live on individual components. Neither can be
        // represented by the client rule builder, so leave them for server-side validation.
        if (getRulesMethod.Invoke(descriptor, [memberName]) is not System.Collections.IEnumerable memberRules)
        {
            return null;
        }

        var rules = memberRules.Cast<object>()
            .Where(_ => !HasCondition(_))
            .SelectMany(ExtractRulesFromRule)
            .ToList();

        return rules.Count > 0 ? new PropertyValidationDescriptor(propertyName, [.. rules]) : null;
    }

    static IEnumerable<ValidationRuleDescriptor> ExtractRulesFromRule(object rule) =>
        rule.GetType().GetProperty("Components")?.GetValue(rule) is System.Collections.IEnumerable components
            ? components.Cast<object>().SelectMany(ExtractRulesFromComponent)
            : [];

    static (object Descriptor, System.Collections.IEnumerable Members, MethodInfo GetRulesMethod) DescribeValidator(Type validatorType)
    {
        var validator = CreateValidatorInstance(validatorType)
            ?? throw new InvalidOperationException("Validator could not be constructed");
        var createDescriptorMethod = validatorType.GetMethod("CreateDescriptor", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Validator does not expose CreateDescriptor");
        var descriptor = createDescriptorMethod.Invoke(validator, null)
            ?? throw new InvalidOperationException("CreateDescriptor returned null");
        var getMembersMethod = descriptor.GetType().GetMethod("GetMembersWithValidators")
            ?? throw new InvalidOperationException("Validator descriptor does not expose GetMembersWithValidators");
        var getRulesMethod = descriptor.GetType().GetMethod("GetRulesForMember")
            ?? throw new InvalidOperationException("Validator descriptor does not expose GetRulesForMember");
        var members = getMembersMethod.Invoke(descriptor, null)
            ?? throw new InvalidOperationException("GetMembersWithValidators returned null");

        return (descriptor, (System.Collections.IEnumerable)members, getRulesMethod);
    }

    static object? CreateValidatorInstance(Type validatorType)
    {
        if (validatorType.Assembly.ReflectionOnly)
        {
            throw RuntimeValidatorAssemblies.LoadFailureFor(validatorType.Assembly)
                ?? new InvalidOperationException("Validator assembly could not be loaded for execution");
        }

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
            throw new InvalidOperationException("Validator has no public constructor");
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

    static bool HasCondition(object rule) =>
        rule.GetType().GetProperty("HasCondition")?.GetValue(rule) is true ||
        rule.GetType().GetProperty("HasAsyncCondition")?.GetValue(rule) is true;

    static List<ValidationRuleDescriptor> ExtractRulesFromComponent(object component)
    {
        var validator = component.GetType().GetProperty("Validator")?.GetValue(component);
        if (validator is null || HasCondition(component))
        {
            return [];
        }

        var ruleDescriptor = ExtractRuleFromValidator(validator, component);
        return ruleDescriptor is not null ? [ruleDescriptor with { Severity = GetSeverity(component) }] : [];
    }

    static ValidationRuleDescriptor? ExtractRuleFromValidator(object validator, object component)
    {
        var errorMessage = GetCustomErrorMessage(component);

        // Check validator type by interface
        var interfaceNames = validator.GetType().GetInterfaces().Select(i => i.FullName ?? i.Name).ToHashSet();

        foreach (var (interfaceName, extract) in _validatorRuleExtractors)
        {
            if (interfaceNames.Contains(interfaceName))
            {
                return extract(validator, errorMessage);
            }
        }

        return null;
    }

    /// <summary>
    /// Reads only FluentValidation's constant severity provider, without invoking request-dependent delegates.
    /// </summary>
    /// <param name="component">The FluentValidation rule component.</param>
    /// <returns>The Arc severity, or null if it cannot be determined statically.</returns>
    static int? GetSeverity(object component)
    {
        var property = component.GetType().GetProperty("SeverityProvider", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property is null)
        {
            return null;
        }

        var provider = property.GetValue(component);
        if (provider is null)
        {
            return 3; // FluentValidation defaults to Error.
        }

        return provider is Delegate severityProvider && IsDefaultSeverityProvider(severityProvider)
            ? ReadConstantSeverity(severityProvider)
            : null;
    }

    static bool IsDefaultSeverityProvider(Delegate severityProvider) =>
        severityProvider.Method.DeclaringType?.DeclaringType?.FullName == "FluentValidation.DefaultValidatorOptions";

    static bool IsSeverityField(FieldInfo field) =>
        field.Name == "severity" && field.FieldType.FullName == "FluentValidation.Severity";

    static int? ReadConstantSeverity(Delegate severityProvider)
    {
        var fields = severityProvider.Target?.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fields is not { Length: 1 } || !IsSeverityField(fields[0]))
        {
            return null;
        }

        return fields[0].GetValue(severityProvider.Target)?.ToString() switch
        {
            "Info" => 1,
            "Warning" => 2,
            "Error" => 3,
            _ => null
        };
    }

    static string? GetCustomErrorMessage(object component)
    {
        try
        {
            var errorMessage = ReadErrorMessageField(component);
            return string.IsNullOrEmpty(errorMessage) ? ReadErrorMessageSource(component) : errorMessage;
        }
        catch
        {
            // Ignore
            return null;
        }
    }

    static string? ReadErrorMessageField(object component)
    {
        // FluentValidation 12.x stores custom messages in the _errorMessage field on RuleComponent
        var field = component.GetType().GetField("_errorMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(component) as string;
    }

    static string? ReadErrorMessageSource(object component)
    {
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

        var errorMessage = errorMessageSource?.GetType()
            .GetProperty("ErrorMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(errorMessageSource) as string;

        return string.IsNullOrEmpty(errorMessage) ? null : errorMessage;
    }

    static ValidationRuleDescriptor ExtractLengthRule(object validator, string? errorMessage)
    {
        var min = ReadLengthBound(validator, "Min", -1);
        var max = ReadLengthBound(validator, "Max", -1);

        // Check for both min and max being set (both non-negative)
        if (min >= 0 && max >= 0)
        {
            return new ValidationRuleDescriptor("length", [min, max], errorMessage);
        }

        // Check for minimum length only
        if (min >= 0 && max is -1 or int.MaxValue)
        {
            return new ValidationRuleDescriptor("minLength", [min], errorMessage);
        }

        // Check for maximum length only
        if (max >= 0 && min is -1 or 0)
        {
            return new ValidationRuleDescriptor("maxLength", [max], errorMessage);
        }

        return new ValidationRuleDescriptor("notEmpty", [], errorMessage);
    }

    static int ReadLengthBound(object validator, string propertyName, int fallback) =>
        validator.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(validator) as int? ?? fallback;

    static ValidationRuleDescriptor ExtractMinimumLengthRule(object validator, string? errorMessage) =>
        new("minLength", [ReadLengthBound(validator, "Min", 0)], errorMessage);

    static ValidationRuleDescriptor ExtractMaximumLengthRule(object validator, string? errorMessage) =>
        new("maxLength", [ReadLengthBound(validator, "Max", 0)], errorMessage);

    static ValidationRuleDescriptor ExtractExactLengthRule(object validator, string? errorMessage)
    {
        // ExactLengthValidator has no Length property - it is a LengthValidator whose Min and Max both carry the
        // exact length.
        var length = ReadLengthBound(validator, "Min", 0);
        return new ValidationRuleDescriptor("length", [length, length], errorMessage);
    }

    static ValidationRuleDescriptor? ExtractComparisonRule(object validator, string? errorMessage)
    {
        var validatorType = validator.GetType();
        var valueToCompare = validatorType.GetProperty("ValueToCompare")?.GetValue(validator);

        // The client-side comparison validators operate on numbers, so a non-numeric constant comparison cannot be
        // projected. In practice this is a "must be set" sentinel such as GreaterThan(DateOnly.MinValue), which the
        // server still enforces; emitting it here would only produce a client rule the browser cannot evaluate.
        if (valueToCompare is null || !IsNumeric(valueToCompare))
        {
            return null;
        }

        // Comparison is an enum, get its string value
        var comparisonName = validatorType.GetProperty("Comparison")?.GetValue(validator)?.ToString();
        var ruleName = ToComparisonRuleName(comparisonName);

        return ruleName is null ? null : new ValidationRuleDescriptor(ruleName, [valueToCompare], errorMessage);
    }

    static string? ToComparisonRuleName(string? comparisonName) => comparisonName switch
    {
        "GreaterThan" => "greaterThan",
        "GreaterThanOrEqual" => "greaterThanOrEqual",
        "LessThan" => "lessThan",
        "LessThanOrEqual" => "lessThanOrEqual",
        _ => null
    };

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
