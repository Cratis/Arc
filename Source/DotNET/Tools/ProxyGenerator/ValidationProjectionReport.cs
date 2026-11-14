// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Formats only allowlisted observations from a successful extraction pass.</summary>
internal sealed class ValidationProjectionReport
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter<ValidationRuleKind>(JsonNamingPolicy.KebabCaseLower),
            new JsonStringEnumConverter<ValidationProjectionSource>(JsonNamingPolicy.KebabCaseLower)
        }
    };
    readonly List<ValidationProjectionRecord> _records = [];

    /// <summary>Records an extraction decision.</summary>
    /// <param name="record">The allowlisted decision.</param>
    internal void Add(ValidationProjectionRecord record) => _records.Add(record);

    /// <summary>Gets distinct records in deterministic order.</summary>
    /// <returns>The ordered decisions.</returns>
    internal IEnumerable<ValidationProjectionRecord> Ordered() => _records
        .OrderBy(_ => _.Endpoint.Kind, StringComparer.Ordinal)
        .ThenBy(_ => _.Endpoint.Name, StringComparer.Ordinal)
        .ThenBy(_ => _.Member, StringComparer.Ordinal)
        .ThenBy(_ => JsonNamingPolicy.KebabCaseLower.ConvertName(_.RuleKind.ToString()), StringComparer.Ordinal)
        .ThenBy(_ => _.Outcome, StringComparer.Ordinal)
        .ThenBy(_ => _.Reason, StringComparer.Ordinal)
        .ThenBy(_ => _.Endpoint.Route, StringComparer.Ordinal)
        .ThenBy(_ => _.Source)
        .ThenBy(_ => _.Severity);

    /// <summary>Formats human-readable extraction decisions.</summary>
    /// <returns>The text report.</returns>
    internal string ToText() => string.Join(Environment.NewLine, Ordered().Select(_ =>
        $"{_.Endpoint.Kind} {_.Endpoint.Name} {_.Endpoint.Route} {_.Member ?? "(type)"}: {JsonNamingPolicy.KebabCaseLower.ConvertName(_.RuleKind.ToString())} {JsonNamingPolicy.KebabCaseLower.ConvertName(_.Source.ToString())} {_.Outcome} {_.Reason} severity={_.Severity?.ToString() ?? "unknown"}")
        .Append($"Validation projection: {_records.Count} record(s). Server validation remains authoritative."));

    /// <summary>Serializes the stable report schema.</summary>
    /// <returns>The JSON document.</returns>
    internal string ToJson() => JsonSerializer.Serialize(new { SchemaVersion = 1, Records = Ordered() }, _jsonOptions);

    /// <summary>Marks annotations superseded by higher-precedence rules.</summary>
    /// <param name="endpoint">The owning endpoint.</param>
    /// <param name="member">The camelCase annotated member.</param>
    internal void OverrideAnnotation(ValidationProjectionEndpoint endpoint, string member)
    {
        for (var index = 0; index < _records.Count; index++)
        {
            var record = _records[index];
            if (IsAnnotation(record, endpoint, member)) _records[index] = record with { Outcome = "omitted", Reason = ValidationProjectionReasons.OverriddenAnnotation };
        }
    }

    /// <summary>Adds a type-level decision when no client rules were projected.</summary>
    /// <param name="endpoint">The completed endpoint.</param>
    internal void CompleteEndpoint(ValidationProjectionEndpoint endpoint)
    {
        if (_records.Exists(_ => _.Endpoint == endpoint && _.Outcome == "projected")) return;
        Add(new(endpoint, null, ValidationRuleKind.Unknown, ValidationProjectionSource.Validator, "omitted", ValidationProjectionReasons.NoProjectableRules));
    }

    static bool IsAnnotation(ValidationProjectionRecord record, ValidationProjectionEndpoint endpoint, string member) =>
        record.Endpoint == endpoint && record.Member == member && record.Source == ValidationProjectionSource.DataAnnotation;
}
