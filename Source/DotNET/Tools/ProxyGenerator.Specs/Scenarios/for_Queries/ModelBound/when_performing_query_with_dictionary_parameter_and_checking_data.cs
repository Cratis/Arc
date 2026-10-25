// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_performing_query_with_dictionary_parameter_and_checking_data : given.a_scenario_web_application
{
    readonly Dictionary<string, string> _parts = new() { ["quoted"] = "\"abc\"", ["number"] = "42", ["comma"] = "a,b" };
    QueryExecutionResult<DictionaryParameterReadModel> _get;
    QueryExecutionResult<DictionaryParameterReadModel> _query;

    async Task Because()
    {
        var parameters = new Dictionary<string, object> { ["parts"] = _parts };
        LoadQueryProxy<DictionaryParameterReadModel>(nameof(DictionaryParameterReadModel.GetParts));
        _get = await Bridge.PerformQueryViaProxyAsync<DictionaryParameterReadModel>(nameof(DictionaryParameterReadModel.GetParts), parameters);
        LoadQueryProxy<DictionaryParameterReadModel>(nameof(DictionaryParameterReadModel.QueryParts));
        _query = await Bridge.PerformQueryViaProxyAsync<DictionaryParameterReadModel>(nameof(DictionaryParameterReadModel.QueryParts), parameters);
    }

    [Fact] void should_succeed_over_get() => _get.Result!.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_over_query() => _query.Result!.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_get_values() => AssertValues(_get.RawJson);
    [Fact] void should_preserve_query_values() => AssertValues(_query.RawJson);

    void AssertValues(string json)
    {
        using var document = JsonDocument.Parse(json);
        var parts = document.RootElement.GetProperty("data").GetProperty("parts");
        foreach (var (key, value) in _parts) parts.GetProperty(key).GetString().ShouldEqual(value);
    }
}

[ReadModel]
public record DictionaryParameterReadModel(Dictionary<string, string> Parts)
{
    public static DictionaryParameterReadModel GetParts(IDictionary<string, string> parts) => new(new Dictionary<string, string>(parts));

    [QueryHttpMethod(QueryHttpMethod.Query)]
    public static DictionaryParameterReadModel QueryParts(IReadOnlyDictionary<string, string> parts) => new(new Dictionary<string, string>(parts));
}
