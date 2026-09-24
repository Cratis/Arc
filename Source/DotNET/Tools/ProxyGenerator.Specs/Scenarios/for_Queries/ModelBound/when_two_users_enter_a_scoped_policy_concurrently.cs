// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_two_users_enter_a_scoped_policy_concurrently : given.a_scenario_web_application
{
    PolicyGate _gate => Host!.Services.GetRequiredService<PolicyGate>();
    IReadOnlyList<(string? User, Guid Scope)> _visits;
    (HttpStatusCode Status, string User)[] _results;
    bool _completedBeforeRelease;
    int _performedBefore;
    int _performedBeforeRelease;

    void Establish()
    {
        _gate.Reset();
        _performedBefore = GatedReadModel.Performed;
    }

    async Task Because()
    {
        var first = QueryAs("alpha");
        var second = QueryAs("beta");
        try
        {
            await _gate.WaitForTwoEntries();
            _visits = _gate.Visits;
            _completedBeforeRelease = first.IsCompleted || second.IsCompleted;
            _performedBeforeRelease = GatedReadModel.Performed;
        }
        finally
        {
            _gate.Release();
        }

        _results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_receive_both_distinct_caller_identities() => _visits.Select(visit => visit.User).ShouldContainOnly(["alpha", "beta"]);
    [Fact] void should_use_a_different_scoped_policy_dependency_for_each_user() => _visits.Select(visit => visit.Scope).Distinct().Count().ShouldEqual(2);
    [Fact] void should_not_return_while_either_policy_is_pending() => _completedBeforeRelease.ShouldBeFalse();
    [Fact] void should_not_execute_either_query_before_release() => _performedBeforeRelease.ShouldEqual(_performedBefore);
    [Fact] void should_return_the_right_user_to_each_caller() => _results.Select(result => result.User).ShouldContainOnly(["alpha", "beta"]);
    [Fact] void should_authorize_both_requests() => _results.All(result => result.Status == HttpStatusCode.OK).ShouldBeTrue();
    [Fact] void should_perform_both_queries_after_release() => GatedReadModel.Performed.ShouldEqual(_performedBefore + 2);

    async Task<(HttpStatusCode Status, string User)> QueryAs(string user)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/gated-read-model");
        request.Headers.Add("X-Default", "active");
        request.Headers.Add("X-User", user);
        using var response = await HttpClient!.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Cratis.Arc.Queries.QueryResult>(body, Json.Globals.JsonSerializerOptions)!;
        var model = JsonSerializer.Deserialize<GatedReadModel>(((JsonElement)result.Data).GetRawText(), Json.Globals.JsonSerializerOptions)!;
        return (response.StatusCode, model.Value);
    }
}
