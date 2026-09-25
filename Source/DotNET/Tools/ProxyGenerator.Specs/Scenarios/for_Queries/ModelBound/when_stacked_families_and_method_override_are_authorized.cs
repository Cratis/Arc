// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

[Collection(ScenarioCollectionDefinition.Name)]
public class when_stacked_families_and_method_override_are_authorized : given.a_scenario_web_application
{
    HttpStatusCode _withoutAudit;
    HttpStatusCode _withBothPolicies;
    HttpStatusCode _methodOverride;
    int _allBefore;
    int _overrideBefore;

    void Establish()
    {
        _allBefore = StackedPolicyReadModel.AllCount;
        _overrideBefore = StackedPolicyReadModel.OverrideCount;
    }

    async Task Because()
    {
        HttpClient!.DefaultRequestHeaders.Add("X-Special", "active");
        using (var denied = await HttpClient.GetAsync("/api/stacked-policy"))
        {
            _withoutAudit = denied.StatusCode;
        }

        HttpClient.DefaultRequestHeaders.Add("X-Permission-Audit", "true");
        using (var allowed = await HttpClient.GetAsync("/api/stacked-policy"))
        {
            _withBothPolicies = allowed.StatusCode;
        }

        HttpClient.DefaultRequestHeaders.Remove("X-Special");
        HttpClient.DefaultRequestHeaders.Remove("X-Permission-Audit");
        HttpClient.DefaultRequestHeaders.Add("X-Default", "active");
        HttpClient.DefaultRequestHeaders.Add("X-Role", "Admin");
        using var overridden = await HttpClient.GetAsync("/api/stacked-override");
        _methodOverride = overridden.StatusCode;
    }

    [Fact] void should_require_both_attribute_family_policies() => _withoutAudit.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_allow_when_both_policies_pass() => _withBothPolicies.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_only_run_the_successful_stacked_query() => StackedPolicyReadModel.AllCount.ShouldEqual(_allBefore + 1);
    [Fact] void should_replace_the_type_policies_with_method_roles() => _methodOverride.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_run_the_method_override_once() => StackedPolicyReadModel.OverrideCount.ShouldEqual(_overrideBefore + 1);
}
