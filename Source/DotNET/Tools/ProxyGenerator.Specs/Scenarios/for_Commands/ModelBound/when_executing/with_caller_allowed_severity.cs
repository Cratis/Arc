// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound.when_executing;

[Collection(ScenarioCollectionDefinition.Name)]
public class with_caller_allowed_severity : given.a_scenario_web_application
{
    record Response(bool IsSuccess);
    Response[] _defaultResults;
    Response[] _optedInResults;

    async Task Because()
    {
        UncappedSeverityCommand.Handled = 0;
        CappedSeverityCommand.Handled = 0;
        _defaultResults = await Send(new UncappedSeverityCommand("invalid"));
        _optedInResults = await Send(new CappedSeverityCommand("invalid"));
    }

    async Task<Response[]> Send<T>(T command)
    {
        var type = typeof(T).GetTypeInfo();
        var route = type.ToCommandDescriptor("", 0, false, "api", [type]).Route;
        var results = new List<Response>();
        foreach (var suffix in new[] { "", "/validate" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, route + suffix) { Content = JsonContent.Create(command) };
            request.Headers.Add("X-Allowed-Severity", "3");
            using var response = await HttpClient!.SendAsync(request);
            results.Add((await response.Content.ReadFromJsonAsync<Response>())!);
        }
        return [.. results];
    }

    [Fact] void should_reject_errors_by_default() => _defaultResults.All(result => !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_not_handle_the_rejected_command() => UncappedSeverityCommand.Handled.ShouldEqual(0);
    [Fact] void should_allow_the_opted_in_command() => _optedInResults.All(result => result.IsSuccess).ShouldBeTrue();
    [Fact] void should_handle_the_opted_in_command_once() => CappedSeverityCommand.Handled.ShouldEqual(1);
}
