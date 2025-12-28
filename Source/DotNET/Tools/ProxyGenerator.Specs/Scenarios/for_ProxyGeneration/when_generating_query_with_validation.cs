// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_query_with_validation : Specification, IDisposable
{
    JavaScriptRuntime _runtime = null!;
    string _generatedCode = null!;
    QueryDescriptor _descriptor = null!;
    bool _typeScriptIsValid;

    void Establish()
    {
        _runtime = new JavaScriptRuntime();

        var queryType = typeof(QueryWithValidation);
        var method = queryType.GetMethod("Handle") ?? typeof(object).GetMethod("GetHashCode")!;
        var properties = queryType.GetProperties()
            .Select(p => p.ToPropertyDescriptor())
            .ToList();

        var validationRules = ValidationRulesExtractor.ExtractValidationRules(queryType.Assembly, queryType);

        _descriptor = new QueryDescriptor(
            queryType,
            method,
            "/api/queries/query-with-validation",
            "QueryWithValidation",
            nameof(String),
            "() => ({})",
            false,
            false,
            Enumerable.Empty<ImportStatement>().OrderBy(_ => _.Module),
            [],
            [],
            properties,
            [],
            null,
            validationRules);
    }

    void Because()
    {
        _generatedCode = InMemoryProxyGenerator.GenerateQuery(_descriptor);

        try
        {
            var transpiledCode = _runtime.TranspileTypeScript(_generatedCode);
            _typeScriptIsValid = !string.IsNullOrEmpty(transpiledCode);
        }
        catch
        {
            _typeScriptIsValid = false;
        }
    }

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeEmpty();
    [Fact] void should_contain_validator_class() => _generatedCode.ShouldContain("class QueryWithValidationValidator");
    [Fact] void should_extend_query_validator() => _generatedCode.ShouldContain("extends QueryValidator");
    [Fact] void should_contain_rule_for_min_age() => _generatedCode.ShouldContain("this.ruleFor(c => c.minAge)");
    [Fact] void should_contain_greater_than_or_equal_rule() => _generatedCode.ShouldContain(".greaterThanOrEqual(0)");
    [Fact] void should_contain_less_than_or_equal_rule() => _generatedCode.ShouldContain(".lessThanOrEqual(150)");
    [Fact] void should_contain_rule_for_search_term() => _generatedCode.ShouldContain("this.ruleFor(c => c.searchTerm)");
    [Fact] void should_contain_min_length_rule() => _generatedCode.ShouldContain(".minLength(3)");
    [Fact] void should_be_valid_typescript() => _typeScriptIsValid.ShouldBeTrue();

    public void Dispose() => _runtime?.Dispose();
}
