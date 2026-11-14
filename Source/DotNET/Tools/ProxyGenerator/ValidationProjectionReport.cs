// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Formats only safe metadata collected by the existing discovery pass.</summary>
internal sealed class ValidationProjectionReport
{
    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter<ValidationRuleKind>(JsonNamingPolicy.KebabCaseLower) }
    };
    readonly List<ValidationProjectionRecord> _records = [];

    internal void Collect(IEnumerable<CommandDescriptor> commands, IEnumerable<QueryDescriptor> queries)
    {
        foreach (var command in commands) Add(new("command", command.Name, command.Route), command.ValidationProjection);
        foreach (var query in queries) Add(new("query", query.Name, query.Route), query.ValidationProjection);
    }

    internal IEnumerable<ValidationProjectionRecord> Ordered() => _records
        .OrderBy(_ => _.Endpoint.Kind, StringComparer.Ordinal)
        .ThenBy(_ => _.Endpoint.Name, StringComparer.Ordinal)
        .ThenBy(_ => _.Endpoint.Route, StringComparer.Ordinal)
        .ThenBy(_ => _.Projection.Member, StringComparer.Ordinal)
        .ThenBy(_ => _.Projection.RuleKind)
        .ThenBy(_ => _.Projection.Outcome, StringComparer.Ordinal)
        .ThenBy(_ => _.Projection.Reason, StringComparer.Ordinal)
        .ThenBy(_ => _.Projection.Source, StringComparer.Ordinal)
        .ThenBy(_ => _.Projection.Severity);

    internal string ToText() => string.Join(Environment.NewLine, Ordered().Select(_ =>
        $"{_.Endpoint.Kind} {_.Endpoint.Name} {_.Endpoint.Route} {_.Projection.Member ?? "(type)"}: {JsonNamingPolicy.KebabCaseLower.ConvertName(_.Projection.RuleKind.ToString())} {_.Projection.Source} {_.Projection.Outcome} {_.Projection.Reason} severity={_.Projection.Severity?.ToString() ?? "unknown"}")
        .Append($"Validation projection: {_records.Count} record(s). Server validation remains authoritative."));

    internal string ToJson() => JsonSerializer.Serialize(new { SchemaVersion = 1, Validation = Ordered() }, _jsonOptions);

    internal string ToCombinedJson(DriftReport drift) => JsonSerializer.Serialize(new { SchemaVersion = 1, Drift = drift.Ordered(), Validation = Ordered() }, _jsonOptions);

    void Add(ValidationProjectionEndpoint endpoint, IReadOnlyList<ValidationProjectionDetail> projection)
    {
        _records.AddRange(projection.Select(_ => new ValidationProjectionRecord(endpoint, _)));
        if (!projection.Any(_ => _.Outcome == "projected")) _records.Add(new(endpoint, new(null, ValidationRuleKind.Unknown, "validator", "omitted", ValidationProjectionReasons.NoProjectableRules)));
    }
}
