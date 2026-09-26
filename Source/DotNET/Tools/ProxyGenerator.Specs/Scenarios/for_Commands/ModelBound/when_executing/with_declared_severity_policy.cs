// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_declared_severity_policy : given.a_scenario_web_application
{
    record PolicyFailure(JsonElement Severity, string Message);
    record PolicyResponse(bool IsSuccess, PolicyFailure[] ValidationResults);

    PolicyResponse[] _results;

    async Task Because()
    {
        SeverityPolicyCommand.Handled = 0;
        var commandType = typeof(SeverityPolicyCommand).GetTypeInfo();
        var route = commandType.ToCommandDescriptor("", 0, false, "api", [commandType]).Route;
        var results = new List<PolicyResponse>();
        foreach (var level in new[] { "warning", "information" })
        {
            results.Add(await Send(level, route, false));
            results.Add(await Send(level, route, true));
        }
        _results = [.. results];
    }

    async Task<PolicyResponse> Send(string level, string route, bool permissive)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(new SeverityPolicyCommand(level))
        };
        if (permissive)
        {
            request.Headers.Add("X-Allowed-Severity", "3");
        }
        using var response = await HttpClient!.SendAsync(request);
        return (await response.Content.ReadFromJsonAsync<PolicyResponse>())!;
    }

    [Fact] void should_reject_all_four_requests() => _results.All(result => !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_keep_the_severity_and_message() => _results.Select((result, index) =>
        result.ValidationResults.Single().Severity.GetInt32() == (int)(index < 2 ? ValidationResultSeverity.Warning : ValidationResultSeverity.Information) &&
        result.ValidationResults.Single().Message == (index < 2 ? "Keep warning message" : "Keep information message"))
        .All(matches => matches).ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => SeverityPolicyCommand.Handled.ShouldEqual(0);
}
