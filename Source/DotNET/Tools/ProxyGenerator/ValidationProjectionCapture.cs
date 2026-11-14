// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Collects allowlisted metadata for one extraction, never validator values.</summary>
internal sealed class ValidationProjectionCapture
{
    readonly List<ValidationProjectionDetail> _details;
    readonly string? _member;
    readonly string _source;
    readonly bool _optional;

    internal ValidationProjectionCapture() : this([], null, "validator", false) { }

    ValidationProjectionCapture(List<ValidationProjectionDetail> details, string? member, string source, bool optional)
    {
        _details = details;
        _member = member;
        _source = source;
        _optional = optional;
    }

    internal IReadOnlyList<ValidationProjectionDetail> Details => _details;

    internal ValidationProjectionCapture ForMember(string member) => new(_details, _member ?? member, _source, _optional);

    internal ValidationProjectionCapture ForConcept(string member, bool optional) => new(_details, member, "concept", optional);

    internal void Omitted(ValidationRuleKind kind, string reason) => _details.Add(new(_member, kind, _source, "omitted", reason));

    internal void Projected(ValidationRuleDescriptor rule)
    {
        if (rule.Severity is null)
        {
            Omitted(Kind(rule.RuleName), ValidationProjectionReasons.DynamicSeverity);
            return;
        }
        if (_optional && rule.RuleName is "notNull" or "notEmpty")
        {
            Omitted(ValidationRuleKind.Required, ValidationProjectionReasons.OptionalConceptPresence);
            return;
        }
        _details.Add(new(_member, Kind(rule.RuleName), _source, "projected", ValidationProjectionReasons.Supported, rule.Severity));
    }

    internal void Annotation(string member, bool projected) => _details.Add(new(member, ValidationRuleKind.DataAnnotation, "data-annotation", projected ? "projected" : "omitted", projected ? ValidationProjectionReasons.Supported : ValidationProjectionReasons.OverriddenAnnotation));

    internal void Annotations(IEnumerable<PropertyValidationDescriptor> annotations, IEnumerable<PropertyValidationDescriptor> explicitRules)
    {
        var existing = explicitRules.Select(_ => _.PropertyName).ToHashSet(StringComparer.Ordinal);
        foreach (var annotation in annotations) Annotation(annotation.PropertyName, !existing.Contains(annotation.PropertyName));
    }

    static ValidationRuleKind Kind(string name) => name switch
    {
        "notEmpty" or "notNull" => ValidationRuleKind.Required,
        "length" or "minLength" or "maxLength" => ValidationRuleKind.Length,
        "matches" => ValidationRuleKind.Regex,
        "emailAddress" => ValidationRuleKind.Email,
        "greaterThan" or "greaterThanOrEqual" or "lessThan" or "lessThanOrEqual" => ValidationRuleKind.Comparison,
        _ => ValidationRuleKind.Unknown
    };
}
