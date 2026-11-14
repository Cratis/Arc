// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

public static partial class ValidationRulesExtractor
{
    static bool ShouldExtractRule(object rule)
    {
        if (!HasCondition(rule)) return true;
        if (ValidationProjectionRecorder.IsEnabled && rule.GetType().GetProperty("Components")?.GetValue(rule) is System.Collections.IEnumerable components)
        {
            foreach (var component in components.Cast<object>())
            {
                var validator = component.GetType().GetProperty("Validator")?.GetValue(component);
                ObserveCondition(rule, validator, ValidationProjectionReasons.BlockCondition);
            }
        }
        return false;
    }

    static void ObserveCondition(object condition, object? validator, string reason)
    {
        if (!ValidationProjectionRecorder.IsEnabled) return;
        var conditionReason = ValidationProjectionRecorder.IsConcept
            ? ValidationProjectionReasons.ConceptCondition
            : condition.GetType().GetProperty("HasAsyncCondition")?.GetValue(condition) is true
                ? ValidationProjectionReasons.AsyncCondition
                : reason;
        ValidationProjectionRecorder.Record(KindForValidator(validator), "omitted", conditionReason);
    }

    static void ObserveUnsupported(object validator)
    {
        if (!ValidationProjectionRecorder.IsEnabled) return;
        ValidationProjectionRecorder.Record(KindForValidator(validator), "omitted", ValidationProjectionReasons.UnsupportedValidator);
    }

    static void ObserveProjected(ValidationRuleDescriptor rule)
    {
        if (!ValidationProjectionRecorder.IsEnabled) return;
        var optionalPresence = ValidationProjectionRecorder.IsOptionalConcept && rule.RuleName is "notEmpty" or "notNull";
        var reason = optionalPresence
            ? ValidationProjectionReasons.OptionalConceptPresence
            : ValidationProjectionRecorder.IsConcept ? ValidationProjectionReasons.ConceptProjected : ValidationProjectionReasons.Projected;
        ValidationProjectionRecorder.Record(KindForRule(rule.RuleName), optionalPresence ? "omitted" : "projected", reason, rule.Severity);
    }

    static void ObserveAnnotations(IEnumerable<ValidationRuleDescriptor> rules)
    {
        if (!ValidationProjectionRecorder.IsEnabled) return;
        foreach (var rule in rules)
        {
            ValidationProjectionRecorder.Record(KindForRule(rule.RuleName), "projected", ValidationProjectionReasons.Projected, rule.Severity);
        }
    }

    static ValidationRuleKind KindForValidator(object? validator)
    {
        var interfaces = validator?.GetType().GetInterfaces();
        var known = _validatorRuleExtractors.FirstOrDefault(candidate => interfaces?.Any(_ => _.FullName == candidate.InterfaceName) is true).InterfaceName;
        return known switch
        {
            NotNullValidatorType or NotEmptyValidatorType => ValidationRuleKind.Required,
            MinimumLengthValidatorType or MaximumLengthValidatorType or ExactLengthValidatorType or LengthValidatorType => ValidationRuleKind.Length,
            ComparisonValidatorType => ValidationRuleKind.Comparison,
            RegularExpressionValidatorType => ValidationRuleKind.Regex,
            EmailValidatorType => ValidationRuleKind.Email,
            _ => ValidationRuleKind.Unknown
        };
    }

    static ValidationRuleKind KindForRule(string rule) => ProjectionRuleKinds.For(rule);

    static class ProjectionRuleKinds
    {
        static readonly Dictionary<string, ValidationRuleKind> _kinds = new()
        {
            ["notNull"] = ValidationRuleKind.Required,
            ["notEmpty"] = ValidationRuleKind.Required,
            ["minLength"] = ValidationRuleKind.Length,
            ["maxLength"] = ValidationRuleKind.Length,
            ["length"] = ValidationRuleKind.Length,
            ["inclusiveBetween"] = ValidationRuleKind.Range,
            ["exclusiveBetween"] = ValidationRuleKind.Range,
            ["greaterThan"] = ValidationRuleKind.Comparison,
            ["greaterThanOrEqual"] = ValidationRuleKind.Comparison,
            ["lessThan"] = ValidationRuleKind.Comparison,
            ["lessThanOrEqual"] = ValidationRuleKind.Comparison,
            ["equal"] = ValidationRuleKind.Comparison,
            ["notEqual"] = ValidationRuleKind.Comparison,
            ["matches"] = ValidationRuleKind.Regex,
            ["emailAddress"] = ValidationRuleKind.Email
        };

        internal static ValidationRuleKind For(string rule) => _kinds.GetValueOrDefault(rule, ValidationRuleKind.DataAnnotation);
    }
}
